module LibXIVServer.TradeLogV2
open System
open System.Net.Http
open MBrace.FsPickler
open MBrace.FsPickler.Json
open System.Net.Http.Headers
open LibFFXIV.Network
open LibFFXIV.Client
open LibXIVServer.Common

//marketlogs/
//         /itemId     PUT  读取7天内数据，或提交交易记录
//         /itemId/d   GET      获取d天内交易记录，默认7，最大28

let APIUrl itemId = sprintf "%s/tradelogs/%i" APIHost itemId
let APIUrlRange itemId days = sprintf "%s/tradelogs/%i/%i" APIHost itemId days


type TradeLogResult = 
    {
        TradeLogs : SpecializedPacket.TradeLogRecord [] option
        Days      : int
        ItemId      : int
        Success     : bool
        HTTPStatus  : string
        UpdateDate  : string
    }


let GetTradeLogRange(itemId, days) =
    let res = Common.HTTPClient.GetAsync(APIUrlRange itemId days).Result
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
            Some(Common.ToJson.UnPickleOfString<SpecializedPacket.TradeLogRecord []>(resp))
        else
            NLog.LogManager.GetCurrentClassLogger().Info("获取交易记录失败 状态码：{0}", res.StatusCode)
            None

    {
        TradeLogs = records 
        Days      = days
        ItemId      = itemId
        Success     = res.IsSuccessStatusCode
        HTTPStatus  = res.StatusCode.ToString()
        UpdateDate  = update
    }

let GetTradeLog(itemId) =
    GetTradeLogRange(itemId, 7)


let PutTradeLog(logs : SpecializedPacket.TradeLogRecord []) =
    let itemId = logs.[0].ItemID
    let containOthers = logs |> Array.exists (fun r -> r.ItemID <> itemId)
    if containOthers then
        NLog.LogManager.GetCurrentClassLogger().Info("提交交易记录失败，ItemID不唯一: {0}", logs)
    else
        let json = ToJson.PickleToString(logs)
        let content= new StringContent(json, UTF8, "application/json")
        let task = HTTPClient.PutAsync(APIUrl itemId, content)
        let resp = task.Result
        //resp.EnsureSuccessStatusCode() |> ignore
        sprintf "Server resp: %s, code:%s" (resp.Content.ReadAsStringAsync().Result) (resp.StatusCode.ToString())
        |> NLog.LogManager.GetCurrentClassLogger().Info