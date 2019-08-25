module TradeLogs
open Nancy
open Nancy.ModelBinding
open SQLite
open XIVServer.Global
open System

type DBTradeLog() = 
    inherit LibXIVServer.TradeLogV2.ServerTradeLog()

    [<Indexed(Name = "ItemId")>]
    member x.ItemId = base.ItemId
    [<Indexed(Name = "TimeStamp", Order=2)>]
    member x.TimeStamp = base.TimeStamp
    [<MaxLengthAttribute(20)>]
    member x.BuyerName = x.BuyerName

(*
[<CLIMutableAttribute>]
type DBTradeLog = 
    {
        [<Indexed(Name = "ItemID")>]
        ItemID      : uint32
        Price       : uint32
        [<Indexed(Name = "TimeStamp", Order=2)>]
        TimeStamp   : uint32
        Count       : uint32
        IsHQ        : bool
        //uint16 casted to uint32
        Unknown     : uint32
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
        }*)

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
        db.CreateTable<DBTradeLog>() |> ignore
        db.Query<DBTradeLog>("CREATE UNIQUE INDEX IF NOT EXISTS DBTradeLog_UniqueIndex ON DBTradeLog(TimeStamp, BuyerName, Price, Count, IsHQ)") |> ignore

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
            let total   = logs.Length
            let updated = db.InsertAll(logs, "OR IGNORE", true)
            db.InsertOrReplace(DBTradeLogUpdate.GetUtcNow(item)) |> ignore
            sprintf "\r\nUpdated tradelog %i,\r\n%i/%i row(s) updated" item updated total

    member private x.GetTradeLogs (item, days) = 
        let timespan = new System.TimeSpan(days |> int , 0, 0, 0)
        let targetTimestamp = DateTimeOffset.UtcNow.Subtract(timespan).ToUnixTimeSeconds()
        let res = db.Query<DBTradeLog>("select * from DBTradeLog where ItemID = ? and TimeStamp >= ?", item, targetTimestamp)
        let test = res |> Seq.toArray
        
        let updated = db.Query<DBTradeLogUpdate>("select * from DBTradeLogUpdate where ItemID = ?", item)
        if updated.Count = 0 then
            x.Response.AsJson(test)
        else
            let upd = updated.[0]
            x.Response.AsJson(test).WithHeader("Last-Modified", upd.LastUpdate.ToString("R"))

            