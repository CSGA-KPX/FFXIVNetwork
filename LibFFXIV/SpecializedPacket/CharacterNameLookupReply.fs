namespace LibFFXIV.Network.SpecializedPacket
open System
open LibFFXIV.Network.Utils

type CharacterNameLookupReply = 
    {
        UserID   : uint64
        Username : string
    }

    static member ParseFromBytes(data : ByteArray) = 
        use r = data.GetReader()
        {
            UserID   = r.ReadUInt64()
            Username = r.ReadFixedUTF8(32)
        }