module LibFFXIV.TcpPacket
open Microsoft.FSharp.Core.Operators.Checked
open System
open LibFFXIV.Utils
open LibFFXIV.GeneralPacket

type QueuedPacket =  
    {   
        SeqNum  : uint32
        NextSeq : uint32
        Data    : byte []
    }

    member x.IsFirstPacket() = 
        if x.Data.Length < 0x10 then
            false
        else
            let magic = x.Data.[0 .. 15]
            Utils.HexString.ToHex(magic) = LibFFXIV.Constants.FFXIVBasePacketMagic

    member x.IsNextPacket(y) = 
        x.NextSeq = y.SeqNum

    member x.FullPacketSize() = 
        FFXIVBasePacket.GetPacketSize(x.Data)

    interface IQueueableItem<uint32, QueuedPacket> with
        member x.QueueCurrentIdx = x.SeqNum
        member x.QueueNextIdx    = x.NextSeq
        member x.QueueData       = x

        member x.IsCompleted() =
            x.IsFirstPacket() && (x.Data.Length >= x.FullPacketSize())

        member x.IsExpried(refSeq) = 
            refSeq - x.SeqNum > 4096u

        member x.Combine(y : QueuedPacket) = 
            {
                SeqNum   = x.SeqNum
                NextSeq  = y.NextSeq
                Data     = Array.append x.Data y.Data
            }

type GamePacketQueue() = 
    inherit GeneralPacketReassemblyQueue<uint32, QueuedPacket, byte []>()

    override x.processPacketCompleteness(p) = 
        let qItem = p :> IQueueableItem<uint32, QueuedPacket>
        if qItem.IsCompleted() then
            let rec yieldPacket (rest) = 
                let rst = FFXIVBasePacket.TakePacket(rest)
                if rst.IsNone then
                    rest
                else
                    let (p, rst) = rst.Value
                    x.OnCompleted(p)
                    yieldPacket(rst)
            let rst = yieldPacket(p.Data)
            if rst.Length <> 0 then
                x.logger.Trace(sprintf "Rst<>0, Added nsq:%i data:%s" (qItem.QueueCurrentIdx) (HexString.ToHex(rst)))
                x.dict.Add(qItem.QueueNextIdx, {p with Data = rst})
            else
                x.logger.Trace(sprintf "Rst =0")
        else
            x.logger.Trace(sprintf "NewPkt, Added nsq:%i" (qItem.QueueNextIdx))
            x.dict.Add(qItem.QueueNextIdx, p)