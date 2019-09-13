module MarketOrders
open Nancy
open SQLite
open System
open Nancy.IO
open Nancy.Extensions
open XIVServer.Global
//orders/
//      / raw/itemid    GET/PUT  读写MarketRecord []


type DBMarketRecord () = 
    inherit LibXIVServer.MarketV2.ServerMarkerOrder(0us) 

    [<Indexed(Name = "UserId")>]
    member val UserId = base.UserId with get, set

    [<Indexed(Name = "ItemId")>]
    member val ItemId = base.ItemId with get, set
(*
[<CLIMutableAttribute>]
type DBMarketRecord = 
    {
        OrderID     : uint32
        Unknown1    : uint32
        RetainerID  : int64
        [<Indexed(Name = "UserID")>]
        UserID      : int64
        SignUserID  : int64 //道具制作者签名
        Price       : uint32
        Unknown2    : uint32
        Count       : uint32
        [<Indexed(Name = "ItemID")>]
        ItemID      : uint32
        ///最后访问雇员的日期
        TimeStamp   : uint32
        Unknown3    : byte [] //24 byte unknown
        Name        : string  //32 byte zero-ter UTF8雇员名称
        IsHQ        : bool    // 1 byte
        MeldCount   : byte    // 1 byte
        Market      : byte    // 1 byte
        Unknown4    : byte    // 1 byte
    }

    static member ToDB(r : SpecializedPacket.MarketRecord) = 
        {
            DBMarketRecord.OrderID = r.OrderID
            DBMarketRecord.Unknown1 = r.Unknown1
            DBMarketRecord.RetainerID = r.RetainerID |> int64
            DBMarketRecord.UserID = r.UserID |> int64
            DBMarketRecord.SignUserID = r.SignUserID |> int64
            DBMarketRecord.Price = r.Price
            DBMarketRecord.Unknown2 = r.Unknown2
            DBMarketRecord.Count = r.Count
            DBMarketRecord.ItemID = r.Itemid
            DBMarketRecord.TimeStamp = r.TimeStamp
            DBMarketRecord.Unknown3 = r.Unknown3
            DBMarketRecord.Name = r.Name
            DBMarketRecord.IsHQ = r.IsHQ
            DBMarketRecord.MeldCount = r.MeldCount
            DBMarketRecord.Market = r.Market
            DBMarketRecord.Unknown4 = r.Unknown4
        }

    static member ToNetwork(r : DBMarketRecord) = 
        {
            SpecializedPacket.MarketRecord.OrderID = r.OrderID
            SpecializedPacket.MarketRecord.Unknown1 = r.Unknown1
            SpecializedPacket.MarketRecord.RetainerID = r.RetainerID |> uint64
            SpecializedPacket.MarketRecord.UserID = r.UserID |> uint64
            SpecializedPacket.MarketRecord.SignUserID = r.SignUserID |> uint64
            SpecializedPacket.MarketRecord.Price = r.Price
            SpecializedPacket.MarketRecord.Unknown2 = r.Unknown2
            SpecializedPacket.MarketRecord.Count = r.Count
            SpecializedPacket.MarketRecord.Itemid = r.ItemID
            SpecializedPacket.MarketRecord.TimeStamp = r.TimeStamp
            SpecializedPacket.MarketRecord.Unknown3 = r.Unknown3
            SpecializedPacket.MarketRecord.Name = r.Name
            SpecializedPacket.MarketRecord.IsHQ = r.IsHQ
            SpecializedPacket.MarketRecord.MeldCount = r.MeldCount
            SpecializedPacket.MarketRecord.Market = r.Market
            SpecializedPacket.MarketRecord.Unknown4 = r.Unknown4
        }
*)
[<CLIMutableAttribute>]
type DBOrdersSnapshot =
    {
        [<PrimaryKeyAttribute>]
        ItemID     : uint32
        OrdersJSON : string
        ///UTC time
        LastUpdate : System.DateTime
    }

