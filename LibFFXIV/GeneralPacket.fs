namespace LibFFXIV.GeneralPacket
open Microsoft.FSharp.Core.Operators.Checked
open System
open System.IO
open System.IO.Compression
open LibFFXIV.Constants
open LibFFXIV.Utils


type FFXIVGamePacket = 
    {
        Magic     : uint16
        Opcode    : uint16
        Unknown1  : uint32
        TimeStamp : TimeStamp
        Unknown2  : uint32
        Data      : bytes
    }


    static member ParseFromBytes(bytes : byte[]) = 
        use r = XIVBinaryReader.FromBytes(bytes)
        {
            Magic    = r.ReadUInt16()
            Opcode   = r.ReadUInt16()
            Unknown1 = r.ReadUInt32()
            TimeStamp= r.ReadTimeStampSec()
            Unknown2 = r.ReadUInt32()
            Data     = r.ReadRestBytes()
        }

    static member private logger = NLog.LogManager.GetCurrentClassLogger()

type FFXIVSubPacket = 
    {
        Size     : uint32
        SourceId : uint32
        TargetId : uint32
        Type     : uint16
        Unknown  : uint16
        Data     : bytes
    }
    static member private logger = NLog.LogManager.GetCurrentClassLogger()
    
    member x.IsGamePacket() = 
        x.Type = 0x0003us

    member x.TryGetGamePacket() = 
        if x.IsGamePacket() then
            FFXIVGamePacket.ParseFromBytes(x.Data)
        else
            failwithf "This subpacket is not game packet"

    static member Parse (bytes : byte []) = 
        [|
            use r = XIVBinaryReader.FromBytes(bytes)
            
            while not (r.IsEnd()) do
                yield { Size     = r.ReadUInt32()
                        SourceId = r.ReadUInt32()
                        TargetId = r.ReadUInt32()
                        Type     = r.ReadUInt16()
                        Unknown  = r.ReadUInt16()
                        Data     = r.ReadRestBytes() }

            assert (r.IsEnd())
        |]


//00~0F Magic = 5252a041ff5d46e27f2a644d7b99c475
//10~17 Timestamp
//18~19 PacketSize (Including Magic)
//1A~1D Unknown //00 00 00 00
//1E~1F MessageChunks
//20~21 Encoding 01 01
//22~27 unknown
type FFXIVBasePacket = 
    {
        Magic     : string // 16byte header
        Timestamp : TimeStamp
        PacketSize: uint16
        Unknown   : uint32
        ChunkCount: uint16
        Encoding  : uint16
        SubPackets: FFXIVSubPacket []
    }

    static member private logger = NLog.LogManager.GetCurrentClassLogger()

    static member TakePacket(bytes : byte []) = 
        let size = FFXIVBasePacket.GetPacketSize(bytes)
        if  size <= 0 || (size > bytes.Length) then
            None
        else
            let p = bytes.[0 .. size - 1]
            let r = bytes.[size .. ]
            Some((p, r))

    static member GetPacketSize(bytes : byte[]) = 
        if   bytes.Length = 0    then    0
        elif bytes.Length < 0x1A then   -1
        else
            let size = BitConverter.ToUInt16(bytes, 0x18) |> int
            if size > 0x10000 then
                FFXIVBasePacket.logger.Error(sprintf "Packet size too large(%i). " size)
                failwithf "Packet size too large(%i). " size
            size

    static member ParseFromBytes(bytes : byte[]) = 
        use r = XIVBinaryReader.FromBytes(bytes)
        
        let magicHex = r.ReadHexString(16)
        let isNormalMagic = magicHex = LibFFXIV.Constants.FFXIVBasePacketMagic
        let isAltMagic    = magicHex = LibFFXIV.Constants.FFXIVBasePacketMagicAlt
        if not (isNormalMagic || isAltMagic) then
            failwithf "Packet magic mismatch! %s : %s" magicHex (HexString.ToHex(bytes))

        let time = r.ReadTimeStampMillisec()
        let packetSize= r.ReadUInt16()
        let unknown = r.ReadUInt32()
        let chunkCount= r.ReadUInt16()
        let encoding = r.ReadUInt16()
        
        let payload = 
            match encoding with
            | 0us | 1us ->
                r.ReadBytes(6) |> ignore
                r.ReadRestBytes()
            | _ ->
                r.ReadBytes(8) |> ignore
                use ms = new MemoryStream(r.ReadRestBytes())
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

        assert (r.IsEnd())

        {
            Magic     = magicHex
            Timestamp = time
            PacketSize= packetSize
            Unknown   = unknown
            ChunkCount= chunkCount
            Encoding  = encoding
            SubPackets= subPackets
        }