module LibXIVServer.MarketV2
open System
open MBrace.FsPickler
open LibFFXIV.Network
open LibXIVServer.Common


type MarketOrderDAO () = 
    inherit DAOBase<SpecializedPacket.MarketRecord []>()

    override x.GetUrl([<ParamArray>] args:Object []) =
        String.Format("/orders/raw/{0}", args)

    override x.PutUrl([<ParamArray>] args:Object []) = 
        String.Format("/orders/raw/{0}", args)

    member x.Get(itemId : uint32) = 
        let url = x.GetUrl(itemId)
        x.DoGet(url)

    member x.Put(itemId : uint32, orders : SpecializedPacket.MarketRecord []) = 
        let url = x.GetUrl(itemId)
        x.DoPut(url, orders)