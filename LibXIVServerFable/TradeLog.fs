namespace LibXIVServer.Shared.TradeLog
open System

[<CLIMutable>]
type FableTradeLog = 
    {
        Id          : string
        ItemId      : uint32
        Price       : uint32
        TimeStamp   : uint32
        Count       : uint32
        IsHQ        : bool
        Unknown     : uint32
        BuyerName   : string

        WorldId     : uint16
    }

    member x.CreateFrom(world, y : LibFFXIV.Network.SpecializedPacket.TradeLog) = 
        {
            Id        = FableTradeLog.CalculateId(world, y.BuyerName, y.TimeStamp)
            ItemId    = y.ItemId
            Price     = y.Price
            TimeStamp = y.TimeStamp
            Count     = y.Count
            IsHQ      = y.IsHQ
            Unknown   = y.Unknown
            BuyerName = y.BuyerName
            WorldId   = world
        }

    // 同一个世界的同一个用户不能在同一个时间买多个物品
    static member CalculateId (world, name, ts) =
        sprintf "%i:%i:%s" world ts name

type ITradeLog = 
    {
        PutTradeLogs        : FableTradeLog [] -> Async<unit>
        GetByIdWorld        : uint16 -> uint32 -> int32 -> Async<FableTradeLog []>
        GetByIdAllWorld     : uint32 -> int32 -> Async<FableTradeLog []>
        GetAllByIdWorld     : uint16 -> uint32 -> Async<FableTradeLog []>
        GetAllByIdAllWorld  : uint32 -> Async<FableTradeLog []>
    }