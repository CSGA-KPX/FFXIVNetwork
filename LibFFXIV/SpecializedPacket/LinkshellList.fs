namespace LibFFXIV.Network.SpecializedPacket
open System
open LibFFXIV.Network.Utils


type LinkshellListRecord = 
    {
        UserID   : uint64
        ServerID : uint16
        UserName : string
    }

type LinkshellListPacket = 
    {
        Header  : byte []
        Records : LinkshellListRecord []
    }

    static member ParseFromBytes(data : ByteArray) = 
        use r = data.GetReader()

        let header = r.ReadBytes(8)
        let chunks, tail = r.ReadRestBytesAsChunk(72, false)

        let r = 
            [|
                if BitConverter.ToUInt64(header, 0) <> 0UL && (chunks.Length <> 0)  then
                    for chunk in chunks do
                        use r = XIVBinaryReader.FromBytes(chunk)
                        let userid = r.ReadUInt64()
                        let serverid = 
                            r.ReadBytes(16) |> ignore
                            r.ReadUInt16()
                        let username =
                            r.ReadBytes(7) |> ignore
                            r.ReadFixedUTF8(32)
                        if userid <> 0UL then
                            yield {UserID = userid; ServerID = serverid; UserName = username}
            |]
        {Header = header; Records = r}