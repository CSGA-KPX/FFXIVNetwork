namespace LibFFXIV.SpecializedPacket
open Microsoft.FSharp.Core.Operators.Checked
open System
open System.IO
open System.IO.Compression
open System.Text
open LibFFXIV.Constants
open LibFFXIV.Utils


type TradeLogRecord = 
    {
        ItemID      : uint32
        Price       : uint32
        TimeStamp   : uint32
        Count       : uint32
        IsHQ        : bool
        Unknown     : byte
        BuyerName  : string
    }
    static member ParseFromBytes(bytes : byte[]) = 
        [|
            let recordSize = 52
            let chunks = 
                bytes 
                |> Array.chunkBySize recordSize
                |> Array.filter (fun x -> x.Length = recordSize)
                |> Array.filter (fun x -> IsByteArrayAllZero(x))
            for chunk in chunks do
                use ms = new MemoryStream(chunk)
                use r  = new BinaryReader(ms)
                yield {
                    ItemID      = r.ReadUInt32()
                    Price       = r.ReadUInt32()
                    TimeStamp   = r.ReadUInt32()
                    Count       = r.ReadUInt32()
                    IsHQ        = r.ReadByte() = 1uy
                    Unknown     = r.ReadByte()
                    BuyerName  = 
                                    let bytes = r.ReadBytes(34)
                                    Encoding.UTF8.GetString(bytes.[0 .. (Array.findIndex ((=) 0uy) bytes) - 1])
                }
        |]

type TradeLogPacket = 
    {
        ItemID : uint32
        Records: TradeLogRecord []
    }
    static member ParseFromBytes(bytes : byte[]) = 
        use ms = new MemoryStream(bytes)
        use r  = new BinaryReader(ms)
        let itemId = r.ReadUInt32()
        let restBytes = 
            let bs = r.BaseStream
            let len = bs.Length - bs.Position
            r.ReadBytes(len |> int)
        let records= TradeLogRecord.ParseFromBytes(restBytes)

        {
            ItemID  = itemId
            Records = records
        }

type MarketRecord = 
    {
        Unknown1 : byte [] //32 byte unknown
        Price    : uint32
        Unknown2 : uint32
        Count    : uint32
        Itemid   : uint32
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
        CurrIdx : byte
        PrevIdx : byte
        Unknown : bytes // 6bytes
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
            CurrIdx = rst.Value.[0]
            PrevIdx = rst.Value.[1]
            Unknown = rst.Value.[2..]
        }

type MarketQueueItem(packet : MarketPacket) = 
    inherit GeneralQueueItem<byte, MarketRecord[]>(packet.PrevIdx, packet.CurrIdx, packet.Records)

    override x.IsFirst() = 
        //TODO:
        x.Current = 0uy

    override x.IsCompleted() = false
    
    override x.IsExpired(ref)  = false