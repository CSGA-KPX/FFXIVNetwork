module Utils
open System
open LibFFXIV.Client
open LibFFXIV.Network

type DisplayOP = 
    | Query  of string * Database.ItemRecord * float
    | Result of string * LibXIVDMF.Market.MarketFetchResult * float
    | BeginSum
    | EndSum of string
    | EmptyLine
    | Error of string

    member x.Fetch() = 
        match x with
        | Query (str, item, count) -> 
            let market = LibXIVDMF.Market.FetchMarketData(item)
            Result (str, market, count)
        | _ -> x

type StringQuery = 
    | Item              of string
    | Materials         of string
    | MaterialsRec      of string
    | MaterialsRecGroup of string


    static member FromString(q : string) = 
        if   q.StartsWith("!!") then
            MaterialsRec q.[2..] 
        elif q.StartsWith("!" ) then
            Materials    q.[1..]
        elif q.StartsWith("#") then
            MaterialsRecGroup  q.[1..]
        else
            Item q

    member x.Name = 
        match x with 
        | Item              x -> x
        | Materials         x -> x
        | MaterialsRec      x -> x
        | MaterialsRecGroup x -> x

    member x.GetOP() = 
        let item = Database.SaintCoinachItemProvider.GetInstance().FromName(x.Name)
        let rm = Database.RecipeManager.GetInstance()
        if item.IsNone then
            [|
                Error (sprintf "找不到物品%A" x)
            |]
        else
            let item = item.Value
            [|
                match x with
                | Item         x ->
                    yield Query (x.ToString() ,item, 1.0)
                | Materials    x ->
                    let ma = rm.GetMaterials(item)
                    let qn = x.ToString()
                    yield BeginSum
                    for (a, b) in ma do
                        yield Query (qn, a, b)
                    yield EndSum (x.ToString())
                | MaterialsRec x ->
                    let ma = rm.GetMaterialsRec(item)
                    let ma = ma |> Array.sortBy (fun (item, count) -> item.Id)

                    let qn = x.ToString()
                    yield BeginSum
                    for (a, b) in ma do
                        yield Query (qn, a, b)
                    yield EndSum (x.ToString())
                | MaterialsRecGroup x ->
                    let maa = rm.GetMaterialsRecGroup(item)
                    for (level, ma) in maa do 
                        if ma.Length = 1 then
                            let (i, c) = ma.[0]
                            yield Query (level, i, c)
                        else
                            yield BeginSum
                            for (i, c) in ma do 
                                yield Query (level, i, c)
                            yield EndSum (level)
                            yield EmptyLine
                yield EmptyLine
            |]

let internal TakeMarketSample (samples : SpecializedPacket.MarketRecord [] , cutPct : int) = 
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

let GetStdEv(market : SpecializedPacket.MarketRecord [] , cutPct : int) = 
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