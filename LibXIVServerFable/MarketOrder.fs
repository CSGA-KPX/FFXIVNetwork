namespace LibDmfXiv.Shared.MarketOrder
open System

[<CLIMutable>]
type FableMarketOrder = 
    {
        Id          : string
        OrderId     : uint64
        RetainerId  : string
        UserId      : string
        SignUserId  : string
        Price       : uint32
        Unknown2    : uint32
        Count       : uint32
        ItemId      : uint32
        TimeStamp   : uint32
        Unknown3    : byte []
        Name        : string
        IsHQ        : bool
        MeldCount   : byte
        Market      : byte
        Unknown4    : byte
        Unknown5    : byte []
        Unknown6    : byte []

        WorldId     : uint16
    }

    static member CalculateUniqueId (y : LibFFXIV.Network.SpecializedPacket.MarketOrder) = 
        sprintf "%i:%i" y.OrderId y.TimeStamp

    static member CreateFrom(world, y : LibFFXIV.Network.SpecializedPacket.MarketOrder) = 
        {
            Id         = FableMarketOrder.CalculateUniqueId(y)
            OrderId    = y.OrderId
            RetainerId = y.RetainerId
            UserId     = y.UserId
            SignUserId = y.SignUserId
            Price      = y.Price
            Unknown2   = y.Unknown2
            Count      = y.Count
            ItemId     = y.ItemId
            TimeStamp  = y.TimeStamp
            Unknown3   = y.Unknown3
            Name       = y.Name
            IsHQ       = y.IsHQ
            MeldCount  = y.MeldCount
            Market     = y.Market
            Unknown4   = y.Unknown4
            Unknown5   = y.Unknown5
            Unknown6   = y.Unknown6
            WorldId    = world
        }

type MarketSnapshot(i,w)= 
    member val Id = MarketSnapshot.GetId(i, w) with get, set
    member val ItemId : uint32 = i with get, set
    member val WorldId : uint16 = w with get, set
    member val UpdateTime = DateTimeOffset.UtcNow with get,set
    member val Orders : FableMarketOrder[] = [||] with get, set

    new () = new MarketSnapshot(0u, 0us)

    static member GetId(i : uint32, w : uint16) = sprintf "%i:%i" i w 

type IMarkerOrder = 
    {
        PutOrders       : uint16 -> uint32 -> FableMarketOrder[] -> Async<unit>
        GetByIdWorld    : uint16 -> uint32 -> Async<MarketSnapshot>
        GetByIdAllWorld : uint32 -> Async<MarketSnapshot []>
    }