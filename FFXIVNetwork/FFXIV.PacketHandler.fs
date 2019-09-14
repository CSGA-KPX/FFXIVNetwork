namespace FFXIV.PacketHandler
open System
open FFXIV.PacketHandlerBase
open LibFFXIV.Network.Constants
open LibFFXIV.Network.BasePacket
open LibFFXIV.Network.SpecializedPacket
open LibFFXIV.ClientData
open LibDmfXiv
open LibDmfXiv.Client
open System.Collections.Generic

type PlayerSpawnHandler() = 
    inherit PacketHandlerBase()
    [<PacketHandleMethodAttribute(Opcodes.PlayerSpawn, PacketDirection.In)>]
    member x.HandlePlayerSpawn(gp : FFXIVGamePacket) = 
        let p = new PlayerSpawn(gp.Data)
        if Utils.RuntimeConfig.CurrentWorld <> p.CurrentServerId then
            x.Logger.Info("服务器变更:{0} -> {1}", Utils.RuntimeConfig.CurrentWorld, p.CurrentServerId)
            Utils.RuntimeConfig.CurrentWorld <- p.CurrentServerId
        x.Logger.Trace("PlayerSpawn: {0}->{1} {2}<{3}>", p.OriginalServerId, p.CurrentServerId, p.PlayerName, p.FreeCompanyName)

type UserIDHandler() = 
    inherit PacketHandlerBase()

    let cache = new HashSet<string>()

    let putMapping(id, name) = 
        async {
            let m = LibDmfXiv.Shared.UsernameMapping.FabelUsernameMapping.CreateFrom(id, name)
            do! UsernameMapping.MarketOrderProxy.call <@ fun server -> server.PutMapping(m) @>
        } |> Async.RunSynchronously

    [<PacketHandleMethodAttribute(Opcodes.Chat, PacketDirection.In)>]
    member x.HandleChat(gp : FFXIVGamePacket) = 
        let r = new Chat(gp.Data)
        let ct = Microsoft.FSharp.Core.LanguagePrimitives.EnumOfValue<uint16, ChatType>(r.ChatType)
        x.Logger.Info("{0} {1}({2})@{3} :{4}", ct, r.UserName, r.UserId, r.ServerId, r.Text)

        if (not <| cache.Contains(r.UserId)) && Utils.RuntimeConfig.CanUploadData() then
            
            putMapping(r.UserId, r.UserName)
            cache.Add(r.UserId) |> ignore

    [<PacketHandleMethodAttribute(Opcodes.LinkshellList, PacketDirection.In)>]
    member x.HandleLinkshellList(gp : FFXIVGamePacket) = 
        let p = new LinkshellListPacket(gp.Data)
        let hex = LibFFXIV.Network.Utils.HexString.ToHex(p.Header)
        x.Logger.Info("LinkshellListHandler Header:{0}--", hex)
        for r in p.Records do 
            x.Logger.Info("{0}({1})@{2}", r.UserName, r.UserId, r.ServerId)
            if (not <| cache.Contains(r.UserId)) && Utils.RuntimeConfig.CanUploadData() then
                putMapping(r.UserId, r.UserName)
                cache.Add(r.UserId) |> ignore

        x.Logger.Info("--LinkshellListHandler Header:{0}--", hex)

    [<PacketHandleMethodAttribute(Opcodes.CharacterNameLookupReply, PacketDirection.In)>]
    member x.HandleCharacterNameLookupReply(gp : FFXIVGamePacket) = 
        let r = new CharacterNameLookupReply(gp.Data)
        x.Logger.Info("收到用户名查询结果： {0} => {1}", r.UserId, r.UserName)
        
        //修正：有些奇怪的用户名第一个字节为0x00
        if Utils.RuntimeConfig.CanUploadData() && (not <| String.IsNullOrEmpty(r.UserName)) then
            putMapping(r.UserId, r.UserName)

