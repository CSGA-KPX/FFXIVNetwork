open System
open System.Threading
open Nancy.Hosting.Self
open Mono.Unix
open Mono.Unix.Native
open Newtonsoft.Json
open Newtonsoft.Json.Serialization
open Nancy
open Nancy.Configuration
open Nancy.Json

type CustomJsonSerializer() = 
    inherit JsonSerializer()

    member x.CustomJsonSerializer() = 
        x.ContractResolver <- new CamelCasePropertyNamesContractResolver()

type Bootstrapper()  =
    inherit DefaultNancyBootstrapper()
    do
        ()

    override x.Configure(e) = 
        e.Json(retainCasing = new System.Nullable<bool>(true))
        base.Configure(e)

[<EntryPoint>]
let main argv = 
    let uri  = "http://127.0.0.1:5000"
    let host = new NancyHost(new Uri(uri))
    Console.WriteLine("Starting Nancy on " + uri);
    host.Start()

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

    host.Stop()
    Console.WriteLine("Stopping Nancy");
    0