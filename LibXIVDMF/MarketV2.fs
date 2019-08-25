module LibXIVServer.MarketV2
open System
open LibFFXIV.Network
open LibXIVServer.Common

type ServerMarkerOrder (world : uint16)= 
    inherit SpecializedPacket.MarketOrder()

    new () = new ServerMarkerOrder(0us)
    member val WorldId = world with get, set

    member x.DoPropCopy(order:SpecializedPacket.MarketOrder) = 
        PropCopier.Copy(order, x)
        x

type MarketOrderDAO () = 
    inherit DAOBase<ServerMarkerOrder []>()

    override x.GetUrl([<ParamArray>] args:Object []) =
        String.Format("/orders/raw/{0}", args)

    override x.PutUrl([<ParamArray>] args:Object []) = 
        String.Format("/orders/raw/{0}", args)

    member x.Get(itemId : uint32) = 
        let url = x.GetUrl(itemId)
        x.DoGet(url)

    member x.Put(itemId : uint32, orders : ServerMarkerOrder []) = 
        let url = x.GetUrl(itemId)
        x.DoPut(url, orders)