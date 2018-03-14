namespace LibFFXIV.Network.BasePacket
open LibFFXIV.Network.Utils

type FFXIVGamePacket = 
    {
        Magic     : uint16
        Opcode    : uint16
        Unknown1  : uint32
        TimeStamp : TimeStamp
        Unknown2  : uint32
        Data      : ByteArray
    }


    static member ParseFromBytes(data : ByteArray) = 
        use r = data.GetReader()
        {
            Magic    = r.ReadUInt16()
            Opcode   = r.ReadUInt16()
            Unknown1 = r.ReadUInt32()
            TimeStamp= r.ReadTimeStampSec()
            Unknown2 = r.ReadUInt32()
            Data     = new ByteArray(r.ReadRestBytes())
        }

    static member private logger = NLog.LogManager.GetCurrentClassLogger()