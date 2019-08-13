namespace LibFFXIV.Network.SpecializedPacket
open LibFFXIV.Network.Utils

[<CLIMutableAttribute>]
type MarketRecord = 
    {
        OrderID     : uint32
        Unknown1    : uint32
        RetainerID  : uint64
        UserID      : uint64
        SignUserID  : uint64 //道具制作者签名
        Price       : uint32
        Unknown2    : uint32
        Count       : uint32
        Itemid      : uint32
        ///最后访问雇员的日期
        TimeStamp   : uint32
        Unknown3    : byte [] //24 byte unknown
        Name        : string  //32 byte zero-ter UTF8雇员名称
        IsHQ        : bool    // 1 byte
        MeldCount   : byte    // 1 byte
        Market      : byte    // 1 byte
        Unknown4    : byte    // 1 byte
    }

    static member private zerosA = ""

    static member ParseFromBytes(data : byte []) = 
        Logger.Log<MarketRecord>(data |> HexString.ToHex)
        use r  = XIVBinaryReader.FromBytes(data)
        {
            OrderID      = r.ReadUInt32()
            Unknown1     = r.ReadUInt32()
            RetainerID   = r.ReadUInt64()
            UserID       = r.ReadUInt64()
            SignUserID   = r.ReadUInt64()
            Price     = r.ReadUInt32()
            Unknown2  = r.ReadUInt32()
            Count     = r.ReadUInt32()
            Itemid    = r.ReadUInt32()
            TimeStamp = r.ReadUInt32()
            Unknown3  = r.ReadBytes(24)
            Name      = r.ReadFixedUTF8(32)
            IsHQ      = 
                let unknown5 = r.ReadBytes(32)
                if (unknown5 |> Array.sum) <> 0uy then 
                    System.Console.WriteLine("MarketRecord Unknown5 = {0}", HexString.ToHex(unknown5))
                r.ReadByte() = 1uy
            MeldCount = r.ReadByte()
            Unknown4  = r.ReadByte()
            Market    = 
                let market = r.ReadByte()
                let unknown6 = r.ReadRestBytes()
                if (unknown6 |> Array.sum) <> 0uy then 
                    System.Console.WriteLine("MarketRecord Unknown6 = {0}", HexString.ToHex(unknown6))
                market
        }

type MarketPacket =
    {
        Records : MarketRecord []
        NextIdx : byte
        CurrIdx : byte
        Unknown : byte [] // 6bytes
    }

    static member private logger = NLog.LogManager.GetCurrentClassLogger()

    static member private recordSize = 304/2 //112 detla = 40

    static member ParseFromBytes(data : ByteArray) = 
        use r = data.GetReader()
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
