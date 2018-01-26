namespace LibFFXIV.SpecializedPacket
open System.IO
open LibFFXIV.Utils

type MarketRecord = 
    {
        Unknown1 : byte [] //32 byte unknown
        Price    : uint32
        Unknown2 : uint32
        Count    : uint32
        Itemid   : uint32
        ///最后访问雇员的日期
        TimeStamp: uint32
        Unknown3 : byte [] //24 byte unknown
        Name     : string  //32 byte zero-ter UTF8雇员名称
        IsHQ     : bool    // 1 byte
        MeldCount: byte    // 1 byte
        Market   : byte    // 1 byte
        Unknown4 : byte    // 1 byte
    }

    static member ParseFromBytes(bytes : byte[]) = 
        use r  = XIVBinaryReader.FromBytes(bytes)
        {
            Unknown1  = r.ReadBytes(32)
            Price     = r.ReadUInt32()
            Unknown2  = r.ReadUInt32()
            Count     = r.ReadUInt32()
            Itemid    = r.ReadUInt32()
            TimeStamp = r.ReadUInt32()
            Unknown3  = r.ReadBytes(24)
            Name      = r.ReadFixedUTF8(32)
            IsHQ      = r.ReadByte() = 1uy
            MeldCount = r.ReadByte()
            Market    = r.ReadByte()
            Unknown4  = r.ReadByte()
        }

type MarketPacket =
    {
        Records : MarketRecord []
        NextIdx : byte
        CurrIdx : byte
        Unknown : bytes // 6bytes
    }

    interface IQueueableItem<byte, MarketPacket> with
        member x.QueueCurrentIdx = x.CurrIdx
        member x.QueueNextIdx    = x.NextIdx

        member x.IsCompleted () = 
            x.CurrIdx = x.NextIdx

        member x.IsExpried   (ref) = 
            false
    
        member x.Combine     (y)   = 
            {
                Records = Array.append x.Records y.Records
                NextIdx = y.NextIdx
                CurrIdx = x.CurrIdx
                Unknown = x.Unknown
            }

    static member private logger = NLog.LogManager.GetCurrentClassLogger()

    static member private recordSize = 112

    static member ParseFromBytes(bytes : byte[]) = 
        use r = XIVBinaryReader.FromBytes(bytes)
        let (chks, rst) = r.ReadRestBytesAsChunk(MarketPacket.recordSize, true)

        if rst.IsNone then
            let errormsg = sprintf "Must have tail bytes!"
            MarketPacket.logger.Error(errormsg)
            failwithf "%s" errormsg

        let records = 
            [|
                for chk in chks do
                    yield MarketRecord.ParseFromBytes(chk)
            |]
        
        {
            Records = records
            NextIdx = rst.Value.[0]
            CurrIdx = rst.Value.[1]
            Unknown = rst.Value.[2..]
        }



type MarketQueue () = 
    inherit GeneralPacketReassemblyQueue<byte, MarketPacket, MarketRecord []>()

    override x.preProcessPacketChain(p : MarketPacket) = 
        let qItem = p :> IQueueableItem<byte, MarketPacket>
        let isFirstPacket = qItem.QueueCurrentIdx = 0uy && qItem.QueueNextIdx = 10uy
        let isDictNotEmpty= x.dict.Count <> 0
        if isFirstPacket && isDictNotEmpty then
            x.logger.Error("Got start packet while dict is not empty. dict.Clear()")
            x.dict.Clear()

    override x.processPacketCompleteness(p) = 
        let qItem = p :> IQueueableItem<byte, MarketPacket>
        if qItem.IsCompleted() then
            let rs = p.Records |> Array.sortBy (fun x -> x.Price)
            x.OnCompleted(rs)
        else
            //printfn "NewPkt, Added curr:%i, next:%i" (qItem.QueueCurrentIdx) (qItem.QueueNextIdx)
            x.dict.Add(qItem.QueueCurrentIdx, p)