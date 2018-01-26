module LibFFXIV.TcpPacket
open System
open LibFFXIV.Utils
open LibFFXIV.BasePacket

type PacketDirection = 
    | In
    | Out 
    