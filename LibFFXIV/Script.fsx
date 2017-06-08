// 在 http://fsharp.org 上了解有关 F# 的详细信息。请参见“F# 教程”项目
// 获取有关 F# 编程的更多指导。
#load "Utils.fs"
#load "Constants.fs"
#load "GeneralPacket.fs"
#load "SpecializedPacket.fs"
#load "DevUtils.fs"

open System
open System.IO
open LibFFXIV.GeneralPacket
open LibFFXIV.SpecializedPacket
open DevUtils


let PacketHandler (bytes : byte []) = 
    let packet = FFXIVBasePacket.ParseFromBytes(bytes)
    for sp in packet.SubPackets do
        if sp.Type = 0x0003us then
            let gp = FFXIVGamePacket.ParseFromBytes(sp.Data)
            let OpHex = Utils.HexString.toHex(BitConverter.GetBytes(gp.Opcode))
            printfn "OP:%s Data:%s" (OpHex) (Utils.HexString.toHex(gp.Data))


let LowPacketLog = 
    let path = @"Z:\KPX\Documents\Visual Studio 2017\Projects\FFXIVNetwork\FFXIVNetwork\bin\Debug\LoggingLowPacket.txt"
    File.ReadAllLines(path)
    |> Array.filter (fun x -> x.ToUpper().StartsWith(LibFFXIV.Constants.FFXIVBasePacketMagic))
    |> Array.map (fun x -> Utils.HexString.toBytes(x))

LowPacketLog
|> Array.iter (fun x -> PacketHandler(x))
// 在此处定义库脚本代码

