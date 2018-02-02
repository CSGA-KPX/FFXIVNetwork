namespace LibFFXIV.Network.SpecializedPacket
open System
open LibFFXIV.Network.Utils

type CharaSelectReply = 
    {
        Sequence    : uint64
        ActorId     : uint32
        CharacterId : uint64
        //Padding0    : uint32
        SessionToken: string
        WorldPort   : uint16
        WorldIP     : string
        Ticket      : byte[]
    }

    static member ParseFromBytes(bytes : byte []) = 
        use r = XIVBinaryReader.FromBytes(bytes)
        let seq = r.ReadUInt64()
        let act = r.ReadUInt32()
        r.ReadUInt32() |> ignore // 4byte padding
        let chr = r.ReadUInt64()
        r.ReadUInt32() |> ignore // 4byte padding
        let ses = r.ReadFixedUTF8(66)
        let port = r.ReadUInt16()
        let ip  = r.ReadFixedUTF8(48)
        let ticket = r.ReadRestBytes()
        {
            Sequence    = seq
            ActorId     = act
            CharacterId = chr
            SessionToken= ses
            WorldPort   = port
            WorldIP     = ip
            Ticket      = ticket
        }