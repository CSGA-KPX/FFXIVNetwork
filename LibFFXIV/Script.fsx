// 在 http://fsharp.org 上了解有关 F# 的详细信息。请参见“F# 教程”项目
// 获取有关 F# 编程的更多指导。
#r @"..\packages\NLog.4.5.6\lib\net45\NLog.dll"
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
let ParseString(str:string) = FFXIVGamePacket.ParseFromBytes(new ByteArray(str))
