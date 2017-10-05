module FFXIV.PacketHandler
open System
open System.Text
open LibFFXIV.Constants
open LibFFXIV.BasePacket
open LibFFXIV.SpecializedPacket
open LibFFXIV.Database

let logger = NLog.LogManager.GetCurrentClassLogger()

let tester (ra : MarketRecord []) = 
    let sb = (new StringBuilder()).AppendFormat("====MarketData====\r\n")
    
    for data in ra do
        let i = SaintCoinachItemProvider.GetInstance().FromId(data.Itemid |> int)
        let date  = LibFFXIV.Utils.TimeStamp.FromSeconds(data.TimeStamp)
        let price = data.Price
        let count = data.Count
        let isHQ  = data.IsHQ
        let meld  = data.MeldCount
        let str = 
            if i.IsSome then
                i.Value.ToString()
            else
                sprintf "未知物品 XIVId(%i)" data.Itemid
        sb.AppendLine(sprintf "%O %s P:%i C:%i HQ:%b Meld:%i Seller:%s" date str price count isHQ meld (data.Name)) |> ignore
    sb.AppendLine("====MarketDataEnd====") |> ignore
    NLog.LogManager.GetCurrentClassLogger().Info(sb.ToString())

let submitData (ra : MarketRecord []) = 
    Threading.ThreadPool.QueueUserWorkItem(fun _ -> 
        NLog.LogManager.GetCurrentClassLogger().Info("正在提交市场数据")
        LibXIVDMF.Market.SubmitMarketData(ra)
    ) |> ignore
    
let marketQueue = 
    let i  = new MarketQueue()
    i.NewCompleteDataEvent.Add(tester)
    i.NewCompleteDataEvent.Add(submitData)
    i

let MarketPacketHandler2 (gp : FFXIVGamePacket) = 
    let marketData = MarketPacket.ParseFromBytes(gp.Data)
    marketQueue.Enqueue(MarketPacket.ParseFromBytes(gp.Data))

let TradeLogPacketHandler (gp : FFXIVGamePacket) = 
    let tradeLogs = TradeLogPacket.ParseFromBytes(gp.Data)
    let sb = (new StringBuilder()).AppendLine("====TradeLogData====")
    for log in tradeLogs.Records do
        let i     = SaintCoinachItemProvider.GetInstance().FromId(log.ItemID |> int)
        let iName = 
            if i.IsSome then
                i.Value.ToString()
            else
                sprintf "未知物品 XIVId(%i)" log.ItemID
        let date  = LibFFXIV.Utils.TimeStamp.FromSeconds(log.TimeStamp)
        let str = sprintf "%O %s P:%i C:%i HQ:%b Buyer:%s" date iName log.Price log.Count log.IsHQ log.BuyerName
        sb.AppendLine(str) |> ignore
    sb.AppendLine("====TradeLogDataEnd====") |> ignore
    NLog.LogManager.GetCurrentClassLogger().Info(sb.ToString())

let LogGamePacket (idx, total, gp : FFXIVGamePacket) = 
    let opcode = Utils.HexString.ToHex (BitConverter.GetBytes(gp.Opcode))
    let ts     = gp.TimeStamp
    let data   = Utils.HexString.ToHex (gp.Data)
    NLog.LogManager.GetCurrentClassLogger().Trace("GamePacket: MA:{5} OP:{0} TS:{1} {2}/{3} Data:{4}", opcode, ts, idx + 1, total + 1, data, gp.Magic)

let MarketListHandler (gp : FFXIVGamePacket) = 
    let sb = new StringBuilder("====MarketList====\r\n")
    let mlp= MarketListPacket.FromBytes(gp.Data)
    for d in mlp.Records do 
        let item = SaintCoinachItemProvider.GetInstance().FromId(d.ItemId |>int)
        let iName = 
            if item.IsSome then
                item.Value.ToString()
            else
                sprintf "未知物品 XIVId(%i)" d.ItemId
        sb.AppendFormat("物品:{0} 订单数:{1} 需求{2}\r\n", iName, d.Count, d.Demand) |> ignore
    sb.AppendLine("====MarketListEnd====") |> ignore
    NLog.LogManager.GetCurrentClassLogger().Info(sb.ToString())

let WorldListHandler(gp : FFXIVGamePacket) = 
    let worlds = WorldList.ParseFromBytes(gp.Data)
    for world in worlds do 
        NLog.LogManager.GetCurrentClassLogger().Info("添加服务器 {0}({1}) 到缓存", world.WorldName, world.WorldId)
        Utils.DictionaryAddOrUpdate(GlobalVars.WorldsIdToWorld, world.WorldId, world)

