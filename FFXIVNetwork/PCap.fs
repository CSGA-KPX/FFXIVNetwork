module PCap
open System
open LibFFXIV.Constants
open LibFFXIV.GeneralPacket
open PcapDotNet.Core
open PcapDotNet.Packets
open PcapDotNet.Packets.IpV4
open PcapDotNet.Packets.Transport

type QueuedPacket =  
    {   
        SeqNum  : uint32
        NextSeq : uint32
        Data    : byte []
        //LastSeen  : DateTime
    }

    member x.IsFirstPacket() = 
        if x.Data.Length < 0x10 then
            false
        else
            let magic = x.Data.[0 .. 15]
            Utils.HexString.toHex(magic) = LibFFXIV.Constants.FFXIVBasePacketMagic
                

    member x.IsPacketComplete() = 
        x.IsFirstPacket() && (x.Data.Length >= x.FullPacketSize())

    member x.IsNextPacket(y) = 
        x.NextSeq = y.SeqNum

    member x.FullPacketSize() = 
        FFXIVBasePacket.GetPacketSize(x.Data)

    override x.ToString() = 
        sprintf "%i -> %i Fin:%b : %s" x.SeqNum x.NextSeq (x.IsPacketComplete()) (Utils.HexString.toHex(x.Data))

    static member FromTcpDatagram(t : PcapDotNet.Packets.Transport.TcpDatagram) = 
        {
            SeqNum   = t.SequenceNumber
            NextSeq  = t.NextSequenceNumber
            Data     = t.Payload.ToMemoryStream().ToArray()
            //LastSeen = DateTime.Now
        }

    static member (+) (x, y) =
        {
            SeqNum   = x.SeqNum
            NextSeq  = y.NextSeq
            Data     = Array.append x.Data y.Data
            //LastSeen = y.LastSeen
        }


type GamePacketQueueV3() = 
    let testtt = ""
    let oldlck func = lock testtt func
    let dict   = new System.Collections.Generic.Dictionary<uint32, QueuedPacket>()
    let evt    = new Event<byte []>()
    let logger = NLog.LogManager.GetCurrentClassLogger()

    member x.NewPacketEvent = evt.Publish

    member x.GetQueuedItemCount() = dict.Count

    member private x.processPacketCompleteness(p : QueuedPacket) = 
        if p.IsPacketComplete() then
            let rec yieldPacket (rest) = 
                let rst = FFXIVBasePacket.TakePacket(rest)
                if rst.IsNone then
                    rest
                else
                    let (p, rst) = rst.Value
                    evt.Trigger(p)
                    yieldPacket(rst)
            let rst = yieldPacket(p.Data)
            if rst.Length <> 0 then
                logger.Trace(sprintf "Rst<>0, Added nsq:%i data:%s" (p.NextSeq) (Utils.HexString.toHex(rst)))
                dict.Add(p.NextSeq, {p with Data = rst})
        else
            logger.Trace(sprintf "NewPkt, Added nsq:%i" (p.NextSeq))
            dict.Add(p.NextSeq, p)

    member private x.processPacketChain(p : QueuedPacket) = 
        let FwdSearch = dict.ContainsKey(p.SeqNum)
        let RevSearch = 
            dict
            |> Seq.filter (fun x -> 
                x.Value.SeqNum = p.NextSeq)
            |> Seq.tryHead

        match FwdSearch, RevSearch.IsSome with
        | true, true   ->
            let np = dict.[p.SeqNum] + p + RevSearch.Value.Value
            dict.Remove(p.SeqNum) |> ignore
            dict.Remove(RevSearch.Value.Key) |> ignore
            x.processPacketChain(np)
        | true, false  -> 
            let np = dict.[p.SeqNum] + p
            dict.Remove(p.SeqNum) |> ignore
            logger.Trace(sprintf "Forward packet: %s" (np.ToString()))
            x.processPacketChain(np)
        | false, true  -> 
            let np =  p + RevSearch.Value.Value
            dict.Remove(RevSearch.Value.Key) |> ignore
            logger.Trace(sprintf "Reverse packet: %s" (np.ToString()))
            x.processPacketChain(np)
        | false, false -> 
            x.processPacketCompleteness(p)

    member x.Enqueue(p : QueuedPacket) =
        logger.Trace(sprintf "Current dict : %A, current seq:%i next seq : %i \r\n%s" dict.Keys p.SeqNum p.NextSeq (Utils.HexString.toHex(p.Data)))
        oldlck (fun () -> x.processPacketChain(p))

let queue = 
    let q = new GamePacketQueueV3()
    q.NewPacketEvent.Add(FFXIV.PacketHandler.PacketHandler)
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

let RawPacketLogger = NLog.LogManager.GetLogger("RawTCPPacket")

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
        RawPacketLogger.Trace(sprintf "<<<<<<%i,%i,%s" (tcp.SequenceNumber) (tcp.NextSequenceNumber) (tcp.Payload.ToMemoryStream().ToArray() |> Utils.HexString.toHex))
        queue.Enqueue(QueuedPacket.FromTcpDatagram(tcp))
        
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
    