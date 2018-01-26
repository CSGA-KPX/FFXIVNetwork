namespace LibFFXIV.SpecializedPacket
open System
open LibFFXIV.Utils

type World = 
    {
        WorldId   : uint16
        WorldName : string
    }

    member x.IsLobby = x.WorldId = 0xFFFFus

    static member LobbyWorld = { WorldId = 0xFFFFus; WorldName = "Lobby" }

type WorldList = 
    {
        WorldList : World []
    }

    static member ParseFromBytes(bytes : byte []) = 
        use r = XIVBinaryReader.FromBytes(bytes)
        let header   = r.ReadBytes(24)
        [|
            while not (r.IsEnd()) do 
                let id     = r.ReadUInt16()
                let unused = r.ReadBytes(18)
                let name   = r.ReadFixedUTF8(64)
                if id <> 0us then
                    yield {
                        WorldId   = id
                        WorldName = name
                    }
        |]
