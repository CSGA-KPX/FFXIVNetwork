module LibXIVServer.MarketV2
open System
open System.Net.Http
open MBrace.FsPickler
open MBrace.FsPickler.Json
open System.Net.Http.Headers
open LibFFXIV.Network
open LibXIVServer.Common
//orders/
//      / raw/itemid    GET/PUT  读写MarketRecord []


let APIUrl itemId = sprintf "%s/orders/raw/%i" APIHost itemId


type MarketFetchRawResult = 
    {
        Records     : SpecializedPacket.MarketRecord [] option
        ItemId      : int
        Success     : bool
        HTTPStatus  : string
        UpdateDate  : string
    }

let GetRawOrders (itemId) = 
    let res = Common.HTTPClient.GetAsync(APIUrl itemId).Result
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
            Some(Common.ToJson.UnPickleOfString<SpecializedPacket.MarketRecord []>(resp))
        else
            NLog.LogManager.GetCurrentClassLogger().Info("获取市场订单失败 状态码：{0}", res.StatusCode)
            None

    {
        Records     = records 
        ItemId      = itemId
        Success     = res.IsSuccessStatusCode
        HTTPStatus  = res.StatusCode.ToString()
        UpdateDate  = update
    }

let PutRawOrders (ra : SpecializedPacket.MarketRecord []) =
    let itemId = ra.[0].Itemid |> int
    let json   = ToJson.PickleToString(ra)
    
    let content= new StringContent(json, UTF8, "application/json")
    let task = HTTPClient.PutAsync(APIUrl itemId, content)
    let resp = task.Result
    resp.EnsureSuccessStatusCode() |> ignore
    sprintf "Server resp: %s, code:%s" (resp.Content.ReadAsStringAsync().Result) (resp.StatusCode.ToString())
    |> NLog.LogManager.GetCurrentClassLogger().Info