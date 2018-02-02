namespace LibFFXIV.Network.BasePacket
open System
open System.IO
open System.IO.Compression
open LibFFXIV.Network.BasePacket
open LibFFXIV.Network.Utils



//00~0F Magic = 5252a041ff5d46e27f2a644d7b99c475
//10~17 Timestamp
//18~19 PacketSize (Including Magic)
//1A~1D Unknown //00 00 00 00
//1E~1F MessageChunks
//20~21 Encoding 01 01
//22~27 unknown

type ConnectionChannel =
    | None  = 0
    | World = 1
    | Chat  = 2
    | Lobby = 3

type FFXIVBasePacket = 
    {
        Magic     : string // 16byte header
        Timestamp : TimeStamp //8byte
        PacketSize: uint32
        Channel   : uint16
        ChunkCount: uint16
        Encoding  : uint16
        Data      : byte []
    }

    member x.GetSubPackets() = 
        if x.Data.Length >= 0x10 then
            FFXIVSubPacket.Parse(x.Data) 
        else
            [| |]

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
        let isNormalMagic = magicHex = LibFFXIV.Network.Constants.FFXIVBasePacketMagic
        let isAltMagic    = magicHex = LibFFXIV.Network.Constants.FFXIVBasePacketMagicAlt
        if not (isNormalMagic || isAltMagic) then
            failwithf "Packet magic mismatch! %s : %s" magicHex (HexString.ToHex(bytes))

        let time = r.ReadTimeStampMillisec()
        let packetSize= r.ReadUInt32()
        let channel = r.ReadUInt16()
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

        assert (r.IsEnd())

        {
            Magic     = magicHex
            Timestamp = time
            PacketSize= packetSize
            Channel   = channel
            ChunkCount= chunkCount
            Encoding  = encoding
            Data      = payload
        }