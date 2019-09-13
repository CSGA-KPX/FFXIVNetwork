open System
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Fable.Remoting.Server
open Fable.Remoting.AspNetCore
open Mono.Unix
open Mono.Unix.Native
open Microsoft.AspNetCore.Http

let errorHandler (exn: Exception) (routeInfo: RouteInfo<HttpContext>) = 
    printfn "Error at %s on method %s" routeInfo.path routeInfo.methodName
    printfn "exn : %A" exn
    Ignore


let marketApi = 
    Remoting.createApi()
    |> Remoting.fromValue MarketOrder.marketOrderApi
    |> Remoting.withDiagnosticsLogger (printfn "%s")
    |> Remoting.withErrorHandler errorHandler

let tradelogApi = 
    Remoting.createApi()
    |> Remoting.fromValue TradeLog.TradeLogApi
    |> Remoting.withDiagnosticsLogger (printfn "%s")
    |> Remoting.withErrorHandler errorHandler
    
let usernameApi = 
    Remoting.createApi()
    |> Remoting.fromValue UsernameMapping.usernameMappingApi
    |> Remoting.withDiagnosticsLogger (printfn "%s")
    |> Remoting.withErrorHandler errorHandler

let configureApp (app : IApplicationBuilder) =
    
    app.UseRemoting marketApi
    app.UseRemoting tradelogApi
    app.UseRemoting usernameApi

[<EntryPoint>]
let main argv = 
    WebHostBuilder()
        .UseKestrel()
        .UseUrls("http://127.0.0.1:5000")
        .Configure(Action<IApplicationBuilder> configureApp)
        .Build()
        .Run()

    if Type.GetType("Mono.Runtime") <> null then
        UnixSignal.WaitAny(
            [|
                new UnixSignal(Signum.SIGINT)
                new UnixSignal(Signum.SIGTERM)
                new UnixSignal(Signum.SIGQUIT)
                new UnixSignal(Signum.SIGHUP)
            |]) |> ignore
    else
        Console.ReadLine() |> ignore

    Console.WriteLine("Stopping Fable.Remoting");
    0 // return an integer exit code
