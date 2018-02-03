module LibXIVServer.Common
open System
open System.Net.Http
open MBrace.FsPickler
open MBrace.FsPickler.Json
open System.Net.Http.Headers


let APIHost = "http://127.0.0.1:5000"

let ToJson = FsPickler.CreateJsonSerializer(false, true)

let UTF8   = new Text.UTF8Encoding(false)

let HTTPClient =
    let client  = new HttpClient()
    client.DefaultRequestHeaders.Accept.Clear()
    //client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"))
    client