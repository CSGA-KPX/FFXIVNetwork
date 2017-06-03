module FFXIV.PacketTypes
open Microsoft.FSharp.Core.Operators.Checked
open System
open System.IO
open System.IO.Compression
open System.Text


type Opcodes = 
    | TradeLog = 0x00C7us
    | Market   = 0x00C3us


type TradeLogRecord = 
    {
        ItemID      : uint32
        Price       : uint32
        TimeStamp   : uint32
        Count       : uint32
        Unknown2    : uint16
        SellerName  : string
    }

    static member Parse(r : BinaryReader) = 
        [|
            let recordSize = 52
            let bs = r.BaseStream
            while bs.Length - bs.Position >= 52L do
                yield {
                    ItemID      = r.ReadUInt32()
                    Price       = r.ReadUInt32()
                    TimeStamp   = r.ReadUInt32()
                    Count       = r.ReadUInt32()
                    Unknown2    = r.ReadUInt16()
                    SellerName  = 
                                    let bytes = r.ReadBytes(34)
                                    Encoding.UTF8.GetString(bytes.[0 .. (Array.findIndex ((=) 0uy) bytes) - 1])
                }
        |]

type TradeLogPacket = 
    {
        ItemID : uint32
        Records: TradeLogRecord []
        Unknown: byte []
    }
    static member ParseFromBytes(bytes : byte[]) = 
        use ms = new MemoryStream(bytes)
        use r  = new BinaryReader(ms)
        let itemId = r.ReadUInt32()
        let records= TradeLogRecord.Parse(r)
        let unknown= r.ReadBytes(4)
        
        {
            ItemID  = itemId
            Records = records
            Unknown = unknown
        }

type MarketArea = 
  | LimsaLominsa = 0x0001
  | Gridania     = 0x0002
  | Uldah        = 0x0003
  | Ishgard      = 0x0004 //未确认

type MarketPacket = 
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
        [|
            let recordSize = 112
            let chunks = 
                bytes 
                |> Array.chunkBySize recordSize
                |> Array.filter (fun x -> x.Length = 112)
                |> Array.filter (fun x -> Utils.IsByteArrayAllZero(x))
            for chunk in chunks do
                use ms = new MemoryStream(chunk)
                use r  = new BinaryReader(ms)
                yield {
                    Unknown1  = r.ReadBytes(32)
                    Price     = r.ReadUInt32()
                    Unknown2  = r.ReadUInt32()
                    Count     = r.ReadUInt32()
                    Itemid    = r.ReadUInt32()
                    TimeStamp = r.ReadUInt32()
                    Unknown3  = r.ReadBytes(24)
                    Name      =
                                let bytes = r.ReadBytes(32)
                                Encoding.UTF8.GetString(bytes.[0 .. (Array.findIndex ((=) 0uy) bytes) - 1])
                    IsHQ      = r.ReadByte() = 1uy
                    MeldCount = r.ReadByte()
                    Market    = r.ReadByte()
                    Unknown4  = r.ReadByte()
                }
        |]

type FFXIVGamePacket = 
    {
        Magic     : uint16
        Opcode    : uint16
        Unknown1  : uint32
        TimeStamp : uint32
        Unknown2  : uint32
        Data      : byte [] 
    }

    static member ParseFromBytes(bytes : byte[]) = 
        use ms = new MemoryStream(bytes)
        use r  = new BinaryReader(ms)
        let magic = r.ReadUInt16()
        assert (magic = 0x0014us)
        let opcode = r.ReadUInt16()
        let unk1 = r.ReadUInt32()
        let ts   = r.ReadUInt32()
        let unk2 = r.ReadUInt32()
        let data = r.ReadBytes(bytes.Length - 0x10)

        {
            Magic    = magic
            Opcode   = opcode
            Unknown1 = unk1
            TimeStamp= ts
            Unknown2 = unk2
            Data     = data
        }

type FFXIVSubPacket = 
    {
        Size : uint32
        SourceId : uint32
        TargetId : uint32
        Type : uint16
        Unknown  : uint16
        Data     : byte []
    }

    static member Parse (bytes : byte []) = 
        [|
            use ms = new MemoryStream(bytes)
            use r  = new BinaryReader(ms)
            
            while not (Utils.IsBinaryReaderEnd(r)) do
                let len = r.ReadUInt32()
                let sid = r.ReadUInt32()
                let did = r.ReadUInt32()
                let typ = r.ReadUInt16()
                let unk = r.ReadUInt16()
                let headerLength = 16 // (16+16+32*3)/8
                let data= r.ReadBytes((int len) - headerLength)
                
                yield { Size = len
                        Type = typ
                        SourceId = sid
                        TargetId = did
                        Unknown  = unk
                        Data = data }
            assert (Utils.IsBinaryReaderEnd(r))
        |]


//00~0F Magic = 5252a041ff5d46e27f2a644d7b99c475
//10~17 Timestamp
//18~1B PacketSize (Including Magic)
//1C~1D Unknown //00 00
//1E~1F MessageChunks
//20~21 Encoding 01 01
//22~27 unknown
type FFXIVBasePacket = 
    {
        Magic     : byte [] // 16byte header
        Timestamp : DateTime
        PacketSize: uint16
        Unknown   : uint32
        ChunkCount: uint16
        Encoding  : uint16
        SubPackets: FFXIVSubPacket []
    }

    static member TakePacket(bytes : byte []) = 
        let size = BitConverter.ToUInt16(bytes, 24) |> int

        if size = 0 || size > 0x10000 || (size > bytes.Length) then
            None
        else
            let p = bytes.[0 .. size - 1]
            let r = bytes.[size .. ]
            Some((p, r))

    static member ParseFromBytes(bytes : byte[]) = 
        use ms = new MemoryStream(bytes)
        use r = new BinaryReader(ms)
        
        let magic = r.ReadBytes(16)
        assert (Utils.HexString.toHex(magic) = Utils.FFXIVBasePacketMagic)
        let time = 
            (new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc))
                .AddMilliseconds(r.ReadUInt64() |> float)
                .ToLocalTime()
        let packetSize= r.ReadUInt16()
        let unknown = r.ReadUInt32()
        let chunkCount= r.ReadUInt16()
        let encoding = r.ReadUInt16()
        
        let payload = 
            let headerLength = 34
            match encoding with
            | 0us | 1us ->
                r.ReadBytes(6) |> ignore
                failwithf "untest method!"
                r.ReadBytes(bytes.Length - headerLength - 6)
                
            | _ ->
                r.ReadBytes(8) |> ignore
                r.ReadBytes(bytes.Length - headerLength - 8)
        let realPayload = 
            use ms = new MemoryStream(payload)
            use ds = new DeflateStream(ms, CompressionMode.Decompress)
            let buf = Array.zeroCreate 0x10000
            let messageLength = ds.Read(buf, 0, buf.Length)
            if messageLength = 0 then
                failwithf ""
            buf.[ 0 .. messageLength - 1]
        
        let subPackets = FFXIVSubPacket.Parse(realPayload) 

        assert (Utils.IsBinaryReaderEnd(r))
        
        {
            Magic     = magic
            Timestamp = time
            PacketSize= packetSize
            Unknown   = unknown
            ChunkCount= chunkCount
            Encoding  = encoding
            SubPackets= subPackets
        }