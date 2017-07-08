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

let incomePacketQueue = 
    let q = new LibFFXIV.TcpPacket.GamePacketQueue()
    q.NewCompleteDataEvent.Add(FFXIV.PacketHandler.PacketHandler)
    q

let outcomePacketQueue = 
    let q = new LibFFXIV.TcpPacket.GamePacketQueue()
    q.NewCompleteDataEvent.Add(FFXIV.PacketHandler.PacketHandler)
    q

//    Payload len = 0
// or Start with 0x00 *  24
let isGameBasePacket(tcp : TcpDatagram) =
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
        let serverIP      = FFXIV.Connections.ServerIP.GetServer()
        if   serverIP.IsSome && serverIP.Value = remoteAddress then
            Outcome
        elif serverIP.IsSome && serverIP.Value = localAddress then
            Income
        else
            Miss

    
    match ip with
    | Income when isGameBasePacket(ip.Tcp) -> 
        RawPacketLogger.Trace(sprintf "<<<<<<%i,%i,%s" (tcp.SequenceNumber) (tcp.NextSequenceNumber) (tcp.Payload.ToMemoryStream().ToArray() |> Utils.HexString.ToHex))
        incomePacketQueue.Enqueue(
            {
                SeqNum  = tcp.SequenceNumber
                NextSeq = tcp.NextSequenceNumber
                Data    = tcp.Payload.ToMemoryStream().ToArray()
            })
        //queue.Enqueue(new LibFFXIV.TcpPacket.PacketQueueItem(tcp.SequenceNumber, tcp.NextSequenceNumber, tcp.Payload.ToMemoryStream().ToArray()))
    | Outcome when isGameBasePacket(ip.Tcp) ->
        RawPacketLogger.Trace(sprintf ">>>>>>%i,%i,%s" (tcp.SequenceNumber) (tcp.NextSequenceNumber) (tcp.Payload.ToMemoryStream().ToArray() |> Utils.HexString.ToHex))
        outcomePacketQueue.Enqueue(
            {
                SeqNum  = tcp.SequenceNumber
                NextSeq = tcp.NextSequenceNumber
                Data    = tcp.Payload.ToMemoryStream().ToArray()
            })

    | _    -> ()

let Start() = 
    let clientIP = FFXIV.Connections.ServerIP.GetClient()
    let devices = 
        LivePacketDevice.AllLocalMachine
        |> Seq.filter (fun x ->
            x.Addresses
            |> Seq.exists (fun addr ->
                let a = addr.Address.Family =SocketAddressFamily.Internet
                let b = addr.Address.ToString().Contains(clientIP.Value)
                a && b
            ))        
        |> Seq.mapi (fun i x -> 
            printfn "可用适配器%i %s" i x.Name
            for addr in x.Addresses do 
                printfn "\t地址：: %A %A" addr.Address addr.Netmask
            x)
        |> Seq.toArray

    let device = devices |> Array.tryHead
    if device.IsSome then
        use communicator = device.Value.Open(65536, PacketDeviceOpenAttributes.Promiscuous, 1000)
        if communicator.DataLink.Kind <> DataLinkKind.Ethernet then
            failwithf "This program works only on Ethernet networks."
        communicator.SetFilter(communicator.CreateFilter("ip and tcp"))
        try
            while true do 
                let (result, packet) = communicator.ReceivePacket()
                match result with
                | PacketCommunicatorReceiveResult.Timeout -> ()
                | PacketCommunicatorReceiveResult.Ok ->PacketHandler(packet)
                | _ ->
                    RawPacketLogger.Error("包捕获异常（无视）")

        with
        | e -> 
            RawPacketLogger.Fatal(e, "包捕获异常终止")
    else
        failwith "检测不到适合的适配器，请检查WinPcap等是否正确安装"
    