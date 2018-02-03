// 在 http://fsharp.org 上了解有关 F# 的详细信息。请参见“F# 教程”项目
// 获取有关 F# 编程的更多指导。
#r @"Z:\KPX\Documents\Visual Studio 2017\Projects\FFXIVNetwork\packages\NLog.4.4.10\lib\net45\NLog.dll"
#r @"C:\Users\dmmik\.nuget\packages\fsharp.data\2.3.3\lib\net40\FSharp.Data.dll"
#r @"bin\Debug\LibFFXIV.dll"
#r @"bin\Debug\YamlDotNet.dll"
#r @"bin\Debug\Newtonsoft.Json.dll"
#r @"bin\Debug\Ionic.Zip.dll"
#r @"bin\Debug\DotSquish.dll"
#r @"bin\Debug\EntityFramework.dll"
#r @"bin\Debug\SaintCoinach.dll"

open System
open System.IO
open LibFFXIV.Network.BasePacket
open LibFFXIV.Network.SpecializedPacket
open LibFFXIV.Network.Utils

let UTF8  = Text.Encoding.UTF8
let ParseString(str) = FFXIVBasePacket.ParseFromBytes(HexString.ToBytes(str))

let bytes = HexString.ToBytes "5252A041FF5D46E27F2A644D7B99C47500DF9532610100005A000000000001000101000000000000789C73606060607BC72800C2CC0CDE55220CCE8C0C0C1AAC591AD9514029060986F50F1BAC3819B08100218346003E2D086D"