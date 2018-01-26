module LibFFXIV.TcpPacket
open System
open LibFFXIV.Utils
open LibFFXIV.BasePacket

type PacketDirection = 
    | In
    | Out 

type ConnectionInfo = 
    | Game of PacketDirection * SpecializedPacket.World
    | Lobby of PacketDirection

type QueuedPacket =  
    {   
        SeqNum    : uint32
        NextSeq   : uint32
        Data      : byte []
        World     : SpecializedPacket.World
        Direction : PacketDirection
    }

    member x.IsFirstPacket() = 
        if x.Data.Length < 0x10 then
            false
        else
            let magic = x.Data.[0 .. 15]
            (Utils.HexString.ToHex(magic) = LibFFXIV.Constants.FFXIVBasePacketMagic)
            || (Utils.HexString.ToHex(magic) = LibFFXIV.Constants.FFXIVBasePacketMagicAlt)

    member x.IsNextPacket(y) = 
        x.NextSeq = y.SeqNum

    member x.FullPacketSize() = 
        FFXIVBasePacket.GetPacketSize(x.Data)

    interface IQueueableItem<uint32, QueuedPacket> with
        member x.QueueCurrentIdx = x.SeqNum
        member x.QueueNextIdx    = x.NextSeq

        member x.IsCompleted() =
            x.IsFirstPacket() && (x.Data.Length >= x.FullPacketSize())

        member x.IsExpried(refSeq) = 
            refSeq - x.SeqNum > 4096u

        member x.Combine(y : QueuedPacket) = 
            if x.World <> y.World then
                failwithf "无法混合不同服务器数据包"
            if x.Direction <> y.Direction then
                failwithf "数据包方向不匹配"

            {
                SeqNum   = x.SeqNum
                NextSeq  = y.NextSeq
                Data     = Array.append x.Data y.Data
                World    = x.World
                Direction= x.Direction
            }

type GamePacketQueue() = 
    inherit GeneralPacketReassemblyQueue<uint32, QueuedPacket, QueuedPacket>()

    override x.processPacketCompleteness(packet) = 
        let qItem = packet :> IQueueableItem<uint32, QueuedPacket>
        if qItem.IsCompleted() then
            let rec yieldPacket (rest) = 
                let rst = FFXIVBasePacket.TakePacket(rest)
                if rst.IsNone then
                    rest
                else
                    let (p, rst) = rst.Value
                    let p = {packet with SeqNum = 0u; NextSeq = 0u; Data = p}
                    x.OnCompleted(p)
                    yieldPacket(rst)
            let rst = yieldPacket(packet.Data)
            if rst.Length <> 0 then
                x.dict.Add(qItem.QueueNextIdx, {packet with Data = rst})
        else
            if x.dict.ContainsKey(qItem.QueueNextIdx) then
                x.dict.[qItem.QueueNextIdx] <- packet
            else
                x.dict.Add(qItem.QueueNextIdx, packet)