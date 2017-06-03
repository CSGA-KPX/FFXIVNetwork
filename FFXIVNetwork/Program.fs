open System

[<EntryPoint>]
let main argv = 
    PCap.Start()

    Console.ReadLine() |> ignore

    0 // 返回整数退出代码
    