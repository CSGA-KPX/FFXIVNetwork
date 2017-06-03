module FFXIV.PacketHandler
open System.Text
open FFXIV.PacketTypes

let PacketHandler (bytes : byte []) = 
    let packet = FFXIVBasePacket.ParseFromBytes(bytes)
    for sp in packet.SubPackets do
        if sp.Type = 0x0003us then
            let gp = FFXIVGamePacket.ParseFromBytes(sp.Data)
            match LanguagePrimitives.EnumOfValue<uint16, Opcodes>(gp.Opcode) with
            | Opcodes.Market ->
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
            | Opcodes.TradeLog ->
                let tradeLogs = TradeLogPacket.ParseFromBytes(gp.Data)
                let sb = (new StringBuilder()).AppendLine("====TradeLogData====")
                for log in tradeLogs.Records do
                    let item = Database.XIVItemDict.[log.ItemID |> int].ToString()
                    let str = sprintf "%s P:%i C:%i HQ:%b Buyer:%s" item log.Price log.Count log.IsHQ log.BuyerName
                    sb.AppendLine(str) |> ignore
                sb.AppendLine("====TradeLogDataEnd====") |> ignore
                NLog.LogManager.GetCurrentClassLogger().Info(sb.ToString())
            | _ -> ()