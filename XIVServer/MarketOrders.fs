module MarketOrders
open System
open Nancy
open Nancy.Extensions
open Nancy.IO
open Nancy.ModelBinding
open LibFFXIV.Network
open SQLite
open LibXIVServer.Global

//orders/
//      / raw/itemid    GET/PUT  读写MarketRecord []

[<CLIMutableAttribute>]
type DBOrdersSnapshot =
    {
        [<PrimaryKeyAttribute>]
        ItemID     : uint32
        OrdersJSON : string
        ///UTC time
        LastUpdate : System.DateTime
    }

type MarketOrders() as x = 
    inherit Nancy.NancyModule()
    do
        db.CreateTable<DBOrdersSnapshot>() |> ignore
        
        x.Put("/orders/raw/{itemId:int}", fun parms -> 
            let p = parms :?> Nancy.DynamicDictionary
            let itemId = p.["ItemId"] |> System.Convert.ToUInt32

            let res = x.PutOrders(itemId)
            
            x.Response.AsText(res) :> obj)

        x.Get("/orders/raw/{itemId:int}", fun parms -> 
            let p = parms :?> Nancy.DynamicDictionary
            let itemId = p.["ItemId"] |> System.Convert.ToUInt32

            x.GetOrders(itemId) :> obj )

    member private x.PutOrders(itemId) =
        let str = RequestStream.FromStream(x.Request.Body).AsString()
        let logs = Json.UnPickleOfString<SpecializedPacket.MarketRecord []>(str)
        if isNull logs then
            sprintf "Updated %i failed: Data error" itemId
        else
            if logs |> Array.exists (fun x -> x.Itemid <> itemId) then
                sprintf "Updated %i failed: ItemID mismatch" itemId
            else
                let obj = {OrdersJSON = str; ItemID = itemId; LastUpdate = DateTime.UtcNow;}
                db.InsertOrReplace(obj) |> ignore
                sprintf "Updated market orders %i complete" itemId
       
    member private x.GetOrders(itemId) =
        let res = db.Query<DBOrdersSnapshot>("select * from DBOrdersSnapshot where ItemID = ?", itemId)
        if res.Count = 0 then
            x.Response.AsJson([| |])
        else
            let r = res.[0]
            x.Response.AsText(r.OrdersJSON).WithHeader("Last-Modified", r.LastUpdate.ToString("R")).WithHeader("Content-Type", "application/json")