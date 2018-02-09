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

type internal RetryBuilder(max) = 
    member x.Return(a) = a               // Enable 'return'
    member x.Delay(f) = f                // Gets wrapped body and returns it (as it is)
                                        // so that the body is passed to 'Run'
    member x.Zero() = x.Return(())       // Support if .. then 
    member x.Run(f) =                    // Gets function created by 'Delay'
        let rec loop(n) = 
            if n = 0 then
                raise RetryLimitExceeded  // Number of retries exceeded
            else 
                let t = f()
                if t then
                    t
                else
                    Thread.Sleep(3000)
                    loop(n-1)
        loop max

type Result<'T> = 
    {
        Record      : 'T option
        Success     : bool
        HTTPStatus  : string
        UpdateDate  : string
    }

type DAOQueue private () = 
    let queue  = new BlockingCollection<unit -> bool>()
    let retry  = RetryBuilder(3)
    let logger = NLog.LogManager.GetCurrentClassLogger()
    do
        let rec ts() = 
            let succ, t = queue.TryTake()
            if succ then
                try
                    retry { return t() } |> ignore
                with
                | RetryLimitExceeded -> logger.Error("任务无法完成")
            else
                ts()
        let t = new Thread(ts)
        t.Start()

    static let instance = new DAOQueue()

    member internal x.AddTask(task) = 
        queue.TryAdd(task) |> ignore

    
    static member Instance = instance

[<AbstractClassAttribute>]
type internal DAOBase<'T>() = 
    static let queue  = new BlockingCollection<unit -> bool>()
    static let retry  = RetryBuilder(3)
    static let client = new HttpClient()
    static let utf8   = new Text.UTF8Encoding(false)
    static let json   = FsPickler.CreateJsonSerializer(false, true)

    abstract GetUrl : url:string * [<ParamArray>] args:Object [] -> string
    abstract PutUrl : url:string * [<ParamArray>] args:Object [] -> string

    member internal x.Get(url : string) = 
        let res = client.GetAsync(url).Result
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
                Some(json.UnPickleOfString<'T>(resp))
            else
                NLog.LogManager.GetCurrentClassLogger().Info("{0}.Get() 失败 状态码：{1}",x.GetType().Name, res.StatusCode)
                None
        {
            Record      = record
            Success     = res.IsSuccessStatusCode
            HTTPStatus  = res.StatusCode.ToString()
            UpdateDate  = update
        }

    member internal x.Put(url : string, json : string) = 
        let task = fun _ -> 
            let content= new StringContent(json, utf8, "application/json")
            let task = client.PutAsync(url, content)
            let resp = task.Result
        
            sprintf "Server resp: %s, code:%s" (resp.Content.ReadAsStringAsync().Result) (resp.StatusCode.ToString())
            |> NLog.LogManager.GetCurrentClassLogger().Info
            resp.IsSuccessStatusCode
        DAOQueue.Instance.AddTask(task)
        