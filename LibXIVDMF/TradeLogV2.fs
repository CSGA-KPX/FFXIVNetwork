module LibXIVServer.TradeLogV2
open System
open LibFFXIV.Network
open LibXIVServer.Common

type TradeLogDAO () = 
    inherit DAOBase<SpecializedPacket.TradeLogRecord []>()

    override x.GetUrl([<ParamArray>] args:Object []) =
        String.Format("/tradelogs/{0}/{1}", args)

    override x.PutUrl([<ParamArray>] args:Object []) = 
        String.Format("/tradelogs/{0}", args)


    member x.GetRange(itemId, days) =
        let url = x.GetUrl(itemId, days)
        x.DoGet(url)

    member x.Get(itemId : uint32) = 
        x.GetRange(itemId, 7)

    member x.Put(itemId : uint32, logs : SpecializedPacket.TradeLogRecord []) = 
        let url = x.PutUrl(itemId)
        x.DoPut(url, logs)
        