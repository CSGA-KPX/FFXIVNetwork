module LibXIVServer.UsernameMapping
open System
open MBrace.FsPickler
open LibFFXIV.Network
open LibXIVServer.Common

type UsernameMappingDAO () = 
    inherit DAOBase<SpecializedPacket.CharacterNameLookupReply>()

    override x.GetUrl([<ParamArray>] args:Object []) =
        String.Format("/userid/{0}", args)

    override x.PutUrl([<ParamArray>] args:Object []) = 
        String.Format("/userid/{0}", args)


    member x.Get(userId) =
        let url = x.GetUrl(userId)
        x.DoGet(url)


    member x.Put(data : SpecializedPacket.CharacterNameLookupReply) = 
        let url = x.GetUrl(data.UserID)
        x.DoPut(url, data)