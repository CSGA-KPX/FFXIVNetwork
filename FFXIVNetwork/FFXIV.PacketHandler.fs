module FFXIV.LobbyPacketHandler
open System
open FFXIV.PacketHandlerBase

open LibFFXIV.Network.Constants
open LibFFXIV.Network.BasePacket
open LibFFXIV.Network.SpecializedPacket
open LibFFXIV.Client.Item

type TradeLogPacketHandler() = 
    inherit PacketHandlerBase()

    //[<PacketHandleMethodAttribute(Opcodes.TradeLogInfo)>]
    //member x.HandleInfo (gp) = 
    //    ()

    [<PacketHandleMethodAttribute(Opcodes.TradeLogData)>]
    member x.HandleData (gp) = 
        let tradeLogs = TradeLogPacket.ParseFromBytes(gp.Data)
        sb {
            yield "====TradeLogData===="
            for log in tradeLogs.Records do
                let i     = SaintCoinachItemProvider.GetInstance().FromId(log.ItemID |> int)
                let iName = 
                    if i.IsSome then
                        i.Value.ToString()
                    else
                        sprintf "未知物品 XIVId(%i)" log.ItemID
                let date  = LibFFXIV.Network.Utils.TimeStamp.FromSeconds(log.TimeStamp)
                yield sprintf "%O %s P:%i C:%i HQ:%b Buyer:%s" date iName log.Price log.Count log.IsHQ log.BuyerName
                    
            yield "====TradeLogDataEnd===="
        }
        |> buildString
        |> x.Logger.Info

        if Utils.UploadClientData then
            LibXIVServer.TradeLogV2.PutTradeLog(tradeLogs.Records)



type MaketPacketHandler () as x = 
    inherit PacketHandlerBase()

    let queue = new MarketQueue()

    do
        queue.NewCompleteDataEvent.Add(x.LogMarketData)
        queue.NewCompleteDataEvent.Add(x.LogMarketRecords)
        if Utils.UploadClientData then
            queue.NewCompleteDataEvent.Add(x.UploadMarketData)

    member x.LogMarketData (mr : MarketRecord []) = 
        sb {
            yield "====MarketData===="
            for data in mr do
                let i = SaintCoinachItemProvider.GetInstance().FromId(data.Itemid |> int)
                let date  = LibFFXIV.Network.Utils.TimeStamp.FromSeconds(data.TimeStamp)
                let price = data.Price
                let count = data.Count
                let isHQ  = data.IsHQ
                let meld  = data.MeldCount
                let str = 
                    if i.IsSome then
                        i.Value.ToString()
                    else
                        sprintf "未知物品 XIVId(%i)" data.Itemid
                yield sprintf "%O %s P:%i C:%i HQ:%b Meld:%i Seller:%s" date str price count isHQ meld (data.Name)
            yield "====MarketDataEnd===="
        } |> buildString |> x.Logger.Info
    member x.LogMarketRecords (mr : MarketRecord []) = 
        for m in mr do 
            x.Logger.Trace(sprintf "%A" m)

    member x.UploadMarketData (mr : MarketRecord []) = 
        Threading.ThreadPool.QueueUserWorkItem(fun _ -> 
            NLog.LogManager.GetCurrentClassLogger().Info("正在提交市场数据")
            LibXIVServer.MarketV2.PutRawOrders(mr)
        ) |> ignore

    [<PacketHandleMethodAttribute(Opcodes.Market)>]
    member x.HandleMarketOrderFragment(gp) = 
        let frag = MarketPacket.ParseFromBytes(gp.Data)
        queue.Enqueue(frag)


type MarketListHandler() = 
    inherit PacketHandlerBase()

    [<PacketHandleMethod(Opcodes.MarketList)>]
    member x.Handle (gp) = 
        sb {
            yield "====MarketList===="
            let mlp= MarketListPacket.FromBytes(gp.Data)
            for d in mlp.Records do 
                let item = SaintCoinachItemProvider.GetInstance().FromId(d.ItemId |>int)
                let iName = 
                    if item.IsSome then
                        item.Value.ToString()
                    else
                        sprintf "未知物品 XIVId(%i)" d.ItemId
                yield sprintf "物品:%s 订单数:%i 需求 %i"  iName d.Count  d.Demand
            yield "====MarketListEnd===="
        }
        |> buildString
        |> x.Logger.Info