﻿namespace LibXIVServer.Shared.UsernameMapping

[<CLIMutable>]
type FabelUsernameMapping = 
    {
        UserId   : string
        UserName : string
    }

    static member Empty = 
        {
            UserId   = ""
            UserName = ""
        }

    member x.CreateFrom(y : LibFFXIV.Network.SpecializedPacket.CharacterNameLookupReply) = 
        {
            UserId   = y.UserId
            UserName = y.UserName
        }

type IUsernameMapping = 
    {
        PutMapping  : FabelUsernameMapping -> Async<unit>
        PutMappings : FabelUsernameMapping[] -> Async<unit>
        GetById     : string -> Async<FabelUsernameMapping option>
        GetByName   : string -> Async<FabelUsernameMapping[]>
    }