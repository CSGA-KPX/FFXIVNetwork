module FFXIV.PacketHandler
open System
open System.Text
open LibFFXIV.Constants
open LibFFXIV.GeneralPacket
open LibFFXIV.SpecializedPacket

let MarketPacketHandler (gp : FFXIVGamePacket) = 
    let marketDatas = MarketPacket.ParseFromBytes(gp.Data)
    let sb = (new StringBuilder()).AppendLine("====MarketData====")
    for data in marketDatas do
        let price = data.Price
        let count = data.Count
        let item  = Database.XIVItemDict.[data.Itemid |> int].ToString()
        let isHQ  = data.IsHQ
        let meld  = data.MeldCount
        let str = sprintf "%s P:%i C:%i HQ:%b Meld:%i Seller:%s" item price count isHQ meld (data.Name)
        sb.AppendLine(str) |> ignore
    sb.AppendLine("====MarketDataEnd====") |> ignore
    NLog.LogManager.GetCurrentClassLogger().Info(sb.ToString())

let TradeLogPacketHandler (gp : FFXIVGamePacket) = 
    let tradeLogs = TradeLogPacket.ParseFromBytes(gp.Data)
    let sb = (new StringBuilder()).AppendLine("====TradeLogData====")
    for log in tradeLogs.Records do
        let item = Database.XIVItemDict.[log.ItemID |> int].ToString()
        let str = sprintf "%s P:%i C:%i HQ:%b Buyer:%s" item log.Price log.Count log.IsHQ log.BuyerName
        sb.AppendLine(str) |> ignore
    sb.AppendLine("====TradeLogDataEnd====") |> ignore
    NLog.LogManager.GetCurrentClassLogger().Info(sb.ToString())


let UnknownPacketHandler (gp : FFXIVGamePacket) = 
    let opcode = Utils.HexString.toHex (BitConverter.GetBytes(gp.Opcode))
    let ts     = gp.TimeStamp
    let data   = Utils.HexString.toHex (gp.Data)
    NLog.LogManager.GetCurrentClassLogger().Info("UnknownGamePacket: Opcode:{0} TS:{1} Data:{2}", opcode, ts, data)
    
let logger = NLog.LogManager.GetCurrentClassLogger()

let PacketHandler (bytes : byte []) = 
    let packet = FFXIVBasePacket.ParseFromBytes(bytes)
    for sp in packet.SubPackets do
        if sp.Type = 0x0003us then
            let gp = FFXIVGamePacket.ParseFromBytes(sp.Data)
            match LanguagePrimitives.EnumOfValue<uint16, Opcodes>(gp.Opcode) with
            | Opcodes.Market -> MarketPacketHandler(gp)
            | Opcodes.TradeLog -> TradeLogPacketHandler(gp)
            | _ -> UnknownPacketHandler(gp)