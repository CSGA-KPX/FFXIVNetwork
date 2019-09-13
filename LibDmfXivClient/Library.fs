module LibDmfXiv.Client
open Fable.Remoting.DotnetClient

module MarketOrder = 
    open LibDmfXiv.Shared.MarketOrder

    let MarketOrderProxy = 
        Proxy.create<IMarkerOrder> Utils.route
        
module TradeLog = 
    open LibDmfXiv.Shared.TradeLog

    let MarketOrderProxy = 
        Proxy.create<ITradeLog> Utils.route
        
module UsernameMapping = 
    open LibDmfXiv.Shared.UsernameMapping

    let MarketOrderProxy = 
        Proxy.create<IUsernameMapping> Utils.route