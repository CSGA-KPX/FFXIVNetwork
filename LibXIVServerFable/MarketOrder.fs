namespace LibXIVServer.Fable.MarketOrder

[<CLIMutable>]
type FableMarketOrder = 
    {
        OrderId     : uint32
        Unknown1    : uint32
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

    static member Empty = 
        {
            OrderId     = 0u
            Unknown1    = 0u
            RetainerId  = ""
            UserId      = ""
            SignUserId  = ""
            Price       = 0u
            Unknown2    = 0u
            Count       = 0u
            ItemId      = 0u
            TimeStamp   = 0u
            Unknown3    = [| |]
            Name        = ""
            IsHQ        = false
            MeldCount   = 0uy
            Market      = 0uy
            Unknown4    = 0uy
            Unknown5    = [| |]
            Unknown6    = [| |]

            WorldId     = 0us
        }
    member x.CopyPropFrom(obj) = 
        LibXIVServer.Fable.Utils.PropCopier.Copy(obj, x)
        x


type IMarkerOrder = 
    {
        PutOrders       : uint16 -> FableMarketOrder[] -> Async<unit>
        GetByIdWorld    : uint16 -> uint32 -> Async<FableMarketOrder []>
        GetByIdAllWorld : uint32 -> Async<(uint16 * FableMarketOrder []) []>
    }