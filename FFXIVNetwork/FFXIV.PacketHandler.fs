namespace FFXIV.PacketHandler
open System
open FFXIV.PacketHandlerBase
open LibFFXIV.Network.Constants
open LibFFXIV.Network.BasePacket
open LibFFXIV.Network.SpecializedPacket
open LibDmfXiv
open LibDmfXiv.Client
open System.Collections.Generic


type TreasureMapHandler() = 
    inherit PacketHandlerBase()

    // (EventItem.MainKey:TreasureSpots.AltKey -> mapName mapX mapY
    let treasureSpots = 
        let col = new LibFFXIV.GameData.Raw.EmbeddedXivCollection(LibFFXIV.GameData.Raw.XivLanguage.ChineseSimplified)
        let col = col:> LibFFXIV.GameData.Raw.IXivCollection
        let ts =
            col.GetSheet("TreasureSpot")
            |> Seq.filter (fun x -> x.As<string>("Location") <> "0")

        let ToMapCoordinate3d(sizeFactor : int, value : single, offset : int) = 
            let c = (float sizeFactor) / 100.0
            let offsetValue = ((float value) + (float offset)) * c
            (41.0/c) * ((offsetValue + 1024.0)/ 2048.0) + 1.0

        let rank =
            col.GetSheet("TreasureHuntRank")
            |> Seq.filter (fun r -> r.As<string>("Icon") <> "0")
            |> Seq.map (fun r -> r.Key.Main, r.AsRow("KeyItemName").Key.Main)
            |> readOnlyDict

        [|
        for t in ts do 
            let key = t.Key
            let loc = t.AsRow("Location")
            let map = loc.AsRow("Map")
            //Saintcoinach的Map.json注释似乎有问题，
            //let sf = map.As<uint16>("SizeFactor") |> int
            //let offsetX = map.As<int16>("Offset{X}") |> int
            //let offsetY = map.As<int16>("Offset{Y}") |> int
            //let mapName = map.AsRow("PlaceName").As<string>("Name")

            //Saintcoinach的Map.json注释似乎有问题，
            let sf = map.As<uint16>(7) |> int
            let offsetX = map.As<int16>(8) |> int
            let offsetY = map.As<int16>(9) |> int
            let mapName = map.AsRow(11).As<string>("Name")

            let mapX = ToMapCoordinate3d(sf, loc.As<single>("X"), offsetX)
            let mapY = ToMapCoordinate3d(sf, loc.As<single>("Z"), offsetY)

            let dictKey = sprintf "%i:%i" (rank.[key.Main]) key.Alt
            let data    = sprintf "%s (%f, %f)" mapName mapX mapY
            let url     = sprintf "https://map.wakingsands.com/#f=mark&x=%f&y=%f&id=%i" mapX mapY map.Key.Main
            yield dictKey, (data + "\r\n" + url)
        |] |> readOnlyDict

    let searchTreasureSpots (eventItem : uint32) (index : uint16) = 
        let key = sprintf "%i:%i" eventItem index
        if treasureSpots.ContainsKey(key) then
            Some(treasureSpots.[key])
        else
            None

    [<PacketHandleMethodAttribute(Opcodes.UnknownInfoUpdate, PacketDirection.In)>]
    member x.HandleTreasureSpot(gp : FFXIVGamePacket) = 
        let raw = gp.Data.PeekRestBytes() |> LibFFXIV.Network.Utils.HexString.ToHex
        let unk1 = gp.Data.ReadUInt16()
        if unk1 = 0x0054us then
            x.Logger.Info(sprintf "OP:011F Data:%s" raw)
            gp.Data.ReadUInt16() |> ignore
            let eventItem = gp.Data.ReadUInt32()
            let eventItemIdx = gp.Data.ReadUInt16()
            let ret = searchTreasureSpots eventItem eventItemIdx
            if ret.IsSome then
                x.Logger.Info (sprintf "找到藏宝图地址: %s" ret.Value)

