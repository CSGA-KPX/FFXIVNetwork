module GlobalVars
open System
open System.Collections.Generic

//let WorldsIdToWorld = new Dictionary<uint16, LibFFXIV.SpecializedPacket.World>()

let ServerIpToWorld = new Dictionary<string, LibFFXIV.SpecializedPacket.World>()
    //let d = new Dictionary<string, LibFFXIV.SpecializedPacket.World>()
    //d.Add(Utils.LobbyServerIP, LibFFXIV.SpecializedPacket.World.LobbyWorld)
    //d

let Character = new Dictionary<uint64, LibFFXIV.SpecializedPacket.Character>()