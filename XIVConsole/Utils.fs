module Utils
open System

type Query = 
    | Item         of string
    | Materials    of string
    | MaterialsRec of string

    member x.TryGetItem() = 
        let str = 
            match x with 
            | Item         x -> x
            | Materials    x -> x
            | MaterialsRec x -> x
        let ip = LibFFXIV.Database.ItemProvider
        let name = lazy (ip.FromName(str))
        let lode = lazy (ip.FromLodeId(str))
    
        if   name.Value.IsSome then
            Some name.Value.Value
        elif lode.Value.IsSome then
            Some lode.Value.Value
        else
            None

    static member FromString(q : string) = 
        if   q.StartsWith("!!") then
            MaterialsRec q.[2..] 
        elif q.StartsWith("!" ) then
            Materials    q.[1..]
        else
            Item q

let internal TakeMarketSample (samples : LibFFXIV.SpecializedPacket.MarketRecord [] , cutPct : int) = 
    [|
        //(price, count)
        let samples = samples |> Array.sortBy (fun x -> x.Price)
        let itemCount = samples |> Array.sumBy (fun x -> x.Count |> int)
        let cutLen = itemCount * cutPct / 100
        let mutable rest = cutLen
        match itemCount = 0 , cutLen = 0 with
        | true, _ -> ()
        | false, true ->
            yield ((int) samples.[0].Price, 1)
        | false, false ->
            for record in samples do
                let takeCount = min rest (record.Count |> int)
                if takeCount <> 0 then
                    rest <- rest - takeCount
                    yield ((int) record.Price, takeCount)
    |]

type StdEv = 
    {
        Average   : float
        Deviation : float
    }

    member x.Plus(y : float) = 
        {
            Average   = x.Average * y
            Deviation = x.Deviation * y
        }

    override x.ToString() = 
        sprintf "%.0f±%.0f" x.Average x.Deviation

let GetStdEv(market : LibFFXIV.SpecializedPacket.MarketRecord [] , cutPct : int) = 
    let samples = TakeMarketSample(market, cutPct)
    let itemCount = samples |> Array.sumBy (fun (a, b) -> (float) b)
    let average = 
        let priceSum = samples |> Array.sumBy (fun (a, b) -> (float) (a * b))
        priceSum / itemCount
    let sum = 
        samples
        |> Array.sumBy (fun (a, b) -> (float b) * (( (float a) - average) ** 2.0) )
    let ev  = sum / itemCount
    { Average = average; Deviation = sqrt ev }
    //(average, sqrt ev)