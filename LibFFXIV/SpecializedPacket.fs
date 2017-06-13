module LibFFXIV.SpecializedPacket
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
        NextIdx : byte
        CurrIdx : byte
        Unknown : bytes // 6bytes
    }

    interface IQueueableItem<byte, MarketPacket> with
        member x.QueueCurrentIdx = x.CurrIdx
        member x.QueueNextIdx    = x.NextIdx
        member x.QueueData       = x

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

let tester (ra : MarketRecord []) = 
    let sb = (new StringBuilder()).AppendFormat("====MarketData====\r\n")
    
    for data in ra do
        let price = data.Price
        let count = data.Count
        let item  = LibFFXIV.Database.XIVItemDict.[data.Itemid |> int].ToString()
        let isHQ  = data.IsHQ
        let meld  = data.MeldCount
        let str = sprintf "%s P:%i C:%i HQ:%b Meld:%i Seller:%s" item price count isHQ meld (data.Name)
        sb.AppendLine(str) |> ignore
    sb.AppendLine("====MarketDataEnd====") |> ignore
    NLog.LogManager.GetCurrentClassLogger().Info(sb.ToString())

type MarketQueue private () = 
    inherit GeneralPacketReassemblyQueue<byte, MarketPacket, MarketRecord []>()

    static let instance = 
        let i  = new MarketQueue()
        i.NewCompleteDataEvent.Add(tester)
        i

    override x.processPacketCompleteness(p) = 
        let qItem = p :> IQueueableItem<byte, MarketPacket>
        if qItem.IsCompleted() then
            let rs = p.Records |> Array.sortBy (fun x -> x.Price)
            x.OnCompleted(rs)
        else
            printfn "NewPkt, Added curr:%i, next:%i" (qItem.QueueCurrentIdx) (qItem.QueueNextIdx)
            x.dict.Add(qItem.QueueCurrentIdx, p)

    static member Instance = instance