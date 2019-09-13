namespace LibXIVServer.Fable.UsernameMapping

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

    member x.CopyPropFrom(obj) = 
        LibXIVServer.Fable.Utils.PropCopier.Copy(obj, x)
        x

type IUsernameMapping = 
    {
        PutMapping  : FabelUsernameMapping -> Async<unit>
        PutMappings : FabelUsernameMapping[] -> Async<unit>
        GetById     : string -> Async<FabelUsernameMapping>
        GetByName   : string -> Async<FabelUsernameMapping>
    }