module PCap
open Microsoft.FSharp.Core.Operators.Checked
open System
open LibFFXIV.Constants
open LibFFXIV.TcpPacket
open PcapDotNet.Core
open PcapDotNet.Packets
open PcapDotNet.Packets.IpV4
open PcapDotNet.Packets.Transport


let RawPacketLogger = NLog.LogManager.GetLogger("RawTCPPacket")

let queue = 
    let q = new LibFFXIV.TcpPacket.GamePacketQueue()
    q.NewCompleteDataEvent.Add(FFXIV.PacketHandler.PacketHandler)
    q.NewCompleteDataEvent.Add((fun x -> RawPacketLogger.Trace(sprintf "Recv full packet:%s" (Utils.HexString.ToHex(x)))))
    q


//    Payload len = 0
// or Start with 0x00 *  24
let isGamePacket(tcp : TcpDatagram) =
    let len = tcp.Payload.Length
    if len = 0 then
        false
    else 
        let payload = tcp.Payload
        let len     = payload.Length
        if (len <= 32) || (payload.ToHexadecimalString().[0..31].ToUpper() <> FFXIVBasePacketMagicAlt) then
            true
        else
            false  


let PacketHandler (p:Packet) = 
    let ip = p.Ethernet.IpV4
    let tcp= ip.Tcp

    let (|Income|Outcome|Miss|) (ip : IpV4Datagram) = 
        let remoteAddress = ip.Destination.ToString()
        let  localAddress = ip.Source.ToString()
        let serverIP      = FFXIV.Connections.ServerIP.Get()
        if   serverIP.IsSome && serverIP.Value = remoteAddress then
            Outcome
        elif serverIP.IsSome && serverIP.Value = localAddress then
            Income
        else
            Miss

    
    match ip with
    | Income when isGamePacket(ip.Tcp) -> 
        RawPacketLogger.Trace(sprintf "<<<<<<%i,%i,%s" (tcp.SequenceNumber) (tcp.NextSequenceNumber) (tcp.Payload.ToMemoryStream().ToArray() |> Utils.HexString.ToHex))
        queue.Enqueue(
            {
                SeqNum  = tcp.SequenceNumber
                NextSeq = tcp.NextSequenceNumber
                Data    = tcp.Payload.ToMemoryStream().ToArray()
            })
        //queue.Enqueue(new LibFFXIV.TcpPacket.PacketQueueItem(tcp.SequenceNumber, tcp.NextSequenceNumber, tcp.Payload.ToMemoryStream().ToArray()))
        
    | Income -> ()
    | Outcome -> () //printfn "%s %i -> %i %i " time (tcp.SourcePort) (tcp.DestinationPort) length
    | Miss -> ()
    ()

let Start() = 
    let device = 
        LivePacketDevice.AllLocalMachine
        |> Seq.filter (fun x ->
            x.Addresses.[0].Address.Family = SocketAddressFamily.Internet)
        |> Seq.mapi (fun i x -> 
            printfn "%i %s %A" i x.Name (x.Addresses.[0].Address);x)
        |> Seq.tryHead

    if device.IsSome then
        using (device.Value.Open(65536, PacketDeviceOpenAttributes.Promiscuous, 1000)) (fun communicator ->
            if communicator.DataLink.Kind <> DataLinkKind.Ethernet then
                failwithf "This program works only on Ethernet networks."
            using (communicator.CreateFilter("ip and tcp")) (fun filter -> communicator.SetFilter(filter))
            communicator.ReceivePackets(0, new HandlePacket (PacketHandler)) |> ignore
        )
    else
        failwith "检测不到适合的适配器"
    