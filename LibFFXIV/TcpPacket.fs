module LibFFXIV.Network.TcpPacket
open System
open LibFFXIV.Network.Utils
open LibFFXIV.Network.BasePacket

type PacketDirection = 
    | In
    | Out 
    