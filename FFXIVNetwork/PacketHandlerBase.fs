module FFXIV.PacketHandlerBase
open System
open System.Text
open System.Reflection
open System.Collections.Generic
open LibFFXIV.Network.BasePacket
open LibFFXIV.Network.Constants
open LibFFXIV.Network.Utils

type StringBuilder = B of (Text.StringBuilder -> unit)

let buildString (B f) =
    let b = new Text.StringBuilder()
    do f b
    b.ToString ()

type StringBuilderM () =
    let (!) = function B f -> f
    member __.Yield (txt : string) = B(fun b -> b.AppendLine txt |> ignore)
    member __.YieldBang (txt : string) = B(fun b -> b.Append txt |> ignore)
    member __.Yield (c : char) = B(fun b -> b.Append c |> ignore)
//    member __.Yield (o : obj) = B(fun b -> b.Append o |> ignore)
    member __.YieldFrom f = f : StringBuilder

    member __.Combine(f,g) = B(fun b -> !f b; !g b)
    member __.Delay f = B(fun b -> !(f ()) b) : StringBuilder
    member __.Zero () = B(fun _ -> ())
    member __.For (xs : 'a seq, f : 'a -> StringBuilder) =
                    B(fun b ->
                        let e = xs.GetEnumerator ()
                        while e.MoveNext() do
                            !(f e.Current) b)
    member __.While (p : unit -> bool, f : StringBuilder) =
                    B(fun b -> while p () do !f b)

let sb = new StringBuilderM ()

[<AttributeUsageAttribute(AttributeTargets.Method)>]
type PacketHandleMethodAttribute(opcode : Opcodes, direction : PacketDirection) = 
    inherit System.Attribute()

    member x.Direction with get() = direction
    member x.OpCode with get() = opcode

[<AbstractClassAttribute>]
type PacketHandlerBase() as x = 
    let logger = NLog.LogManager.GetLogger(x.GetType().Name)

    member internal x.Logger = logger

type PacketHandler() as x = 
    let handlersOut= new Dictionary<LibFFXIV.Network.Constants.Opcodes, (PacketHandlerBase *  MethodInfo)>()
    let handlersIn = new Dictionary<LibFFXIV.Network.Constants.Opcodes, (PacketHandlerBase *  MethodInfo)>()
    let logger     = NLog.LogManager.GetCurrentClassLogger()
    let plogger    = NLog.LogManager.GetLogger("PacketLogger")

    do
        x.AddAssembly(Assembly.GetExecutingAssembly())

    member x.AddAssembly(asm : Assembly) = 
        let types = 
            try
                asm.GetTypes()
            with
            | :? ReflectionTypeLoadException as e ->
                e.Types |> Array.filter (isNull >> not)

        let pbt   = typeof<PacketHandlerBase>
        let att   = typeof<PacketHandleMethodAttribute>
        
        for t in types do 
            let isAbstract = t.IsInterface || t.IsAbstract
            if (not isAbstract) && (t.IsSubclassOf(pbt)) then
                let instance = (Activator.CreateInstance(t)) :?> PacketHandlerBase
                let methods = t.GetMethods(BindingFlags.Instance ||| BindingFlags.Public ||| BindingFlags.DeclaredOnly)
                for m in methods do 
                    let at = m.GetCustomAttribute(att)
                    if not (isNull at) then
                        let at = at :?> PacketHandleMethodAttribute
                        match at.Direction with
                        | PacketDirection.In  ->  handlersIn.Add(at.OpCode, (instance, m))
                        | PacketDirection.Out -> handlersOut.Add(at.OpCode, (instance, m))
                        | _ -> invalidArg "direction" "unknown"
                        

    member private x.LogGamePacketOne (gp : FFXIVGamePacket, direction, epoch) = 
        let dir = 
            match direction with
            | PacketDirection.In  -> "<<<<<"
            | PacketDirection.Out -> ">>>>>"
            | _ -> invalidArg "direction" "unknown"
        plogger.Trace("{0} {1}", dir, gp.ToString())

    member x.HandlePacketMachina (epoch : int64, data : byte [], direction : PacketDirection) = 
        try
            let data = new ByteArray(data)
            let sp = new FFXIVSubPacket(data)
            match sp.Type with
            | PacketTypes.KeepAliveRequest
            | PacketTypes.KeepAliveResponse
            | PacketTypes.ClientHelloWorld
            | PacketTypes.ServerHelloWorld
            | PacketTypes.ClientHandShake
            | PacketTypes.ServerHandShake
                -> ()
            | PacketTypes.GameMessage ->
                let gp = new FFXIVGamePacket(sp.Data)
                x.LogGamePacketOne(gp, direction, epoch)
                try
                    let op = gp.Opcode
                    match direction with
                    | PacketDirection.In  ->
                        if handlersIn.ContainsKey(op) then
                            let (obj, method) = handlersIn.[op]
                            method.Invoke(obj, [| box gp |]) |> ignore
                    | PacketDirection.Out -> 
                        if handlersOut.ContainsKey(op) then
                            let (obj, method) = handlersOut.[op]
                            method.Invoke(obj, [| box gp |]) |> ignore
                    | _ -> invalidArg "direction" "unknown"
                with
                | e -> logger.Error("数据包处理错误:{0}, {1}", e.ToString(), sprintf "%A" gp)
            | _ -> failwithf "未知子包类型%O" (sp.Type)
            
        with
        | e ->  
            logger.Error("Error packet:{0}, {1}", e.ToString(), HexString.ToHex(data))
