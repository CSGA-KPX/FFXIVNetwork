module TradeLog
open System.Linq
open LibXIVServer.Shared.TradeLog
open LiteDB
open LiteDB.FSharp.Extensions

let tradelogDb = Database.db.GetCollection<FableTradeLog>()

let TradeLogApi : ITradeLog = 
    {
        PutTradeLogs = fun logs -> async {
            for log in logs do 
                tradelogDb.Upsert(log) |> ignore
        }

        GetByIdWorld = fun worldId itemId count -> async {
            let itemQuery = Query.EQ("ItemId", new BsonValue(itemId))
            let worldQuery= Query.EQ("WorldId", new BsonValue(worldId))
            let query = Query.And(itemQuery, worldQuery)
            
            return tradelogDb.Find(query, limit = count) |> Seq.toArray
        }
        GetByIdAllWorld = fun itemId count -> async {
            let itemQuery = Query.EQ("ItemId", new BsonValue(itemId))

            return tradelogDb.Find(itemQuery, limit = count) |> Seq.toArray
        }
        
        GetAllByIdWorld = fun worldId itemId -> async {
            let itemQuery = Query.EQ("ItemId", new BsonValue(itemId))
            let worldQuery= Query.EQ("WorldId", new BsonValue(worldId))
            let query = Query.And(itemQuery, worldQuery)
            
            return tradelogDb.Find(query) |> Seq.toArray
        }
        
        GetAllByIdAllWorld = fun itemId  -> async {
            let itemQuery = Query.EQ("ItemId", new BsonValue(itemId))

            return tradelogDb.Find(itemQuery) |> Seq.toArray
        }
    }

do
    tradelogDb.EnsureIndex(fun x -> x.ItemId) |> ignore
    tradelogDb.EnsureIndex(fun x -> x.WorldId) |> ignore