module UsernameMapping
open System
open Nancy
open Nancy.Extensions
open Nancy.IO
open Nancy.ModelBinding
open LibFFXIV.Network
open SQLite
open LibXIVServer.Global

[<CLIMutableAttribute>]
type DBUsername =
    {
        [<PrimaryKeyAttribute>]
        UserID     : int64
        Username   : string
    }

type MarketOrders() as x = 
    inherit Nancy.NancyModule()
    do
        db.CreateTable<DBUsername>() |> ignore
        
        x.Put("/userid/{userId:long}", fun parms -> 
            let p = parms :?> Nancy.DynamicDictionary
            let itemId = p.["userId"] |> System.Convert.ToInt64

            let res = x.PutUsername(itemId)
            
            x.Response.AsText(res) :> obj)

        x.Get("/userid/{userId:long}", fun parms -> 
            let p = parms :?> Nancy.DynamicDictionary
            let itemId = p.["userId"] |> System.Convert.ToInt64

            x.GetUsername(itemId) :> obj )

    member private x.PutUsername(userId) =
        let str = RequestStream.FromStream(x.Request.Body).AsString()
        let r = Json.UnPickleOfString<SpecializedPacket.CharacterNameLookupReply>(str)
        let dbobj = {UserID = r.UserID |> int64; Username = r.Username}
        let num = db.InsertOrReplace(dbobj)
        sprintf "Update user %i -> %s complete, %i row affected." r.UserID r.Username num

    member private x.GetUsername(userId) =
        let res = db.Query<DBUsername>("select * from DBUsername where UserID = ?", userId)
        if res.Count = 0 then
            x.Response.AsJson({UserID = userId; Username = ""}).WithStatusCode(HttpStatusCode.NotFound)
        else
            x.Response.AsJson(res.[0]).WithHeader("Content-Type", "application/json")