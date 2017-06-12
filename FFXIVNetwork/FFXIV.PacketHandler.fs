module FFXIV.PacketHandler
open System
open System.Text
open LibFFXIV.Constants
open LibFFXIV.GeneralPacket
open LibFFXIV.SpecializedPacket
open LibFFXIV.Database

let MarketPacketHandler2 (idx : int, gp : FFXIVGamePacket) = 
    let marketData = MarketPacket.ParseFromBytes(gp.Data)
    //MarketQueue.Instance.Enqueue(new MarketQueueItem(marketData))
    ()

let MarketPacketHandler (idx : int, gp : FFXIVGamePacket) = 
    let marketData = MarketPacket.ParseFromBytes(gp.Data)
    let sb = (new StringBuilder()).AppendFormat("====MarketData{0}/{1}====\r\n", marketData.CurrIdx, marketData.NextIdx)
    
    for data in marketData.Records do
        let price = data.Price
        let count = data.Count
        let item  = XIVItemDict.[data.Itemid |> int].ToString()
        let isHQ  = data.IsHQ
        let meld  = data.MeldCount
        let str = sprintf "%s P:%i C:%i HQ:%b Meld:%i Seller:%s" item price count isHQ meld (data.Name)
        sb.AppendLine(str) |> ignore
    sb.AppendLine("====MarketDataEnd====") |> ignore
    NLog.LogManager.GetCurrentClassLogger().Info(sb.ToString())

let TradeLogPacketHandler (idx : int, gp : FFXIVGamePacket) = 
    let tradeLogs = TradeLogPacket.ParseFromBytes(gp.Data)
    let sb = (new StringBuilder()).AppendLine("====TradeLogData====")
    for log in tradeLogs.Records do
        let item = XIVItemDict.[log.ItemID |> int].ToString()
        let str = sprintf "%s P:%i C:%i HQ:%b Buyer:%s" item log.Price log.Count log.IsHQ log.BuyerName
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
            | Opcodes.Market -> MarketPacketHandler(idx, gp)
            | Opcodes.TradeLog -> TradeLogPacketHandler(idx, gp)
            | _ -> ()) // UnknownPacketHandler(idx ,spCount , gp))