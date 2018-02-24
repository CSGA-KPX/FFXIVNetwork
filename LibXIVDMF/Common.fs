module LibXIVServer.Common
open System
open System.Threading
open System.Net.Http
open MBrace.FsPickler
open MBrace.FsPickler.Json
open System.Net.Http.Headers
open System.Collections.Concurrent


let APIHost = 
    //"http://127.0.0.1:5000"
    "https://xivnet.danmaku.org"

let ToJson = FsPickler.CreateJsonSerializer(false, true)

let UTF8   = new Text.UTF8Encoding(false)

let HTTPClient =
    let client  = new HttpClient()
    client.DefaultRequestHeaders.Accept.Clear()
    //client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"))
    client

exception RetryLimitExceeded

type internal RetryBuilder(max, sleep : TimeSpan) = 
      member x.Return(a) = a
      member x.Delay(f) = f
      member x.Zero() = x.Return ()
      member x.Run(f) =
        let rec loop(n) = 
            if n = 0 then raise RetryLimitExceeded
            else 
                try f() 
                with ex -> 
                    sprintf "Call failed with %s. Retrying." ex.Message |> printfn "%s"
                    Thread.Sleep(sleep); 
                    loop(n-1)
        loop max

type Result<'T> = 
    {
        Record      : 'T option
        Success     : bool
        HTTPStatus  : string
        UpdateDate  : string
    }


type DAOUtils private () = 
    let client = new HttpClient()
    let utf8   = new Text.UTF8Encoding(false)
    let json   = FsPickler.CreateJsonSerializer(false, true)
    let host   = "https://xivnet.danmaku.org"
    //let host    = "http://127.0.0.1:5000"
    
    static let instance = new DAOUtils()

    member x.HTTP       = client
    member x.UTF8       = utf8
    member x.JSON       = json
    member x.Host       = host

    static member Instance = instance

[<AbstractClassAttribute>]
type DAOBase<'T>() as x = 
    let utils = DAOUtils.Instance

    let logger = NLog.LogManager.GetLogger(x.GetType().Name)

    abstract GetUrl : [<ParamArray>] args:Object [] -> string
    abstract PutUrl : [<ParamArray>] args:Object [] -> string



    member internal x.DoGet(url : string) = 
        async {
            let! res = utils.HTTP.GetAsync(utils.Host + url)  |> Async.AwaitTask
            let! resp= res.Content.ReadAsStringAsync()        |> Async.AwaitTask
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
                    Some(utils.JSON.UnPickleOfString<'T>(resp))
                else
                    logger.Info("{0}.Get() 失败 状态码：{1}",x.GetType().Name, res.StatusCode)
                    None
            return {
                Record      = record
                Success     = res.IsSuccessStatusCode
                HTTPStatus  = res.StatusCode.ToString()
                UpdateDate  = update
            }
        }
        |> Async.RunSynchronously

    member internal x.DoPut(url : string, obj) = 
        let task = 
            async {
                let content= new StringContent(utils.JSON.PickleToString(obj), utils.UTF8, "application/json")
                let! response =  utils.HTTP.PutAsync(utils.Host + url, content) |> Async.AwaitTask
                let! str = response.Content.ReadAsStringAsync() |> Async.AwaitTask
                let code = response.StatusCode.ToString()
                sprintf "Server resp: %s, code:%s" str code
                |> logger.Info
            }
        task |> Async.Start
        