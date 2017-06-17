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

    member x.GetMaterials() = 
        let item = x.TryGetItem()
        if item.IsNone then
            failwithf "找不到物品%A" x
        else
            let item = item.Value
            match x with 
            | Item         x ->
                [| LibXIVDMF.Market.FetchMarketData(item) , 1.0 |]
            | Materials    x ->
                let recipe = LibFFXIV.Database.SuRecipeData.Instance.GetMaterials(item.LodestoneId)
                if recipe.IsSome then
                    let test = recipe.Value |> Array.map (fun (item, count) -> LibXIVDMF.Market.FetchMarketData(item), count)
                    [| yield! recipe.Value |> Array.map (fun (item, count) -> LibXIVDMF.Market.FetchMarketData(item), count) |]
                else
                    failwithf "找不到配方:%A" x
            | MaterialsRec x ->
                let recipe = LibFFXIV.Database.SuRecipeData.Instance.GetMaterialsRec(item.LodestoneId)
                if recipe.IsSome then
                    [| yield! recipe.Value |> Array.map (fun (item, count) -> LibXIVDMF.Market.FetchMarketData(item), count) |]
                else
                    failwithf "找不到配方:%A" x

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
    override x.ToString() = 
        String.Format("{0:n0}±{1:n0}", x.Average, x.Deviation)

    static member (*) (x : StdEv, y : float) = 
        {
            Average   = x.Average * y
            Deviation = x.Deviation * y
        }

    static member (+) (x : StdEv, y : StdEv) = 
        {
            Average   = x.Average + y.Average
            Deviation = x.Deviation + y.Deviation
        }

    static member Zero = 
        {
            Average   = 0.0
            Deviation = 0.0
        }

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