let CharacterListHandler (gp : FFXIVGamePacket) = 
    let list = CharacterList.ParseFromBytes(gp.Data)
    for char in list.Charas do 
        NLog.LogManager.GetCurrentClassLogger().Info("添加{2}角色 {0}({1}) 到缓存", char.UserId, char.UserName, char.WorldId)
        Utils.DictionaryAddOrUpdate(GlobalVars.Character, char.UserId, char)

let CharaSelectReply (gp : FFXIVGamePacket) = 
    let reply = CharaSelectReply.ParseFromBytes(gp.Data)
    let char  = GlobalVars.Character.[reply.CharacterId]
    let world = GlobalVars.WorldsIdToWorld.[char.WorldId]
    let logger = NLog.LogManager.GetCurrentClassLogger()
    logger.Info("以角色:{0}({1})登陆到服务器{2}({3}) : {4}:{5}", char.UserName, reply.CharacterId
                                                     , world.WorldName, world.WorldId
                                                     , reply.WorldIP, reply.WorldPort)
    logger.Info("添加{0}:{1}到服务器列表缓存", reply.WorldIP, reply.WorldPort)
    Utils.DictionaryAddOrUpdate(GlobalVars.ServerIpToWorld, reply.WorldIP, world)

let IncomingGamePacketHandler (gp : FFXIVGamePacket) = 
    match LanguagePrimitives.EnumOfValue<uint16, Opcodes>(gp.Opcode) with
    | Opcodes.Market -> MarketPacketHandler2(gp)
    | Opcodes.TradeLog -> TradeLogPacketHandler(gp)
    | Opcodes.MarketList -> MarketListHandler(gp)
    | Opcodes.WorldList -> WorldListHandler(gp)
    | Opcodes.CharaList -> CharacterListHandler(gp)
    | Opcodes.SelectCharaReply -> CharaSelectReply(gp)
    | _ -> ()

let OutgoingGamePacketHandler (gp : FFXIVGamePacket) = 
    match LanguagePrimitives.EnumOfValue<uint16, Opcodes>(gp.Opcode) with
    | _ -> ()

let HandleClientHandshake(sp : FFXIVSubPacket) = 
    let ticketAscii = 
        let bytes = sp.Data.[36 .. 36 + 0x20 - 1]
        Encoding.ASCII.GetString(bytes)
    let clientNumber = BitConverter.ToUInt32(sp.Data, 100)
    logger.Info("Ticket{0} ClientNumber{1}", ticketAscii, clientNumber)
    LibFFXIV.Utils.InitBlowfish(ticketAscii, clientNumber)
    


let PacketHandler (p : LibFFXIV.TcpPacket.QueuedPacket) = 
    let isLobby = p.World.IsLobby
    let bytes   = p.Data
    try
        let packet = FFXIVBasePacket.ParseFromBytes(bytes)
        let subPackets = 
            let sp = packet.GetSubPackets()
            let rdy= LibFFXIV.Utils.IsDecipherReady()
            let dec= 
                if sp.Length <> 0 then
                    let en = LanguagePrimitives.EnumOfValue<uint16, PacketTypes>(sp.[0].Type)
                    match en with
                    | PacketTypes.ClientHandShake
                    | PacketTypes.Ping
                    | PacketTypes.Pong
                        -> false
                    | _ -> true
                else
                    false
            if isLobby && (dec) then
                if rdy then
                    sp
                    |> Array.map (fun sp ->
                        {sp with Data = LibFFXIV.Utils.DecipherData(sp.Data)}
                    )
                else
                    failwithf "需要解密，但解密器未就绪"
            else
                sp

        let spCount= subPackets.Length - 1

        for idx = 0 to spCount do
            let sp   = subPackets.[idx]
            let en   = LanguagePrimitives.EnumOfValue<uint16, PacketTypes>(sp.Type)
            match en with
            | PacketTypes.Game  ->
                let gp = FFXIVGamePacket.ParseFromBytes(sp.Data)
                LogGamePacket(idx, spCount, gp)
                match p.Direction with
                | LibFFXIV.TcpPacket.PacketDirection.In  -> IncomingGamePacketHandler(gp)
                | LibFFXIV.TcpPacket.PacketDirection.Out -> OutgoingGamePacketHandler(gp)
                
            | PacketTypes.ServerHandShake ->
                logger.Info("Server say hello!")
            | PacketTypes.ClientHandShake ->
                HandleClientHandshake(sp)
            | PacketTypes.Ping
            | PacketTypes.Pong
                -> ()
            | _ -> 
                let t = Utils.HexString.ToHex (BitConverter.GetBytes(sp.Type))
                let d = Utils.HexString.ToHex (sp.Data)
                logger.Info("Unknown SubPacket isLobby({0}): Type:{1} DATA:{2}", isLobby, sp.Type, d)
    with
    | e ->  
        NLog.LogManager.GetCurrentClassLogger().Error("Error packet:{0}", Utils.HexString.ToHex(bytes))
