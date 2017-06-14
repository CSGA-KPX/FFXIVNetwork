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
    let client = getClient()
    let resp   = client.GetStringAsync(dataUrl itemId).Result
    toJson.UnPickleOfString<LibFFXIV.SpecializedPacket.MarketRecord []>(resp)