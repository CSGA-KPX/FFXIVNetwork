module PCap
open System
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
        x.IsFirstPacket() && (x.Data.Length >= x.FullPacketSize)

    member x.IsNextPacket(y) = 
        x.NextSeq = y.SeqNum

    member x.FullPacketSize = 
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
    let locker = new Threading.ReaderWriterLockSlim(Threading.LockRecursionPolicy.SupportsRecursion)
    let dict   = new System.Collections.Generic.Dictionary<uint32, QueuedPacket>()
    let evt    = new Event<byte []>()
    let logger = NLog.LogManager.GetCurrentClassLogger()

    member x.NewPacketEvent = evt.Publish

    member private x.processPacketCompleteness(p : QueuedPacket, oldKey : uint32 option) = 
        locker.EnterWriteLock()
        if oldKey.IsSome then
            dict.Remove(oldKey.Value) |> ignore

        if p.IsPacketComplete() then
            let (pkt, rst) = 
                let rst = FFXIVBasePacket.TakePacket(p.Data)
                assert (rst.IsSome)
                rst.Value
            evt.Trigger(pkt)

            if rst.Length <> 0 then
                dict.Add(p.NextSeq, {p with Data = rst})
        else
            dict.Add(p.NextSeq, p)
        locker.ExitWriteLock()

    member private x.processPacketChain(p : QueuedPacket) = 
        locker.EnterUpgradeableReadLock()
        if  dict.ContainsKey(p.SeqNum)   then //发包顺序 A -> B 
            let np = dict.[p.SeqNum] + p
            x.processPacketCompleteness(np, Some(p.SeqNum))
            logger.Trace(sprintf "Forward packet: %s" (np.ToString()))
        elif dict.ContainsKey(p.NextSeq) then  //发包顺序 B -> A
            let np =  p + dict.[p.NextSeq]
            x.processPacketCompleteness(np, Some(p.NextSeq))
            logger.Trace(sprintf "Reverse packet: %s" (np.ToString()))
        else //单包完整
            x.processPacketCompleteness(p, None)
        locker.ExitUpgradeableReadLock()

    member x.Enqueue(tcp : TcpDatagram) =
        let p = QueuedPacket.FromTcpDatagram(tcp)
        logger.Trace(sprintf "Current dict : %A, current seq:%i next seq : %i \r\n%s" dict.Keys p.SeqNum p.NextSeq (Utils.HexString.toHex(p.Data)))
        if p.IsPacketComplete() then
            evt.Trigger(p.Data)
        else
            x.processPacketChain(p)

let queue = 
    let q = new GamePacketQueueV3()
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

    
    match ip with
    | Income when isGamePacket(ip.Tcp) -> 
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
    