module Test
open System
open System.Net.Http
open Fable.Remoting.DotnetClient
open LibDmfXiv.Shared.MarketOrder
open LibDmfXiv.Shared.TradeLog
open LibDmfXiv.Shared.UsernameMapping

type ClientInstance (?httpClient : HttpClient) = 
    let client = defaultArg httpClient (new HttpClient())
    let local = false
    let route = 
        if local then
            sprintf "http://127.0.0.1:5000/%s/%s"
        else
            sprintf "https://xivnet.danmaku.org/%s/%s"

    member val MarketOrderProxy     = Proxy.custom<IMarkerOrder> route client with get
    member val TradelogProxy        = Proxy.custom<ITradeLog> route with get
    member val UsernameMappingProxy = Proxy.custom<IUsernameMapping> route with get