namespace LibXIVServer.Fable.TradeLog

[<CLIMutable>]
type FableTradeLog = 
    {
        ItemId      : uint32
        Price       : uint32
        TimeStamp   : uint32
        Count       : uint32
        IsHQ        : bool
        Unknown     : uint32
        BuyerName   : string

        WorldId     : uint16
    }

    static member Empty = 
        {
            ItemId      = 0u
            Price       = 0u
            TimeStamp   = 0u
            Count       = 0u
            IsHQ        = false
            Unknown     = 0u
            BuyerName   = ""

            WorldId     = 0us
        }

    member x.CopyPropFrom(obj) = 
        LibXIVServer.Fable.Utils.PropCopier.Copy(obj, x)
        x

type ITradeLog = 
    {
        PutTradeLogs        : uint16 -> FableTradeLog [] -> Async<unit>
        GetByIdWorld        : uint16 -> uint32 -> int32 -> Async<FableTradeLog []>
        GetByIdAllWorld     : uint32 -> int32 -> Async<FableTradeLog []>
        GetAllByIdWorld     : uint16 -> uint32 -> Async<FableTradeLog []>
        GetAllByIdAllWorld  : uint32 -> Async<FableTradeLog []>
    }