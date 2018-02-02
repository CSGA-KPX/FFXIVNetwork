namespace LibFFXIV.Network.BasePacket
open LibFFXIV.Network.Utils

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