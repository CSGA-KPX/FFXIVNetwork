namespace LibFFXIV.Network.SpecializedPacket
open Microsoft.FSharp.Core.Operators.Checked
open System
open System.IO
open System.Text
open LibFFXIV.Network.Utils

[<CLIMutableAttribute>]
type TradeLogRecord = 
    {
        ItemID      : uint32
        Price       : uint32
        TimeStamp   : uint32
        Count       : uint32
        IsHQ        : bool
        Unknown     : uint32
        BuyerName  : string
    }
    static member ParseFromBytes(bytes : byte[]) = 
        [|
            let recordSize = 104/2
            let chunks = 
                bytes 
                |> Array.chunkBySize recordSize
                |> Array.filter (fun x -> x.Length = recordSize)
                |> Array.filter (fun x -> IsByteArrayNotAllZero(x))
            for chunk in chunks do
                use ms = new MemoryStream(chunk)
                use r  = new BinaryReader(ms)
                yield {
                    ItemID      = r.ReadUInt32()
                    Price       = r.ReadUInt32()
                    TimeStamp   = r.ReadUInt32()
                    Count       = r.ReadUInt32()
                    IsHQ        = r.ReadByte() = 1uy
                    Unknown     = r.ReadInt16() |> uint32
                    BuyerName  = 
                                    let bytes = r.ReadBytes(34)
                                    Encoding.UTF8.GetString(bytes.[0 .. (Array.findIndex ((=) 0uy) bytes) - 1])
                }
        |]

type TradeLogPacket = 
    {
        ItemID : uint32
        Records: TradeLogRecord []
    }
    static member ParseFromBytes(data : ByteArray) = 
        use r = data.GetReader()
        let itemId = r.ReadUInt32()
        let restBytes = 
            let bs = r.BaseStream
            let len = bs.Length - bs.Position
            r.ReadBytes(len |> int)
        let records= TradeLogRecord.ParseFromBytes(restBytes)
        {
            ItemID  = itemId
            Records = records
        }