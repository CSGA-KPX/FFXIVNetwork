namespace FFXIV.PacketHandler
open System
open FFXIV.PacketHandlerBase
open LibFFXIV.Network.Constants
open LibFFXIV.Network.BasePacket
open LibFFXIV.Network.SpecializedPacket
open LibFFXIV.Client.Item
open System.Collections.Generic

type UserIDHandler() = 
    inherit PacketHandlerBase()

    let cache = new HashSet<uint64>()
    let dao   = new LibXIVServer.UsernameMapping.UsernameMappingDAO()

    [<PacketHandleMethodAttribute(Opcodes.Chat, PacketDirection.In)>]
    member x.HandleChat(gp) = 
        let r = Chat.ParseFromBytes(gp.Data)
        let ct = Microsoft.FSharp.Core.LanguagePrimitives.EnumOfValue<uint16, ChatType>(r.ChatType)
        x.Logger.Info("{0} {1}({2})@{3} :{4}", ct, r.Username, r.UserID, r.ServerID, r.Text)

        if (not <| cache.Contains(r.UserID)) && Utils.UploadClientData then
            dao.Put({UserID = r.UserID; Username = r.Username})
            cache.Add(r.UserID) |> ignore

    [<PacketHandleMethodAttribute(Opcodes.LinkshellList, PacketDirection.In)>]
    member x.HandleLinkshellList(gp) = 
        let p = LinkshellListPacket.ParseFromBytes(gp.Data)
        let hex = Utils.HexString.ToHex(p.Header)
        x.Logger.Info("LinkshellListHandler Header:{0}--", hex)
        for r in p.Records do 
            x.Logger.Info("{0}({1})@{2}", r.UserName, r.UserID, r.ServerID)
            if (not <| cache.Contains(r.UserID)) && Utils.UploadClientData then
                dao.Put({UserID = r.UserID; Username = r.UserName})
                cache.Add(r.UserID) |> ignore

        x.Logger.Info("--LinkshellListHandler Header:{0}--", hex)

    [<PacketHandleMethodAttribute(Opcodes.CharacterNameLookupReply, PacketDirection.In)>]
    member x.HandleCharacterNameLookupReply(gp) = 
        let r = CharacterNameLookupReply.ParseFromBytes(gp.Data)
        x.Logger.Info("收到用户名查询结果： {0} => {1}", r.UserID, r.Username)
        
        //修正：有些奇怪的用户名第一个字节为0x00
        if Utils.UploadClientData && (not <| String.IsNullOrEmpty(r.Username)) then
            dao.Put(r)

type TradeLogPacketHandler() = 
    inherit PacketHandlerBase()

    let dao = new LibXIVServer.TradeLogV2.TradeLogDAO()

    [<PacketHandleMethodAttribute(Opcodes.TradeLogData, PacketDirection.In)>]
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
                let date  = DateTimeOffset.FromUnixTimeSeconds(log.TimeStamp |> int64).ToLocalTime()
                yield sprintf "%O %s P:%i C:%i HQ:%b Buyer:%s" date iName log.Price log.Count log.IsHQ log.BuyerName
                    
            yield "====TradeLogDataEnd===="
        }
        |> buildString
        |> x.Logger.Info

        if Utils.UploadClientData then
            let itemId = tradeLogs.ItemID
            dao.Put(itemId, tradeLogs.Records)



type MaketPacketHandler ()= 
    inherit PacketHandlerBase()

    let arr = LibFFXIV.Network.Utils.XIVArray<MarketRecord>()
    let dao = LibXIVServer.MarketV2.MarketOrderDAO()

    member x.LogMarketData (mr : MarketRecord []) = 
        sb {
            yield "====MarketData===="
            for data in mr do
                let i = SaintCoinachItemProvider.GetInstance().FromId(data.Itemid |> int)
                let date  = DateTimeOffset.FromUnixTimeSeconds(data.TimeStamp |> int64).ToLocalTime()
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
        NLog.LogManager.GetCurrentClassLogger().Info("正在提交市场数据")
        let itemId = mr.[0].Itemid
        dao.Put(itemId, mr)

    [<PacketHandleMethodAttribute(Opcodes.Market, PacketDirection.In)>]
    member x.HandleMarketOrderFragment(gp) = 
        let frag = MarketPacket.ParseFromBytes(gp.Data)
        let reset = 
            let itemID = frag.Records.[0].Itemid
            arr.First.IsSome && (arr.First.Value.Itemid <> itemID)
        if reset then
            arr.Reset()
        arr.AddSlice(frag.CurrIdx |> int, frag.NextIdx |> int, frag.Records)
        
        if arr.IsCompleted then
            let data = arr.Data
            x.LogMarketData(data)
            x.LogMarketRecords(data)
            if Utils.UploadClientData then
                x.UploadMarketData(data)
            arr.Reset()

(*
type MarketListHandler() = 
    inherit PacketHandlerBase()

    let arr = LibFFXIV.Network.Utils.XIVArray<MarketListRecord>()
    let mutable reqNum = 0uy

    [<PacketHandleMethod(Opcodes.MarketListRequest, PacketDirection.Out)>]
    member x.HandleRequest(gp) = 
        let data = gp.Data
        let req  = data.[4]
        if reqNum <> req then
            x.Logger.Info(sprintf "MarketListRequestChanged : %i -> %i" reqNum req)
            arr.Reset()
            reqNum <- req


    [<PacketHandleMethod(Opcodes.MarketList, PacketDirection.In)>]
    member x.Handle (gp) = 
        let frag = MarketListPacket.FromBytes(gp.Data)
        arr.AddSlice(frag.CurrIdx |> int, frag.NextIdx |> int, frag.Records)

        if arr.IsCompleted then
            let data = arr.Data
            arr.Reset()
            sb {
                yield "====MarketList===="
                for d in data do 
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
*)