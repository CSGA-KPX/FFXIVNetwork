module LibXIVServer.UsernameMapping
open System
open LibXIVServer.Common


type UserInfo(id, name) = 
    member val UserId = id with get, set
    member val UserName = name with get, set

    new () = new UserInfo("", "")

    static member From(id, name) = new UserInfo(id, name)

type UsernameMappingDAO () = 
    inherit DAOBase<UserInfo>()

    override x.GetUrl([<ParamArray>] args:Object []) =
        String.Format("/userid/{0}", args)

    override x.PutUrl([<ParamArray>] args:Object []) = 
        String.Format("/userid/{0}", args)


    member x.Get(userId) =
        let url = x.GetUrl(userId)
        x.DoGet(url)

    member x.Put(data : UserInfo) = 
        let url = x.GetUrl(data.UserId)
        x.DoPut(url, data)