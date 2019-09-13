module LibDmfXiv.Client
open Fable.Remoting.DotnetClient

module MarketOrder = 
    open LibXIVServer.Shared.MarketOrder

    let MarketOrderProxy = 
        Proxy.create<IMarkerOrder> Utils.route
        
module TradeLog = 
    open LibXIVServer.Shared.TradeLog

    let MarketOrderProxy = 
        Proxy.create<ITradeLog> Utils.route
        
module UsernameMapping = 
    open LibXIVServer.Shared.UsernameMapping

    let MarketOrderProxy = 
        Proxy.create<IUsernameMapping> Utils.route