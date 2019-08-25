namespace XIVServer.Route.UsernameMapping
open Nancy
open Nancy.Extensions
open Nancy.IO
open SQLite
open XIVServer.Global

type DBUserName() = 
    inherit LibXIVServer.UsernameMapping.UserInfo()

    [<PrimaryKeyAttribute>]
    member val UserId = base.UserId with get, set

type MarketOrders() as x = 
    inherit Nancy.NancyModule()
    do
        db.CreateTable<DBUserName>() |> ignore
        
        x.Put("/userid/{userId}", fun parms -> 
            let p = parms :?> Nancy.DynamicDictionary
            let itemId = p.["userId"] |> string

            let res = x.PutUsername(itemId)
            
            x.Response.AsText(res) :> obj)

        x.Get("/userid/{userId}", fun parms -> 
            let p = parms :?> Nancy.DynamicDictionary
            let itemId = p.["userId"] |> string

            x.GetUsername(itemId) :> obj )

    member private x.PutUsername(userId) =
        let str = RequestStream.FromStream(x.Request.Body).AsString()
        let r = Newtonsoft.Json.JsonConvert.DeserializeObject<DBUserName>(str)
        let num = db.InsertOrReplace(r)
        sprintf "Update user %s -> %s complete, %i row affected." r.UserId r.UserName num

    member private x.GetUsername(userId) =
        let res = db.Query<DBUserName>("select * from DBUsername where UserID = ?", userId)
        if res.Count = 0 then
            x.Response.AsJson(new DBUserName()).WithStatusCode(HttpStatusCode.NotFound)
        else
            x.Response.AsJson(res.[0]).WithHeader("Content-Type", "application/json")