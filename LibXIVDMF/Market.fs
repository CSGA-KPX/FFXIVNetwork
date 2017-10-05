module LibXIVDMF.Market
open System
open System.Net.Http
open MBrace.FsPickler
open MBrace.FsPickler.Json
open System.Net.Http.Headers

let   toJson = FsPickler.CreateJsonSerializer(false, true)
let utf8   = new Text.UTF8Encoding(false)
let dataUrl itemId = sprintf "https://xiv.danmaku.org/order/%i" itemId

let client =
    let client  = new HttpClient()
    client.DefaultRequestHeaders.Accept.Clear()
    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"))
    client

let SubmitMarketData(ra : LibFFXIV.SpecializedPacket.MarketRecord []) = 
    let itemId = ra.[0].Itemid |> int
    let json   = toJson.PickleToString(ra)
    let content= new StringContent(json, utf8, "application/json")
    let task = client.PostAsync(dataUrl itemId, content)
    let resp = task.Result
    resp.EnsureSuccessStatusCode() |> ignore
    sprintf "Server resp: %s, code:%s" (resp.Content.ReadAsStringAsync().Result) (resp.StatusCode.ToString())
    |> NLog.LogManager.GetCurrentClassLogger().Info

type MarketFetchResult = 
    {
        Records : LibFFXIV.SpecializedPacket.MarketRecord [] option
        Item    : LibFFXIV.Database.ItemRecord
        Success : bool
        Status  : string
        Updated : string
    }

let FetchMarketData(item : LibFFXIV.Database.ItemRecord) =
    let res    = client.GetAsync(dataUrl (item.Id)).Result
    let resp   = res.Content.ReadAsStringAsync().Result
    let update = 
        let v = res.Content.Headers.LastModified
        if v.HasValue then
            let t = v.Value.ToLocalTime()
            let now = DateTimeOffset.Now
            let diff = now - t
            sprintf "%3i天%2i时%2i分前" (diff.Days) (diff.Hours) (diff.Minutes)
        else
            "N/A"

    let records = 
        if res.IsSuccessStatusCode then
            Some(toJson.UnPickleOfString<LibFFXIV.SpecializedPacket.MarketRecord []>(resp))
        else
            NLog.LogManager.GetCurrentClassLogger().Info("获取价格失败 状态码：{0}", res.StatusCode)
            None
    {
        Records = records
        Item    = item
        Success = res.IsSuccessStatusCode
        Status  = res.StatusCode.ToString()
        Updated = update
    }