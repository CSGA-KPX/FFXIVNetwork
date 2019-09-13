module MarketOrder
open System
open LibXIVServer.Shared.MarketOrder
open LiteDB.FSharp.Extensions

/// 市场快照
/// 用于查询价格
let dbSnapshot = Database.db.GetCollection<MarketSnapshot>()
let dbHistory  = Database.db.GetCollection<LibXIVServer.Shared.MarketOrder.FableMarketOrder>()

let marketOrderApi : IMarkerOrder = 
    {
        PutOrders = fun worldId itemId orders -> async {
            let id = MarketSnapshot.GetId(itemId, worldId)
            let snap =
                match dbSnapshot.TryFindById(new LiteDB.BsonValue(id)) with
                | None -> new MarketSnapshot(itemId, worldId, Orders = orders)
                | Some(x) -> x
            dbSnapshot.Upsert(snap) |> ignore

            orders
            |> Array.iter (fun x -> dbHistory.Upsert(x) |> ignore)

            return ()
        }

        GetByIdWorld = fun worldId itemId -> async {
            let id = MarketSnapshot.GetId(itemId, worldId)
            let ret = 
                match dbSnapshot.TryFindById(new LiteDB.BsonValue(id)) with
                | None -> new MarketSnapshot(itemId, worldId)
                | Some(x) -> x
            return ret
        }

        GetByIdAllWorld = fun itemId -> async {
            return dbSnapshot.findMany <@ fun snap -> snap.ItemId = itemId @> |> Seq.toArray
        }
    }

do
    dbSnapshot.EnsureIndex((fun x -> x.ItemId), false) |> ignore

    dbHistory.EnsureIndex((fun x -> x.OrderId), "STRING($.OrderId)+':'+STRING($.TimeStamp)",  true) |> ignore
    