namespace LibFFXIV.Network.SpecializedPacket
open Microsoft.FSharp.Core.Operators.Checked
open System
open LibFFXIV.Network.Utils

type MarketListRecord = 
    {
        ItemId : uint32
        Count  : uint16
        Demand : uint16
    }

type MarketListPacket = 
    {
        Records : MarketListRecord[]
        NextIdx : uint16
        CurrIdx : uint16
    }

    interface IQueueableItem<uint16, MarketListPacket> with
        member x.QueueCurrentIdx = x.CurrIdx
        member x.QueueNextIdx    = x.NextIdx

        member x.IsCompleted () = 
            (x.NextIdx |> int) % 10  = 0

        member x.IsExpried   (ref) = 
            false
    
        member x.Combine     (y)   = 
            {
                Records = Array.append x.Records y.Records
                NextIdx = y.NextIdx
                CurrIdx = x.CurrIdx
            }

    static member FromBytes (bs : bytes) = 
        let ars = bs |> Array.chunkBySize 8
        let data= 
            let d = 
                ars.[0 .. 19]
                |> Array.filter (fun x -> IsByteArrayNotAllZero(x))
            [|
            for chk in d do
                yield {
                    ItemId = BitConverter.ToUInt32(chk, 0)
                    Count  = BitConverter.ToUInt16(chk, 4)
                    Demand = BitConverter.ToUInt16(chk, 6)
                }
            |]
            
        let ta, tb = ars.[20], ars.[21]
        let next = BitConverter.ToUInt16(ta, 0)
        let curr = BitConverter.ToUInt16(tb, 0)
        {
            Records = data
            NextIdx = next
            CurrIdx = curr
        }