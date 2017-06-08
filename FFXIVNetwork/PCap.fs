module PCap
open System
open System.Diagnostics
open LibFFXIV.Constants
open LibFFXIV.GeneralPacket
open PcapDotNet.Core
open PcapDotNet.Packets
open PcapDotNet.Packets.IpV4
open PcapDotNet.Packets.Transport

//    Payload len = 0
// or Start with 0x00 *  24
let isGamePacket(tcp : TcpDatagram) =
    let len = tcp.Payload.Length
    if len = 0 then
        false
    else 
        let payload = tcp.Payload
        let magic   = payload.ToHexadecimalString().[0..31].ToUpper()
        if magic = FFXIVBasePacketMagic || (magic <> FFXIVBasePacketMagicAlt) then
            true
        else
            false

type GamePacketQueueV2() = 
    let dict = new System.Collections.Generic.Dictionary<uint32, TcpDatagram []>()
    let setLock = ref ()
    let sLock func =
            lock setLock func
    let getBytes (ds : TcpDatagram[]) = 
        ds
        |> Array.map (fun x -> x.Payload.ToMemoryStream().ToArray())
        |> Array.reduce (fun acc item -> Array.append acc item) 

    let evt = new Event<byte []>()

    let logger = NLog.LogManager.GetCurrentClassLogger()

    member x.NewPacketEvent = evt.Publish

    member x.Enqueue(tcp : TcpDatagram) =
        let bytes = tcp.Payload.ToMemoryStream().ToArray()
        let res = FFXIVBasePacket.TakePacket(bytes)
        if res.IsSome then
            //当前段已经完整
            evt.Trigger(bytes)
        else
            logger.Trace(sprintf "All next seqs:%A" dict.Keys)
            logger.Trace(sprintf "current  seqs:%A" tcp.SequenceNumber)
            let seq = tcp.SequenceNumber
            if dict.ContainsKey(seq) then
                sLock (fun () ->
                    let merged = Array.append dict.[seq] [|tcp|]
                    let bytes = getBytes merged
                    logger.Trace(Utils.HexString.toHex(bytes))
                    let res = FFXIVBasePacket.TakePacket(bytes)
                    dict.Remove(seq) |> ignore
                    if res.IsSome then
                        logger.Trace("Indirect packet Hit!")
                        evt.Trigger(bytes)
                    else
                        logger.Trace(sprintf "Readd %i" tcp.NextSequenceNumber)
                        dict.Add(tcp.NextSequenceNumber, merged))
            else // 没找到，新建一个
                logger.Trace(sprintf "New incomplete packet tcp.seq =  %i" tcp.NextSequenceNumber)
                sLock (fun () -> dict.Add(tcp.NextSequenceNumber, [| tcp |]))


let queue = 
    let q = new GamePacketQueueV2()
    q.NewPacketEvent.Add(FFXIV.PacketHandler.PacketHandler)
    q

let PacketHandler (p:Packet) = 
    let ip = p.Ethernet.IpV4
    let tcp= ip.Tcp

    let (|Income|Outcome|Miss|) (ip : IpV4Datagram) = 
        let remoteAddress = ip.Destination.ToString()
        let  localAddress = ip.Source.ToString()
        let serverIP = FFXIV.Connections.ServerIP.Get()
        assert (serverIP.IsSome)
        if   serverIP.Value = remoteAddress then
            Outcome
        elif serverIP.Value = localAddress then
            Income
        else
            Miss

    let time = p.Timestamp.ToString("yyyy-MM-dd hh:mm:ss.fff")
    let length = p.Length
    let ipInfo = sprintf "IP:length %i/%i F:%i" ip.Length ip.TotalLength ip.Fragmentation.Offset
    let tcpInfo = sprintf "TCP: port: %i->%i seq: %i len: %i/%i" (tcp.SourcePort) (tcp.DestinationPort) tcp.SequenceNumber tcp.PayloadLength tcp.Length
    
    match ip with
    | Income when isGamePacket(ip.Tcp) -> 
        let sb = new Text.StringBuilder()
        sb.AppendLine(sprintf "%s %s %s " time ipInfo tcpInfo)
          .AppendLine(sprintf "%s" (tcp.Payload.ToHexadecimalString())) |> ignore
        NLog.LogManager.GetCurrentClassLogger().Trace(sb.ToString())
        queue.Enqueue(tcp)

    | Income -> ()
    | Outcome -> () //printfn "%s %i -> %i %i " time (tcp.SourcePort) (tcp.DestinationPort) length
    | Miss -> ()
    ()

let Start() = 
    let allDevices = LivePacketDevice.AllLocalMachine
    allDevices
    |> Seq.iteri (fun i x -> 
        printfn "%i %s %A" i x.Name (x.Addresses.[0].Address)
    )

    let selectedDevice = allDevices.[0]
    
    using (selectedDevice.Open(65536, PacketDeviceOpenAttributes.Promiscuous, 1000)) (fun communicator ->
        if communicator.DataLink.Kind <> DataLinkKind.Ethernet then
            failwithf "This program works only on Ethernet networks."
        using (communicator.CreateFilter("ip and tcp")) (fun filter -> communicator.SetFilter(filter))
        communicator.ReceivePackets(0, new HandlePacket (PacketHandler)) |> ignore
    )
    