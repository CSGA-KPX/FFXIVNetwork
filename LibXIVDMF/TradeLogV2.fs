module LibXIVServer.TradeLogV2
open System
open LibFFXIV.Network
open LibXIVServer.Common

type ServerTradeLog (world : uint16) = 
    inherit SpecializedPacket.TradeLog()

    member val WorldId = world with get, set

    new () = new ServerTradeLog(0us)

    member x.DoPropCopy(log:SpecializedPacket.TradeLog) = 
        PropCopier.Copy(log, x)
        x

type TradeLogDAO () = 
    inherit DAOBase<ServerTradeLog []>()

    override x.GetUrl([<ParamArray>] args:Object []) =
        String.Format("/tradelogs/{0}/{1}", args)

    override x.PutUrl([<ParamArray>] args:Object []) = 
        String.Format("/tradelogs/{0}", args)


    member x.GetRange(itemId, days) =
        let url = x.GetUrl(itemId, days)
        x.DoGet(url)

    member x.Get(itemId : uint32) = 
        x.GetRange(itemId, 7)

    member x.Put(itemId : uint32, logs : ServerTradeLog []) = 
        let url = x.PutUrl(itemId)
        x.DoPut(url, logs)
        