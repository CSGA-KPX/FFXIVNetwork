namespace FFXIVNetwork.PacketAnalyzer

open System
open LibFFXIV.Network.BasePacket
open LibFFXIV.Network.Utils

type LogEntry = 
    {
        Time : string
        Level : string
        Source : string
        Direction : string
        OpCode : string
        Data : string
    }

    static member Parse(str : string) = 
        let a = str.Split([|'|'|], StringSplitOptions.RemoveEmptyEntries)
        let time = a.[0]
        let level = a.[1]
        let source = a.[2]
        let msg = a.[3]
        let dir = if msg.[0] = '>' then ">>>>>" else "<<<<<"
        let op = 
            let idx = msg.IndexOf("OP:")
            msg.[idx + 3 .. idx + 3 + 4 - 1]
        let data = 
            let idx = msg.IndexOf("Data:")
            msg.[idx + 5..]
        {
            Time = time
            Level = level
            Source = source
            Direction = dir
            OpCode = op
            Data = data
        }

    member x.AsBytes() = 
        HexString.ToBytes(x.Data)

    member x.AsPacket() = 
        let ba = ByteArray(x.AsBytes())
        let sub = FFXIVSubPacket(ba)
        let gp = FFXIVGamePacket(sub.Data)
        gp.Data

    override x.ToString() = 
        sprintf "%s %s OP:%s Data:%s" x.Time x.Direction x.OpCode x.Data