module FFXIV.PacketHandlerBase
open System
open System.Text
open System.Reflection
open System.Collections.Generic
open LibFFXIV.BasePacket
open LibFFXIV.Constants

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
type PacketHandleMethodAttribute(opcode : LibFFXIV.Constants.Opcodes) = 
    inherit System.Attribute()

    member x.OpCode with get() = opcode

[<AbstractClassAttribute>]
type PacketHandlerBase() as x = 
    let logger = NLog.LogManager.GetLogger(x.GetType().FullName)

    member internal x.Logger = logger


(*
type PacketReceivedEvent() = 
    let event = new Event<FFXIVGamePacket>()

    member x.AddHandler(func) = 
        Event.add func event.Publish
    
    member x.Trigger(p : FFXIVGamePacket) = 
        event.Trigger(p)*)


type PacketHandler() as x = 
    let handlers   = new Dictionary<LibFFXIV.Constants.Opcodes, (PacketHandlerBase *  MethodInfo)>()
    let logger     = NLog.LogManager.GetCurrentClassLogger()
    let plogger    = NLog.LogManager.GetLogger("PacketLogger")

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
                        handlers.Add(at.OpCode, (instance, m))

    member private x.LogGamePacket (idx, total, gp : FFXIVGamePacket, direction) = 
        let opcode = Utils.HexString.ToHex (BitConverter.GetBytes(gp.Opcode))
        let ts     = gp.TimeStamp
        let data   = Utils.HexString.ToHex (gp.Data)
        let dir    = 
            match direction with
            | LibFFXIV.TcpPacket.PacketDirection.In  -> "<<<<<"
            | LibFFXIV.TcpPacket.PacketDirection.Out -> ">>>>>"
        plogger.Trace("GamePacket:{6} MA:{5} OP:{0} TS:{1} {2}/{3} Data:{4}", opcode, ts, idx + 1, total + 1, data, gp.Magic, dir)

    member private x.LogGamePacketOne (gp : FFXIVGamePacket, direction) = 
        let opcode = Utils.HexString.ToHex (BitConverter.GetBytes(gp.Opcode))
        let ts     = gp.TimeStamp
        let data   = Utils.HexString.ToHex (gp.Data)
        let dir    = 
            match direction with
            | LibFFXIV.TcpPacket.PacketDirection.In  -> "<<<<<"
            | LibFFXIV.TcpPacket.PacketDirection.Out -> ">>>>>"
        plogger.Trace("GamePacket:{6} MA:{5} OP:{0} TS:{1} {2}/{3} Data:{4}", opcode, ts, 0, 0, data, gp.Magic, dir)

    member x.HandlePacketMachina (epoch : int64, data : byte [], direction : LibFFXIV.TcpPacket.PacketDirection) = 
        try

            let sp = FFXIVSubPacket.Parse(data).[0] //TODO
            let spType = LanguagePrimitives.EnumOfValue<uint16, PacketTypes>(sp.Type)
            match spType with
            | PacketTypes.None ->
                failwithf "不应出现PacketTypes.None"
            | PacketTypes.KeepAliveRequest
            | PacketTypes.KeepAliveResponse
            | PacketTypes.ClientHelloWorld
            | PacketTypes.ServerHelloWorld
            | PacketTypes.ClientHandShake
            | PacketTypes.ServerHandShake
                -> ()
            | PacketTypes.GameMessage ->
                let gp = FFXIVGamePacket.ParseFromBytes(sp.Data)
                x.LogGamePacketOne(gp, direction)
                match direction with
                | LibFFXIV.TcpPacket.PacketDirection.In  ->
                    let op = LanguagePrimitives.EnumOfValue<uint16, Opcodes>(gp.Opcode)
                    if handlers.ContainsKey(op) then
                        let (obj, method) = handlers.[op]
                        method.Invoke(obj, [| box gp |]) |> ignore
                | LibFFXIV.TcpPacket.PacketDirection.Out -> ()
            | _ -> failwithf "未知子包类型%O" spType
            
        with
        | e ->  
            logger.Error("Error packet:{0}", Utils.HexString.ToHex(data))
