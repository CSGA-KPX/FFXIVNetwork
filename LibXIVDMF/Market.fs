module LibXIVDMF.Market
open System
open System.Net.Http
open MBrace.FsPickler
open MBrace.FsPickler.Json
open System.Net.Http.Headers

let   toJson = FsPickler.CreateJsonSerializer(false, true)
//let fromJson = FsPickler.
let utf8   = new Text.UTF8Encoding(false)
let dataUrl itemId = sprintf "https://xiv.danmaku.org/order/%i" itemId
let getClient() =
    let client  = new HttpClient()
    client.DefaultRequestHeaders.Accept.Clear()
    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"))
    client

type AveragePriceCalculationMethod = 
    | Top25Pct = 0
    | Top50Pct = 1
    | Top75Pct = 2
    | All      = 3



let SubmitMarketData(ra : LibFFXIV.SpecializedPacket.MarketRecord []) = 
    let itemId = ra.[0].Itemid |> int
    let client = getClient()
    let json   = toJson.PickleToString(ra)
    let content= new StringContent(json, utf8, "application/json")
    let task = client.PostAsync(dataUrl itemId, content)
    let resp = task.Result
    resp.EnsureSuccessStatusCode() |> ignore
    sprintf "Server resp: %s, code:%s" (resp.Content.ReadAsStringAsync().Result) (resp.StatusCode.ToString())
    |> NLog.LogManager.GetCurrentClassLogger().Info

let FetchMarketData(itemId : int) =
    try
        let client = getClient()
        let resp   = client.GetStringAsync(dataUrl itemId).Result
        Some(toJson.UnPickleOfString<LibFFXIV.SpecializedPacket.MarketRecord []>(resp))
    with 
    | e ->
        //printfn "%s" (e.ToString())
        None

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