[<CLIMutableAttribute>]
type DBMarketRecordOrderIDUpdate =
    {
        [<PrimaryKeyAttribute>]
        OrderID     : uint64
        Price       : uint32
        ///UTC time
        LastAccessTime : System.DateTime
    }
    static member FromOrder (o : DBMarketRecord) = 
        {
            OrderID = o.OrderId
            Price   = o.Price
            LastAccessTime = DateTime.UtcNow
        }

[<CLIMutableAttribute>]
type ExistsResult =
    {
        Result : uint32
    }


type MarketOrders() as x = 
    inherit Nancy.NancyModule()
    do
        db.CreateTable<DBOrdersSnapshot>() |> ignore
        db.CreateTable<DBMarketRecord>() |> ignore
        db.CreateTable<DBMarketRecordOrderIDUpdate>() |> ignore
        db.Query<DBMarketRecordOrderIDUpdate>("CREATE INDEX IF NOT EXISTS `DBMarketRecordOrderIDUpdate_Index` ON `DBMarketRecordOrderIDUpdate` (`OrderID`,`Price`)") |> ignore
        

        x.Put("/orders/raw/{itemId:int}", fun parms -> 
            let p = parms :?> Nancy.DynamicDictionary
            let itemId = p.["ItemId"] |> System.Convert.ToUInt32

            let res = x.PutOrders(itemId)
            
            x.Response.AsText(res) :> obj)

        x.Get("/orders/raw/{itemId:int}", fun parms -> 
            let p = parms :?> Nancy.DynamicDictionary
            let itemId = p.["ItemId"] |> System.Convert.ToUInt32

            x.GetOrders(itemId) :> obj )

        x.OnError.AddItemToStartOfPipeline(fun ctx exn ->
            printfn "%O" exn
            null :> obj)

    member private x.PutOrders(itemId) =
        let str = RequestStream.FromStream(x.Request.Body).AsString()
        let logs = Newtonsoft.Json.JsonConvert.DeserializeObject<DBMarketRecord[]>(str)
            //Json.UnPickleOfString<DBMarketRecord[]>(str)
        
        if isNull logs then
            sprintf "Updated %i failed: Data error" itemId
        else
            if logs |> Array.exists (fun x -> x.ItemId <> itemId) then
                sprintf "Updated %i failed: ItemID mismatch" itemId
            else
                let obj = {OrdersJSON = str; ItemID = itemId; LastUpdate = DateTime.UtcNow;}
                db.InsertOrReplace(obj) |> ignore
                let total = logs.Length
                try
                    let count = ref 0
                    db.RunInTransaction(fun () ->
                        //Add Orders
                        for o in logs do 
                            //Check for existence
                            let result = db.Query<ExistsResult>("SELECT EXISTS(SELECT 1 FROM DBMarketRecordOrderIDUpdate WHERE OrderID = ? AND Price = ?) AS Result", o.OrderId, o.Price).[0].Result
                            if result = 0u then
                                 //If not exist, then insert
                                db.Insert(o) |> ignore
                                incr count
                        //Update all caches
                        db.InsertAll(logs |> Array.map (DBMarketRecordOrderIDUpdate.FromOrder), "OR REPLACE", true) |> ignore
                    )
                    sprintf "Updated market orders %i complete, %i/%i row(s) updated" itemId (!count) total
                with
                | e -> sprintf "Updated market orders %i failed, exp is %A" itemId e
       
    member private x.GetOrders(itemId) =
        let res = db.Query<DBOrdersSnapshot>("select * from DBOrdersSnapshot where ItemID = ?", itemId)
        if res.Count = 0 then
            x.Response.AsJson([| |])
        else
            let r = res.[0]
            x.Response.AsText(r.OrdersJSON).WithHeader("Last-Modified", r.LastUpdate.ToString("R")).WithHeader("Content-Type", "application/json")