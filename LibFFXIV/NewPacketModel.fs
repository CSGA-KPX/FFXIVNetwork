namespace LibFFXIV.Network.NewPacketModel
open System
open LibFFXIV.Network.Constants
open LibFFXIV.Network.Utils

[<AbstractClass>]
type XIVPacket() as x = 
    let logger = NLog.LogManager.GetLogger(x.GetType().Name)
    member internal x.Logger = logger
    
    abstract Read : XIVBinaryReader -> unit

    //member x.Paser<'T when 'T :> XIVPacket>(reader) = 
    //    let packet = Activator.CreateInstance(typeof<'T>) :?> XIVPacket
    //    packet.Read(reader)
    //    packet :?> 'T


type FFXIVSubPacket () = 
    inherit XIVPacket()

    member val Size     = 0u  with get, set
    member val SourceID = 0u  with get, set
    member val TargetID = 0u  with get, set
    member val Type     = PacketTypes.None with get, set
    member val Unknown  = 0us with get, set
    
    member internal x.isGamePacket() = 
        x.Type = PacketTypes.GameMessage 


    override x.Read(r) = 
        x.Size     <- r.ReadUInt32()
        x.SourceID <- r.ReadUInt32()
        x.TargetID <- r.ReadUInt32()
        x.Type     <- r.ReadUInt16() |> LanguagePrimitives.EnumOfValue<uint16, PacketTypes>
        x.Unknown  <- r.ReadUInt16()

type FFXIVUnknownMessage() = 
    inherit XIVPacket()

    member val Data = new ByteArray("") with get, set

    override x.Read(r) =
        x.Data <- new ByteArray(r.ReadRestBytes())

type FFXIVGameMessage() = 
    inherit XIVPacket()

    member val Magic     = 0us with get, set
    member val Opcode    = Opcodes.None with get, set
    member val Unknown1  = 0u with get, set
    member val TimeStamp = DateTimeOffset.UtcNow with get, set
    member val Unknown2  = 0u with get, set
    member val Packet    = None with get, set

    override x.Read(r) =
        x.Magic <- r.ReadUInt16()
        x.Opcode <- r.ReadUInt16() |> LanguagePrimitives.EnumOfValue<uint16, Opcodes>
        x.Unknown1 <- r.ReadUInt32()
        x.TimeStamp <- r.ReadTimeStampSec()
        x.Unknown2 <- r.ReadUInt32()
