module MarketOrder
open System
open LibDmfXiv.Shared.MarketOrder
open LiteDB.FSharp.Extensions

/// 市场快照
/// 用于查询价格
let private dbSnapshot = Database.db.GetCollection<MarketSnapshot>()
let private dbHistory  = Database.db.GetCollection<LibDmfXiv.Shared.MarketOrder.FableMarketOrder>()

let marketOrderApi : IMarkerOrder = 
    {
        PutOrders = fun worldId itemId orders -> async {
            let current = new MarketSnapshot(itemId, worldId, Orders = orders)
            dbSnapshot.Upsert(current) |> ignore

            orders
            |> Array.iter (fun x -> 
                dbHistory.Upsert(x) |> ignore
            )

            return ()
        }

        GetByIdWorld = fun worldId itemId -> async {
            let id = MarketSnapshot.GetId(itemId, worldId)
            let ret = 
                match dbSnapshot.TryFindById(Database.Utils.ToDocument(id)) with
                | None -> new MarketSnapshot(itemId, worldId)
                | Some(x) -> x
            return ret
        }

        GetByIdAllWorld = fun itemId -> async {
            let query = LiteDB.Query.EQ("ItemId", Database.Utils.ToDocument(itemId))
            return dbSnapshot.Find(query) |> Seq.toArray
        }
    }

do
    dbSnapshot.EnsureIndex((fun x -> x.ItemId), false) |> ignore

    dbHistory.EnsureIndex((fun x -> x.Id), true) |> ignore
    dbHistory.EnsureIndex((fun x -> x.TimeStamp), "STRING($._id)+':'+STRING($.TimeStamp)",  true) |> ignore
    