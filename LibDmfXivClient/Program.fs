open System


[<EntryPoint>]
let main argv = 
    (*printfn "Putting"
    let mapping = 
        {
            LibXIVServer.Shared.UsernameMapping.FabelUsernameMapping.UserId = "189237491082734091234"
            LibXIVServer.Shared.UsernameMapping.FabelUsernameMapping.UserName = "asdjfhasjkdhfakljsdhfkl"
        }
    async {
        do! LibDmfXiv.Client.UsernameMapping.MarketOrderProxy.call <@ fun server -> server.PutMapping(mapping) @>
    }
    |> Async.RunSynchronously*)

    printfn "Getting"
    let ret = 
        async {
            return! LibDmfXiv.Client.UsernameMapping.MarketOrderProxy.call <@ fun server -> server.GetById("189237491082734091234") @>
        }
        |> Async.RunSynchronously
    printfn "%A" ret
    0 // return an integer exit code
