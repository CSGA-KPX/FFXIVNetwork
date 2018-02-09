module LibXIVServer.UsernameMapping
open System
open System.Net.Http
open MBrace.FsPickler
open MBrace.FsPickler.Json
open System.Net.Http.Headers
open LibFFXIV.Network
open LibXIVServer.Common

let APIUrl itemId = sprintf "%s/userid/%i" APIHost itemId

type UsernameLookupResult = 
    {
        Record      : SpecializedPacket.CharacterNameLookupReply option
        UserID      : uint64
        Success     : bool
        HTTPStatus  : string
        UpdateDate  : string
    }

let GetUsername (userId) = 
    let res = Common.HTTPClient.GetAsync(APIUrl userId).Result
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

    let record = 
        if res.IsSuccessStatusCode then
            Some(Common.ToJson.UnPickleOfString<SpecializedPacket.CharacterNameLookupReply>(resp))
        else
            NLog.LogManager.GetCurrentClassLogger().Info("获取市场订单失败 状态码：{0}", res.StatusCode)
            None

    {
        Record      = record
        UserID      = userId
        Success     = res.IsSuccessStatusCode
        HTTPStatus  = res.StatusCode.ToString()
        UpdateDate  = update
    }

let PutUsername (ra : SpecializedPacket.CharacterNameLookupReply) =
    let userId = ra.UserID
    let json   = ToJson.PickleToString(ra)
    
    let content= new StringContent(json, UTF8, "application/json")
    let task = HTTPClient.PutAsync(APIUrl userId, content)
    let resp = task.Result
    resp.EnsureSuccessStatusCode() |> ignore
    sprintf "Server resp: %s, code:%s" (resp.Content.ReadAsStringAsync().Result) (resp.StatusCode.ToString())
    |> NLog.LogManager.GetCurrentClassLogger().Info