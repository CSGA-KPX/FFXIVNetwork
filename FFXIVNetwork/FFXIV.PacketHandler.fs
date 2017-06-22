module FFXIV.PacketHandler
open System
open System.Text
open LibFFXIV.Constants
open LibFFXIV.GeneralPacket
open LibFFXIV.SpecializedPacket
open LibFFXIV.Database

let tester (ra : MarketRecord []) = 
    let sb = (new StringBuilder()).AppendFormat("====MarketData====\r\n")
    
    for data in ra do
        let date  = LibFFXIV.Utils.TimeStamp.FromSeconds(data.TimeStamp)
        let price = data.Price
        let count = data.Count
        let item  = SuItemData.Instance.FromXIVId(data.Itemid |> int).Value.ToString()
        //let item  = LibFFXIV.Database.XIVItemDict.[data.Itemid |> int].ToString()
        let isHQ  = data.IsHQ
        let meld  = data.MeldCount
        let str = sprintf "%O %s P:%i C:%i HQ:%b Meld:%i Seller:%s" date item price count isHQ meld (data.Name)
        sb.AppendLine(str) |> ignore
    sb.AppendLine("====MarketDataEnd====") |> ignore
    NLog.LogManager.GetCurrentClassLogger().Info(sb.ToString())

let submitData (ra : MarketRecord []) = 
    NLog.LogManager.GetCurrentClassLogger().Info("正在提交市场数据")
    LibXIVDMF.Market.SubmitMarketData(ra)
    
let marketQueue = 
    let i  = new MarketQueue()
    i.NewCompleteDataEvent.Add(tester)
    i.NewCompleteDataEvent.Add(submitData)
    i

let MarketPacketHandler2 (idx : int, gp : FFXIVGamePacket) = 
    let marketData = MarketPacket.ParseFromBytes(gp.Data)
    marketQueue.Enqueue(MarketPacket.ParseFromBytes(gp.Data))

let TradeLogPacketHandler (idx : int, gp : FFXIVGamePacket) = 
    let tradeLogs = TradeLogPacket.ParseFromBytes(gp.Data)
    let sb = (new StringBuilder()).AppendLine("====TradeLogData====")
    for log in tradeLogs.Records do
        let item  = SuItemData.Instance.FromXIVId(log.ItemID |> int).Value.ToString()
        let date  = LibFFXIV.Utils.TimeStamp.FromSeconds(log.TimeStamp)
        let str = sprintf "%O %s P:%i C:%i HQ:%b Buyer:%s" date item log.Price log.Count log.IsHQ log.BuyerName
        sb.AppendLine(str) |> ignore
    sb.AppendLine("====TradeLogDataEnd====") |> ignore
    NLog.LogManager.GetCurrentClassLogger().Info(sb.ToString())

let AllPacketHandler (idx, total, gp : FFXIVGamePacket) = 
    let opcode = Utils.HexString.ToHex (BitConverter.GetBytes(gp.Opcode))
    let ts     = gp.TimeStamp
    let data   = Utils.HexString.ToHex (gp.Data)
    NLog.LogManager.GetCurrentClassLogger().Trace("UnknownGamePacket: MA:{5} OP:{0} TS:{1} {2}/{3} Data:{4}", opcode, ts, idx, total, data, gp.Magic)

let UnknownPacketHandler (idx, total, gp : FFXIVGamePacket) = 
    let opcode = Utils.HexString.ToHex (BitConverter.GetBytes(gp.Opcode))
    let ts     = gp.TimeStamp
    let data   = Utils.HexString.ToHex (gp.Data)
    NLog.LogManager.GetCurrentClassLogger().Trace("UnknownGamePacket: MA:{5} OP:{0} TS:{1} {2}/{3} Data:{4}", opcode, ts, idx, total, data, gp.Magic)
    

let MarketListHandler (idx, gp : FFXIVGamePacket) = 
    let sb = new StringBuilder("====MarketList====\r\n")
    let mlp= MarketListPacket.FromBytes(gp.Data)
    for d in mlp.Records do 
        let item = ItemProvider.FromXIVId(d.ItemId |>int).Value
        sb.AppendFormat("物品:{0} 订单数:{1} 需求{2}\r\n", item, d.Count, d.Demand) |> ignore
    sb.AppendLine("====MarketListEnd====") |> ignore
    NLog.LogManager.GetCurrentClassLogger().Info(sb.ToString())

let logger = NLog.LogManager.GetCurrentClassLogger()

let PacketHandler (bytes : byte []) = 
    let packet = FFXIVBasePacket.ParseFromBytes(bytes)
    let spCount= packet.SubPackets.Length - 1
    packet.SubPackets
    |> Array.iteri (fun idx sp ->
        if sp.Type = 0x0003us then
            let gp = FFXIVGamePacket.ParseFromBytes(sp.Data)
            AllPacketHandler(idx ,spCount , gp)
            match LanguagePrimitives.EnumOfValue<uint16, Opcodes>(gp.Opcode) with
            | Opcodes.Market -> MarketPacketHandler2(idx, gp)
            | Opcodes.TradeLog -> TradeLogPacketHandler(idx, gp)
            | Opcodes.MarketList -> MarketListHandler(idx, gp)
            | _ -> ()) // UnknownPacketHandler(idx ,spCount , gp))