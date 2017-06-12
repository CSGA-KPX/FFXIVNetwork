module LibFFXIV.TcpPacket
open System
open LibFFXIV.Utils
open LibFFXIV.GeneralPacket

type PacketQueueItem (cur, next, data) = 
    inherit GeneralQueueItem<uint32, byte []>(cur, next, data)

    member x.FullPacketSize() = 
        FFXIVBasePacket.GetPacketSize(x.Data)

    override x.IsExpired(refSeq) = 
        refSeq - x.Current > 4u*1024u

    override x.IsFirst()  = 
        if x.Data.Length < 0x10 then
            false
        else
            let magic = x.Data.[0 .. 15]
            HexString.ToHex(magic) = LibFFXIV.Constants.FFXIVBasePacketMagic

    override x.IsCompleted() =
        x.IsFirst() && (x.Data.Length >= x.FullPacketSize())

type GamePacketQueue() = 
    inherit GeneralPacketReassemblyQueue<uint32, PacketQueueItem, byte [], byte []>()

    override x.combineItemData (a, b) = 
        Array.append a b

    override x.processPacketCompleteness(p) = 
        if p.IsCompleted() then
            let rec yieldPacket (rest) = 
                let rst = FFXIVBasePacket.TakePacket(rest)
                if rst.IsNone then
                    rest
                else
                    let (p, rst) = rst.Value
                    x.evt.Trigger(p)
                    yieldPacket(rst)
            let rst = yieldPacket(p.Data)
            if rst.Length <> 0 then
                x.logger.Trace(sprintf "Rst<>0, Added nsq:%i data:%s" (p.Current) (HexString.ToHex(rst)))
                p.Data <- rst
                x.dict.Add(p.Current, p)
        else
            x.logger.Trace(sprintf "NewPkt, Added nsq:%i" (p.Current))
            x.dict.Add(p.Current, p)