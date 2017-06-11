﻿// 在 http://fsharp.org 上了解有关 F# 的详细信息。请参见“F# 教程”项目
// 获取有关 F# 编程的更多指导。
#r @"Z:\KPX\Documents\Visual Studio 2017\Projects\FFXIVNetwork\packages\NLog.4.4.10\lib\net45\NLog.dll"
#load "Utils.fs"
#load "Constants.fs"
#load "GeneralPacket.fs"
#load "SpecializedPacket.fs"
//#load "DevUtils.fs"

open System
open System.IO
open LibFFXIV.GeneralPacket
open LibFFXIV.SpecializedPacket
open LibFFXIV.Utils


let PacketHandler (bytes : byte []) = 
    let packet = FFXIVBasePacket.ParseFromBytes(bytes)
    for sp in packet.SubPackets do
        if sp.Type = 0x0003us then
            let gp = FFXIVGamePacket.ParseFromBytes(sp.Data)
            let OpHex = HexString.ToHex(BitConverter.GetBytes(gp.Opcode))
            printfn "OP:%s Data:%s" (OpHex) (HexString.ToHex(gp.Data))


let LowPacketLog () = 
    let path = @"Z:\KPX\Documents\Visual Studio 2017\Projects\FFXIVNetwork\FFXIVNetwork\bin\Debug\LoggingLowPacket.txt"
    File.ReadAllLines(path)
    |> Array.filter (fun x -> x.ToUpper().StartsWith(LibFFXIV.Constants.FFXIVBasePacketMagic))
    |> Array.map (fun x -> HexString.ToBytes(x))

//LowPacketLog
//|> Array.iter (fun x -> PacketHandler(x))
// 在此处定义库脚本代码

let hex3 = "D32F0000D32F0000DB9B00007A203C59010000000000E998BFE7A986E88A99E88EB1E789B900000000000000000000000000000000000000D32F0000369C00004A153C59010000000000E995BFE6AD8CE4B99DE5B79E00000000000000000000000000000000000000000000D32F0000359C00002F143C59010000000000E889BEE9A39BE5A4A900000000000000000000000000000000000000000000000000D32F0000409C00005A0C3C59010000000000E892BCE69C88E4B8B6E687BAE68294E8A9A900000000000000000000000000000000D32F00003F9C0000240C3C590100000000005376656E000000000000000000000000000000000000000000000000000000000000D32F00003F9C00000F0C3C590100000000005376656E000000000000000000000000000000000000000000000000000000000000D32F0000409C0000F90B3C59010000000000E5A4A9E9AD94E5A49CE5888000000000000000000000000000000000000000000000D32F00003F9C0000EF0B3C59010000000000E59388E5858BE5A5A5E7BD9700000000000000000000000000000000000000000000D32F00003F9C0000DF0B3C59010000000000E5A4A9E9AD94E5A49CE5888000000000000000000000000000000000000000000000D32F00003F9C00009B0B3C59010000000000E79A87E794ABE5878CE69C8800000000000000000000000000000000000000000000D32F00003F9C0000880B3C59010000000000E79A87E794ABE5878CE69C8800000000000000000000000000000000000000000000D32F0000D29B0000F2093C590100000000005772796E6E0000000000000000000000000000000000000000000000000000000000D32F000058980000E5093C59010000000000E889BEE89C9CE88E89E4BA9A00000000000000000000000000000000000000000000D32F0000C49B0000D6093C590100000000005772796E6E0000000000000000000000000000000000000000000000000000000000D32F000058980000BE093C59010000000000E889BEE89C9CE88E89E4BA9A00000000000000000000000000000000000000000000D32F00005898000088093C59010000000000E998BFE7A986E88A99E88EB1E789B900000000000000000000000000000000000000D32F00004E98000046093C59010000000000E69DB0E8A5BFC2B7E9BAA5E5858BE99BB70000000000000000000000000000000000D32F00004D98000033093C59010000000000E69DB0E8A5BFC2B7E9BAA5E5858BE99BB70000000000000000000000000000000000D32F00004E980000B7073C59010000000000E788B1E5A48F00000000000000000000000000000000000000000000000000000000D32F00005698000041073C59010000000000E9BD90E69E95E9A38E0000000000000000000000000000000000000000000000000000000000"

let bytes = HexString.ToBytes(hex3)
let recordSize = 52
let chunks = 
    bytes 
    |> Array.chunkBySize recordSize