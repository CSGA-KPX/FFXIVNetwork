module MarketOrder
open System
open LibXIVServer.Fable.MarketOrder

/// 市场快照
/// 用于查询价格
[<CLIMutable>]
type MarketSnapshot = 
    {
        ItemId      : uint32
        WorldId     : uint16
        UpdateTime  : DateTimeOffset
    }

let marketOrderApi : IMarkerOrder = 
    {
        PutOrders = fun worldId orders -> async {
            return ()
        }
        GetByIdWorld = fun worldId itemId -> async {
            return [||]
        }

        GetByIdAllWorld = fun itemId -> async {
            return 
                [|
                    yield 0us, [||]
                |]
        }
    }

do
    let col = Database.db.GetCollection<MarketSnapshot>()
    col.EnsureIndex(fun x -> sprintf "%i%i" x.ItemId x.WorldId, true) |> ignore
    col.EnsureIndex(fun x -> x.ItemId, false) |> ignore
    col.EnsureIndex(fun x -> x.WorldId, false) |> ignore
    
    //Unique index

    ()