type CFNotifyHandler() = 
    inherit PacketHandlerBase()

    let instances = 
        let col = new LibFFXIV.GameData.Raw.EmbeddedXivCollection(LibFFXIV.GameData.Raw.XivLanguage.ChineseSimplified)
        let icol = col:> LibFFXIV.GameData.Raw.IXivCollection
        let cfc = icol.GetSheet("ContentFinderCondition")
        [|
            for row in cfc do 
                let k = row.Key.Main |> uint16
                let c = row.As<string>("Name")
                yield k,c
        |] |> readOnlyDict

    [<PacketHandleMethodAttribute(Opcodes.CFNotifyCHN, PacketDirection.In)>]
    member x.HandleCFNotifyCHN(gp : FFXIVGamePacket) = 
        x.Logger.Info(gp.Data.PeekRestBytes() |> LibFFXIV.Network.Utils.HexString.ToHex)

        let i = gp.Data.ReadUInt16()
        let status2 = gp.Data.ReadUInt16()
        let status1 = gp.Data.ReadUInt16()
        let status = 
            match status1 with
            | 0us -> "已发送"
            | 1us -> "不符合要求"
            | 2us -> "队长申请失败"
            | 3us -> "取消"
            | 4us -> "就绪"
            | 7us -> "关禁闭"
            | 8us -> "0x08"
            | 9us -> "连接服务器失败"
            | 10us -> "服务器错误"
            | _ -> sprintf "不明错误：%i" status1

        if instances.ContainsKey(i) then
             x.Logger.Info(sprintf "%s 排本：%s" status instances.[i])
        else
             x.Logger.Info(sprintf "%s 找不到副本信息:%i" status i)

    [<PacketHandleMethodAttribute(Opcodes.CFNotify, PacketDirection.In)>]
    member x.HandleCFNotify(gp : FFXIVGamePacket) = 
        x.Logger.Info(gp.Data.PeekRestBytes() |> LibFFXIV.Network.Utils.HexString.ToHex)

        gp.Data.ReadBytes(12) |> ignore
        let i = gp.Data.ReadUInt16()
        let status = "未知定义"
        if instances.ContainsKey(i) then
            x.Logger.Info(sprintf "%s 排本：%s" status instances.[i])
        else
            x.Logger.Info(sprintf "%s 找不到副本信息:%i" status i)

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
            let! ret = UsernameMapping.MarketOrderProxy.callSafely <@ fun server -> server.PutMapping(m) @>
            match ret with
            | Ok _ ->
                return "用户名请求 成功"
            | Error err ->
                return sprintf  "用户名请求 失败：%A" err
        } |> Async.RunSynchronously

    [<PacketHandleMethodAttribute(Opcodes.Chat, PacketDirection.In)>]
    member x.HandleChat(gp : FFXIVGamePacket) = 
        let r = new Chat(gp.Data)
        let ct = Microsoft.FSharp.Core.LanguagePrimitives.EnumOfValue<uint16, ChatType>(r.ChatType)
        x.Logger.Info("{0} {1}({2})@{3} :{4}", ct, r.UserName, r.UserId, r.ServerId, r.Text)

        if (not <| cache.Contains(r.UserId)) && Utils.RuntimeConfig.CanUploadData() then
            putMapping(r.UserId, r.UserName) |> x.Logger.Info
            cache.Add(r.UserId) |> ignore

    [<PacketHandleMethodAttribute(Opcodes.LinkshellList, PacketDirection.In)>]
    member x.HandleLinkshellList(gp : FFXIVGamePacket) = 
        let p = new LinkshellListPacket(gp.Data)
        let hex = LibFFXIV.Network.Utils.HexString.ToHex(p.Header)
        x.Logger.Info("LinkshellListHandler Header:{0}--", hex)
        for r in p.Records do 
            x.Logger.Info("{0}({1})@{2}", r.UserName, r.UserId, r.ServerId)
            if (not <| cache.Contains(r.UserId)) && Utils.RuntimeConfig.CanUploadData() then
                putMapping(r.UserId, r.UserName)  |> x.Logger.Info
                cache.Add(r.UserId) |> ignore

        x.Logger.Info("--LinkshellListHandler Header:{0}--", hex)

    [<PacketHandleMethodAttribute(Opcodes.CharacterNameLookupReply, PacketDirection.In)>]
    member x.HandleCharacterNameLookupReply(gp : FFXIVGamePacket) = 
        let r = new CharacterNameLookupReply(gp.Data)
        x.Logger.Info("收到用户名查询结果： {0} => {1}", r.UserId, r.UserName)
        
        //修正：有些奇怪的用户名第一个字节为0x00
        if Utils.RuntimeConfig.CanUploadData() && (not <| String.IsNullOrEmpty(r.UserName)) then
            putMapping(r.UserId, r.UserName)  |> x.Logger.Info

type TradeLogPacketHandler() = 
    inherit PacketHandlerBase()

    let putTradelog (logs) = 
        async {
            let! ret = TradeLog.TradelogProxy.callSafely <@ fun server -> server.PutTradeLogs(logs) @>
            match ret with
            | Ok _ ->
                return "提交交易记录 成功"
            | Error err ->
                return sprintf  "提交交易记录 失败：%A" err
        } |> Async.RunSynchronously

    [<PacketHandleMethodAttribute(Opcodes.TradeLogData, PacketDirection.In)>]
    member x.HandleData (gp : FFXIVGamePacket) = 
        let tradeLogs = new TradeLogPacket(gp.Data)
        sb {
            yield "====TradeLogData===="
            for log in tradeLogs.Records do
                let i     = Utils.Data.ItemLookupById(log.ItemId |> int)
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
            putTradelog(logs)  |> x.Logger.Info

type MarketPacketHandler ()= 
    inherit PacketHandlerBase()

    let arr = LibFFXIV.Network.Utils.XIVArray<MarketOrder>()

    let putOrders (w, i, orders) = 
        async {
            let! ret = MarketOrder.MarketOrderProxy.callSafely <@ fun server -> server.PutOrders w i orders @>
            match ret with
            | Ok _ ->
                return "提交订单 成功"
            | Error err ->
                return sprintf  "提交订单 失败：%A" err
        } |> Async.RunSynchronously

    member x.LogMarketData (mr : MarketOrder []) = 
        sb {
            yield "====MarketData===="
            for data in mr do
                let i = Utils.Data.ItemLookupById(data.ItemId |> int)
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
        putOrders(worldId, itemId, sr)  |> x.Logger.Info

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
                Console.Beep()
                x.UploadMarketData(data)
            arr.Reset()