type TradeLogPacketHandler() = 
    inherit PacketHandlerBase()

    let putTradelog (logs) = 
        async {
            do! TradeLog.TradelogProxy.call <@ fun server -> server.PutTradeLogs(logs) @>
        } |> Async.RunSynchronously

    [<PacketHandleMethodAttribute(Opcodes.TradeLogData, PacketDirection.In)>]
    member x.HandleData (gp : FFXIVGamePacket) = 
        let tradeLogs = new TradeLogPacket(gp.Data)
        sb {
            yield "====TradeLogData===="
            for log in tradeLogs.Records do
                let i     = Item.LookupById(log.ItemId |> int)
                let iName = 
                    if i.IsSome then
                        i.Value.ToString()
                    else
                        sprintf "未知物品 XIVId(%i)" log.ItemId
                let date  = DateTimeOffset.FromUnixTimeSeconds(log.TimeStamp |> int64).ToLocalTime()
                yield sprintf "%O %s P:%i C:%i HQ:%b Buyer:%s" date iName log.Price log.Count log.IsHQ log.BuyerName
                    
            yield "====TradeLogDataEnd===="
        }
        |> buildString
        |> x.Logger.Info

        if Utils.RuntimeConfig.CanUploadData() && Utils.RuntimeConfig.IsWorldReady() then
            let itemId = tradeLogs.ItemID
            let logs = 
                tradeLogs.Records
                |> Array.map (fun x -> Shared.TradeLog.FableTradeLog.CreateFrom(Utils.RuntimeConfig.CurrentWorld, x))
            putTradelog(logs)

type MarketPacketHandler ()= 
    inherit PacketHandlerBase()

    let arr = LibFFXIV.Network.Utils.XIVArray<MarketOrder>()

    let putOrders (w, i, orders) = 
        async {
            do! MarketOrder.MarketOrderProxy.call <@ fun server -> server.PutOrders w i orders @>
        } |> Async.RunSynchronously

    member x.LogMarketData (mr : MarketOrder []) = 
        sb {
            yield "====MarketData===="
            for data in mr do
                let i = Item.LookupById(data.ItemId |> int)
                let date  = DateTimeOffset.FromUnixTimeSeconds(data.TimeStamp |> int64).ToLocalTime()
                let price = data.Price
                let count = data.Count
                let isHQ  = data.IsHQ
                let meld  = data.MeldCount
                let str = 
                    if i.IsSome then
                        i.Value.ToString()
                    else
                        sprintf "未知物品 XIVId(%i)" data.ItemId
                yield sprintf "%O %s P:%i C:%i HQ:%b Meld:%i Seller:%s" date str price count isHQ meld (data.Name)
            yield "====MarketDataEnd===="
        } |> buildString |> x.Logger.Info

    member x.UploadMarketData (mr : MarketOrder []) = 
        NLog.LogManager.GetCurrentClassLogger().Info("正在提交市场数据")
        let itemId = mr.[0].ItemId
        let worldId= Utils.RuntimeConfig.CurrentWorld
        let sr = 
            mr 
            |> Array.map (fun x -> Shared.MarketOrder.FableMarketOrder.CreateFrom(worldId, x))
        putOrders(worldId, itemId, sr)

    [<PacketHandleMethodAttribute(Opcodes.Market, PacketDirection.In)>]
    member x.HandleMarketOrderFragment(gp : FFXIVGamePacket) = 
        let frag = new MarketOrderPacket(gp.Data)
        let reset = 
            let itemID = frag.Records.[0].ItemId
            arr.First.IsSome && (arr.First.Value.ItemId <> itemID)
        if reset then
            arr.Reset()
        arr.AddSlice(frag.CurrIdx |> int, frag.NextIdx |> int, frag.Records)
        
        if arr.IsCompleted then
            let data = arr.Data
            x.LogMarketData(data)
            if Utils.RuntimeConfig.CanUploadData() && Utils.RuntimeConfig.IsWorldReady() then
                x.UploadMarketData(data)
            arr.Reset()
