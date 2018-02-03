module TradeLogs
open Nancy
open Nancy.ModelBinding
open LibFFXIV.Network
open SQLite
open LibXIVServer.Global
open System

[<CLIMutableAttribute>]
type DBTradeLog = 
    {
        ItemID      : uint32
        Price       : uint32
        TimeStamp   : uint32
        Count       : uint32
        IsHQ        : bool
        Unknown     : byte
        [<MaxLengthAttribute(20)>]
        BuyerName  : string
    }

    static member FromNetwork(log : SpecializedPacket.TradeLogRecord) = 
        {
            DBTradeLog.ItemID     = log.ItemID
            DBTradeLog.Price      = log.Price
            DBTradeLog.TimeStamp  = log.TimeStamp
            DBTradeLog.Count      = log.Count
            DBTradeLog.IsHQ       = log.IsHQ
            DBTradeLog.Unknown    = log.Unknown
            DBTradeLog.BuyerName  = log.BuyerName
        }

    static member ToNetwork(log : DBTradeLog) =
        {
            SpecializedPacket.TradeLogRecord.ItemID     = log.ItemID
            SpecializedPacket.TradeLogRecord.Price      = log.Price
            SpecializedPacket.TradeLogRecord.TimeStamp  = log.TimeStamp
            SpecializedPacket.TradeLogRecord.Count      = log.Count
            SpecializedPacket.TradeLogRecord.IsHQ       = log.IsHQ
            SpecializedPacket.TradeLogRecord.Unknown    = log.Unknown
            SpecializedPacket.TradeLogRecord.BuyerName  = log.BuyerName
        }

[<CLIMutableAttribute>]
type DBTradeLogUpdate = 
    {
        [<PrimaryKeyAttribute>]
        ItemID     : uint32
        LastUpdate : DateTime
    }

    static member GetUtcNow(id) = {ItemID = id; LastUpdate = System.DateTime.UtcNow}

type TradeLogs() as x = 
    inherit Nancy.NancyModule()
    do
        db.CreateTable<DBTradeLogUpdate>() |> ignore
        db.Execute("""CREATE TABLE IF NOT EXISTS "DBTradeLog" ( "ItemID" integer , "Price" integer , "TimeStamp" integer , "Count" integer , "IsHQ" integer , "Unknown" integer , "BuyerName" varchar(20), UNIQUE ("ItemID" , "TimeStamp" ) ON CONFLICT IGNORE)""")
        |> ignore
        x.Put("/tradelogs/{itemId:int}", fun parms -> 
            let p = parms :?> Nancy.DynamicDictionary
            let itemId = p.["ItemId"] |> System.Convert.ToUInt32

            let res = x.PutTradeLogs(itemId)
            
            x.Response.AsText(res) :> obj)

        x.Get("/tradelogs/{itemId:int}/{days:max(28)}", fun parms -> 
            let p = parms :?> Nancy.DynamicDictionary
            let itemId = p.["ItemId"] |> System.Convert.ToUInt32
            let days   = p.["days"]   |> System.Convert.ToUInt32

            x.GetTradeLogs(itemId, days) :> obj )

    member private x.PutTradeLogs (item) = 
        let logs = x.Bind<DBTradeLog []>() 
        if isNull logs then
            sprintf "Updated %i failed: Data error" item
        else
            db.InsertAll(logs) |> ignore
            db.InsertOrReplace(DBTradeLogUpdate.GetUtcNow(item)) |> ignore
            sprintf "Updated tradelog %i complete" item

    member private x.GetTradeLogs (item, days) = 
        let timespan = (new System.TimeSpan(days |> int , 0, 0, 0)).TotalSeconds |> int
        let targetTimestamp = Utils.TimeStamp.Now - timespan
        let res = db.Query<DBTradeLog>("select * from DBTradeLog where ItemID = ? and TimeStamp >= ?", item, targetTimestamp)
        let test = res |> Seq.map (DBTradeLog.ToNetwork) |> Seq.toArray
        
        let updated = db.Query<DBTradeLogUpdate>("select * from DBTradeLogUpdate where ItemID = ?", item)
        if updated.Count = 0 then
            x.Response.AsJson(test)
        else
            let upd = updated.[0]
            x.Response.AsJson(test).WithHeader("Last-Modified", upd.LastUpdate.ToString("R"))

