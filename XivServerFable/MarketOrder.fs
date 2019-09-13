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
            let id = MarketSnapshot.GetId(itemId, worldId)
            let snap =
                match dbSnapshot.TryFindById(new LiteDB.BsonValue(id)) with
                | None -> new MarketSnapshot(itemId, worldId, Orders = orders)
                | Some(x) -> x
            dbSnapshot.Upsert(snap) |> ignore

            orders
            |> Array.iter (fun x -> 
                if not <| dbSnapshot.Exists(LiteDB.Query.EQ("_id", new LiteDB.BsonValue(x.Id))) then
                    dbHistory.Insert(x) |> ignore
            )

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

    dbHistory.EnsureIndex((fun x -> x.TimeStamp), "STRING($._id)+':'+STRING($.TimeStamp)",  true) |> ignore
    