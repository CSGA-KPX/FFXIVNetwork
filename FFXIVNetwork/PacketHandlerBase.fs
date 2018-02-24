module FFXIV.PacketHandlerBase
open System
open System.Text
open System.Reflection
open System.Collections.Generic
open LibFFXIV.Network.BasePacket
open LibFFXIV.Network.Constants

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
    let logger = NLog.LogManager.GetLogger(x.GetType().FullName)

    member internal x.Logger = logger

type PacketHandler() as x = 
    let handlersOut= new Dictionary<LibFFXIV.Network.Constants.Opcodes, (PacketHandlerBase *  MethodInfo)>()
    let handlersIn = new Dictionary<LibFFXIV.Network.Constants.Opcodes, (PacketHandlerBase *  MethodInfo)>()
    let logger     = NLog.LogManager.GetCurrentClassLogger()
    let plogger    = NLog.LogManager.GetLogger("PacketLogger")
    let rawLogger    = NLog.LogManager.GetLogger("RawTCPPacket")

    do
        x.AddAssembly(Assembly.GetExecutingAssembly())

    member x.AddAssembly(asm : Assembly) = 
        let types = asm.GetTypes()
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
        let opcode = Utils.HexString.ToHex (BitConverter.GetBytes(gp.Opcode))
        let ts     = gp.TimeStamp
        let data   = Utils.HexString.ToHex (gp.Data)
        let dir    = 
            match direction with
            | PacketDirection.In  -> "<<<<<"
            | PacketDirection.Out -> ">>>>>"
            | _ -> invalidArg "direction" "unknown"
        plogger.Trace("GamePacket:{6} {7} OP:{0} TS:{1} {2}/{3} Data:{4}", opcode, ts, 0, 0, data, gp.Magic, dir, epoch)

    member x.HandlePacketMachina (epoch : int64, data : byte [], direction : PacketDirection) = 
        try
            let sp = FFXIVSubPacket.Parse(data).[0] //TODO
            let spType = LanguagePrimitives.EnumOfValue<uint16, PacketTypes>(sp.Type)
            match spType with
            | PacketTypes.KeepAliveRequest
            | PacketTypes.KeepAliveResponse
            | PacketTypes.ClientHelloWorld
            | PacketTypes.ServerHelloWorld
            | PacketTypes.ClientHandShake
            | PacketTypes.ServerHandShake
                -> ()
            | PacketTypes.GameMessage ->
                rawLogger.Trace(Utils.HexString.ToHex(sp.Data))
                let gp = FFXIVGamePacket.ParseFromBytes(sp.Data)
                x.LogGamePacketOne(gp, direction, epoch)
                try
                    match direction with
                    | PacketDirection.In  ->
                        let op = LanguagePrimitives.EnumOfValue<uint16, Opcodes>(gp.Opcode)
                        if handlersIn.ContainsKey(op) then
                            let (obj, method) = handlersIn.[op]
                            method.Invoke(obj, [| box gp |]) |> ignore
                    | PacketDirection.Out -> 
                        let op = LanguagePrimitives.EnumOfValue<uint16, Opcodes>(gp.Opcode)
                        if handlersOut.ContainsKey(op) then
                            let (obj, method) = handlersOut.[op]
                            method.Invoke(obj, [| box gp |]) |> ignore
                    | _ -> invalidArg "direction" "unknown"
                with
                | e -> logger.Error("数据包处理错误:{0}, {1}", e.ToString(), sprintf "%A" gp)
            | _ -> failwithf "未知子包类型%O" spType
            
        with
        | e ->  
            logger.Error("Error packet:{0}, {1}", e.ToString(), Utils.HexString.ToHex(data))
