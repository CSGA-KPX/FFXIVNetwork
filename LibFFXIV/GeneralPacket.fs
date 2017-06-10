namespace LibFFXIV.GeneralPacket
open Microsoft.FSharp.Core.Operators.Checked
open System
open System.IO
open System.IO.Compression
open System.Text
open LibFFXIV.Constants
open LibFFXIV.Utils


type FFXIVGamePacket = 
    {
        Magic     : uint16
        Opcode    : uint16
        Unknown1  : uint32
        TimeStamp : TimeStamp
        Unknown2  : uint32
        Data      : byte [] 
    }

    static member ParseFromBytes(bytes : byte[]) = 
        use ms = new MemoryStream(bytes)
        use r  = new BinaryReader(ms)
        let magic = r.ReadUInt16()
        //似乎如果游戏已经登陆，那么magic是0x0014
        //assert (magic = 0x0014us)
        let opcode = r.ReadUInt16()
        let unk1 = r.ReadUInt32()
        let ts   = TimeStamp.FromSeconds(r.ReadUInt32())
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

    static member private logger = NLog.LogManager.GetCurrentClassLogger()

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
            
            while not (IsBinaryReaderEnd(r)) do
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
            assert (IsBinaryReaderEnd(r))
        |]

    static member private logger = NLog.LogManager.GetCurrentClassLogger()


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
        Timestamp : TimeStamp
        PacketSize: uint16
        Unknown   : uint32
        ChunkCount: uint16
        Encoding  : uint16
        SubPackets: FFXIVSubPacket []
    }

    static member private logger = NLog.LogManager.GetCurrentClassLogger()

    static member TakePacket(bytes : byte []) = 
        if bytes.Length < 24 then
            None
        else
            let size = BitConverter.ToUInt16(bytes, 24) |> int

            if size = 0 || size > 0x10000 || (size > bytes.Length) then
                None
            else
                let p = bytes.[0 .. size - 1]
                let r = bytes.[size .. ]
                Some((p, r))

    static member GetPacketSize(bytes : byte[]) = 
        if not (bytes.Length >= 0x1B) then
            failwith "Packet too short"

        use ms = new MemoryStream(bytes)
        use r = new BinaryReader(ms)
        
        let magic = r.ReadBytes(16)
        if not (HexString.ToHex(magic) = LibFFXIV.Constants.FFXIVBasePacketMagic) then
            failwith "Magic not match"
        let time = 
            TimeStamp.FromMilliseconds(r.ReadUInt64())

        r.ReadUInt16() |> int32

    static member ParseFromBytes(bytes : byte[]) = 
        use ms = new MemoryStream(bytes)
        use r = new BinaryReader(ms)
        
        let magic = r.ReadBytes(16)
        let magicStr = magic |> HexString.ToHex
        let isNormalMagic = magicStr = LibFFXIV.Constants.FFXIVBasePacketMagic
        let isAltMagic    = magicStr = LibFFXIV.Constants.FFXIVBasePacketMagicAlt
        if not (isNormalMagic || isAltMagic) then
            failwithf "Packet magic mismatch! %s : %s" magicStr (HexString.ToHex(bytes))

        let time = 
            TimeStamp.FromMilliseconds(r.ReadUInt64())

        let packetSize= r.ReadUInt16()
        let unknown = r.ReadUInt32()
        let chunkCount= r.ReadUInt16()
        let encoding = r.ReadUInt16()
        
        let payload = 
            let headerLength = 34
            match encoding with
            | 0us | 1us ->
                r.ReadBytes(6) |> ignore
                r.ReadBytes(bytes.Length - headerLength - 6)
            | _ ->
                r.ReadBytes(8) |> ignore
                let encoded = r.ReadBytes(bytes.Length - headerLength - 8)
                use ms = new MemoryStream(encoded)
                use ds = new DeflateStream(ms, CompressionMode.Decompress)
                let buf = Array.zeroCreate 0x10000
                let messageLength = ds.Read(buf, 0, buf.Length)
                if messageLength = 0 then
                    [| |]
                else
                    buf.[ 0 .. messageLength - 1]

        let subPackets = 
            if payload.Length >= 0x10 then
                FFXIVSubPacket.Parse(payload) 
            else
                [| |]

        assert (IsBinaryReaderEnd(r))
        
        {
            Magic     = magic
            Timestamp = time
            PacketSize= packetSize
            Unknown   = unknown
            ChunkCount= chunkCount
            Encoding  = encoding
            SubPackets= subPackets
        }