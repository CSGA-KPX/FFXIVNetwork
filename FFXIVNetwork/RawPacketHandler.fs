module RawPacketHandler
open LibFFXIV.TcpPacket
open PacketDotNet
open FFXIV.PacketHandlerBase

let RawPacketLogger = NLog.LogManager.GetLogger("RawTCPPacket")

let handler = new PacketHandler()

let incomePacketQueue = 
    let q = new LibFFXIV.TcpPacket.GamePacketQueue()
    q.NewCompleteDataEvent.Add(handler.HandlePacket)
    q

let outcomePacketQueue = 
    let q = new LibFFXIV.TcpPacket.GamePacketQueue()
    q.NewCompleteDataEvent.Add(handler.HandlePacket)
    q

//    Payload len = 0
// or Start with 0x00 *  24
let isGameBasePacket(tcp : TcpPacket) =
    let len = tcp.PayloadData.Length
    if len = 0 then
        false
    else 
        true
        //let payload     = tcp.PayloadData
        //let first16Str  = lazy (Utils.HexString.ToHex(tcp.PayloadData.[0..15]).ToUpper() )
        //(len <= 32) || (first16Str.Value <> FFXIVBasePacketMagicAlt)


let PacketHandler (p:TcpPacket) = 
    let ip = p.ParentPacket :?> IPv4Packet
    let tcp= p

    let (|Income|Outcome|Miss|) (ip : IPv4Packet) = 
        let remoteAddress = ip.DestinationAddress.ToString()
        let  localAddress = ip.SourceAddress.ToString()
        if   GlobalVars.ServerIpToWorld.ContainsKey(remoteAddress) then
            Outcome GlobalVars.ServerIpToWorld.[remoteAddress]
        elif GlobalVars.ServerIpToWorld.ContainsKey(localAddress)  then
            Income GlobalVars.ServerIpToWorld.[localAddress]
        else
            Miss

    let seqNum = tcp.SequenceNumber
    let nsqNum = seqNum + ( tcp.PayloadData.Length |> uint32)
    match ip with
    | Income world when isGameBasePacket(tcp) -> 
        let data = tcp.PayloadData
        RawPacketLogger.Trace(sprintf "<<<<<<%i,%i,%s" (seqNum) (nsqNum) (data |> Utils.HexString.ToHex))
        incomePacketQueue.Enqueue(
            {
                SeqNum    = seqNum
                NextSeq   = nsqNum
                Data      = data
                World     = world
                Direction = LibFFXIV.TcpPacket.PacketDirection.In
            })
    | Outcome world when isGameBasePacket(tcp) ->
        let data = tcp.PayloadData
        RawPacketLogger.Trace(sprintf ">>>>>>%i,%i,%s" (seqNum) (nsqNum) (data |> Utils.HexString.ToHex))
        outcomePacketQueue.Enqueue(
            {
                SeqNum    = seqNum
                NextSeq   = nsqNum
                Data      = data
                World     = world
                Direction = LibFFXIV.TcpPacket.PacketDirection.Out
            })

    | _    -> ()