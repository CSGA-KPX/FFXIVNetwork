module TradeLog
open System.Linq
open LibDmfXiv.Shared.TradeLog
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
            let itemQuery = Query.EQ("ItemId", Database.Utils.ToDocument(itemId))
            let worldQuery= Query.EQ("WorldId", Database.Utils.ToDocument(worldId))
            let query = Query.And(itemQuery, worldQuery)
            
            let ret = 
                tradelogDb.Find(query)
                            .OrderByDescending(fun x -> x.TimeStamp)
                            .Take(count)
                            .ToArray()
            return ret
        }
        GetByIdAllWorld = fun itemId count -> async {
            let itemQuery = Query.EQ("ItemId", Database.Utils.ToDocument(itemId))

            // 每个物品每个服务器是20条记录，* 代表最多10个服务器
            let ret = 
                tradelogDb.Find(itemQuery)
                            .OrderByDescending(fun x -> x.TimeStamp)
                            .Take(count * 10)
                            .ToArray()
            return ret
        }
        
        GetAllByIdWorld = fun worldId itemId -> async {
            let itemQuery = Query.EQ("ItemId", Database.Utils.ToDocument(itemId))
            let worldQuery= Query.EQ("WorldId", Database.Utils.ToDocument(worldId))
            let query = Query.And(itemQuery, worldQuery)

            let ret = 
                tradelogDb.Find(query)
                            .OrderByDescending(fun x -> x.TimeStamp)
                            .ToArray()
            return ret
        }
        
        GetAllByIdAllWorld = fun itemId  -> async {
            let itemQuery = Query.EQ("ItemId", Database.Utils.ToDocument(itemId))

            let ret = 
                tradelogDb.Find(itemQuery)
                            .OrderByDescending(fun x -> x.TimeStamp)
                            .ToArray()
            return ret
        }
    }

do
    tradelogDb.EnsureIndex(fun x -> x.ItemId) |> ignore
    tradelogDb.EnsureIndex(fun x -> x.WorldId) |> ignore