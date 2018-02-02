namespace LibFFXIV.Network.SpecializedPacket
open System
open LibFFXIV.Network.Utils

type Character = 
    {
        UserId   : uint64
        WorldId  : uint16
        UserName : string
        WorldName: string
        InfoJson : string
    }

    static member ParseFromBytes (bytes : byte []) = 
        use r = XIVBinaryReader.FromBytes(bytes)
        r.ReadBytes(48) |> ignore
        let uid = r.ReadUInt64()
        r.ReadUInt64() |> ignore
        let wid = r.ReadUInt16()
        let name= r.ReadFixedUTF8(32)
        let sname = r.ReadFixedUTF8(32)
        let json = r.ReadFixedUTF8(1980)
        {
            UserId   = uid
            WorldId  = wid
            UserName = name
            WorldName= sname
            InfoJson = json
        }

type CharacterList = 
    {
        Header : byte []
        Charas : Character []
        Footer : byte []
    }

    static member ParseFromBytes (bytes : byte []) = 
        use r = XIVBinaryReader.FromBytes(bytes)
        let header         = r.ReadBytes(32)
        let (chks, footer) = r.ReadRestBytesAsChunk(1120, false)
        let characters = 
            [|
                for chk in chks do 
                    let chr = Character.ParseFromBytes(chk)
                    if chr.WorldId <> 0us then
                        yield chr
            |]

        {
            Header = header
            Charas = characters
            Footer = footer.Value
        }