module LibDmfXiv.Client
open Fable.Remoting.DotnetClient

module Utils = 
    let local = false
    let route = 
        if local then
            sprintf "http://127.0.0.1:5000/%s/%s"
        else
            sprintf "https://xivnet.danmaku.org/%s/%s"
module MarketOrder = 
    open LibDmfXiv.Shared.MarketOrder

    let MarketOrderProxy = 
        Proxy.create<IMarkerOrder> Utils.route
        
module TradeLog = 
    open LibDmfXiv.Shared.TradeLog

    let TradelogProxy = 
        Proxy.create<ITradeLog> Utils.route
        
module UsernameMapping = 
    open LibDmfXiv.Shared.UsernameMapping

    let MarketOrderProxy = 
        Proxy.create<IUsernameMapping> Utils.route