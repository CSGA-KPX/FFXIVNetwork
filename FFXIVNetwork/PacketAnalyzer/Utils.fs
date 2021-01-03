namespace FFXIVNetwork.PacketAnalyzer

open System
open System.Collections
open System.IO
open System.Text
open System.Text.RegularExpressions
open LibFFXIV.GameData.Raw


type Log(str : string) = 
    member x.Value = str
    member x.ContainsWildcards = str.Contains("?") || str.Contains("*")
    member x.ToRegex() = 
        let str = str.Replace("?", ".").Replace("*", ".*")
        printfn "regex : %s" str
        new Regex(str, RegexOptions.Compiled)

type BitMask(str : string) = 
    let ba = 
        str.ToCharArray()
        |> Array.map (fun x -> x = '1')
        |> BitArray

    member x.BitArray = ba
    member x.BitMask  = str

type internal PatternBuilder private () as x = 
    inherit BinaryWriter()

    static let utf8 = Encoding.UTF8

    let sb = StringBuilder()
    let mutable regex = false

    do
        x.OutStream <- { new Stream() with
            member x.CanRead = false
            member x.CanWrite = true
            member x.CanSeek = false
            member x.Length = raise <| NotSupportedException()
            member x.SetLength(_) = raise <| NotSupportedException()
            member x.Position with get() = raise <| NotSupportedException()
                               and set _ = raise <| NotSupportedException()
            member x.Flush() = ()
            member x.Seek(_, _) = raise <| NotSupportedException()
            member x.Read(_, _, _) = raise <| NotSupportedException()
            member x.Write(buf, offset, count) = 
                let hex = BitConverter.ToString(buf, offset, count)
                sb.Append(hex.Replace("-", "")) |> ignore
        }


    override x.Write(s : string) = 
        let bytes = utf8.GetBytes(s)
        x.Write(bytes, 0, bytes.Length)

    member x.Write(l : Log) = 
        if l.ContainsWildcards then
            regex <- true
            let str = l.Value.Replace("?", ".").Replace("*", ".*")
            sb.Append(str) |> ignore
        else
            sb.Append(l.Value) |> ignore

    member x.Write(ba : BitArray) = 
        let MaskToBytesLen c = 
            let d = c / 8
            let m = c % 8
            if m = 0 then
                d
            else
                d + 1
        let numBytes = MaskToBytesLen(ba.Length)
        let a = Array.zeroCreate<byte> numBytes
        ba.CopyTo(a, 0)
        x.Write(a)

    member x.RequireRegex = regex

    override x.ToString() = sb.ToString()

    static member Parse([<ParamArray>] objs : obj []) = 
        let pb = new PatternBuilder()
        for item in objs do 
            match item with
            | :? byte as item -> pb.Write(item)
            | :? sbyte as item -> pb.Write(item)
            | :? int16 as item -> pb.Write(item)
            | :? uint16 as item -> pb.Write(item)
            | :? int32 as item -> pb.Write(item)
            | :? uint32 as item -> pb.Write(item)
            | :? int64 as item -> pb.Write(item)
            | :? uint64 as item -> pb.Write(item)
            | :? string as item -> pb.Write(item)
            | :? float as item -> pb.Write(item)
            | :? float32 as item -> pb.Write(item)
            | :? Log as item -> pb.Write(item)
            | :? BitMask as bm -> pb.Write("")
            | _ -> raise <| NotSupportedException()
        pb

[<RequireQualifiedAccess>]
type internal SearchPattern = 
    | String of string
    | Regex of Regex

    member x.Match(line : string) = 
        match x with
        | String str ->
            line.IndexOf(str)
        | Regex regex -> 
            let m = regex.Match(line)
            if m.Success then
                m.Index
            else
                -1

    static member Parse([<ParamArray>] objs : obj []) = 
        let pb = PatternBuilder.Parse(objs)
        if pb.RequireRegex then
            Regex (new Regex(pb.ToString(), RegexOptions.Compiled))
        else
            String (pb.ToString())

type Xiv private () = 
    let col = new EmbeddedXivCollection(XivLanguage.ChineseSimplified)
    let items = col.GetSheet("Item")

    static let inst = Xiv()

    static member Instance = inst

    member x.SearchItem(name : string) = 
        items
        |> Seq.filter (fun x -> x.As<string>("Name").Contains(name))
        |> Seq.iter(fun x -> printfn "%i -> %s" x.Key.Main (x.As<string>("Name")))

    member x.GetItem(idx : int) = items.[idx]

    member x.GetItem(name : string) =
        items
        |> Seq.find (fun x -> x.As<string>("Name") = name)

    member x.GetSheet(name : string) = col.GetSheet(name)

type PacketAnalyzer(logs : LogEntry []) = 

    member x.Item(i) = logs.[i]

    member x.NotOpcode(op : string) = 
        logs
        |> Array.filter (fun log -> log.OpCode <> op)
        |> PacketAnalyzer

    member x.ByOpcode(op : string) = 
        logs
        |> Array.filter (fun log -> log.OpCode = op)
        |> PacketAnalyzer

    member x.SumOpcodes() = 
        logs
        |> Seq.map (fun x -> x.OpCode)
        |> Seq.distinct
        |> Seq.toArray

    member x.Print() = 
        logs
        |> Array.iteri (fun idx line -> Console.WriteLine(sprintf "[%i] %O" idx line))

    member x.Search([<ParamArray>] objs : obj []) = 
        let p = SearchPattern.Parse(objs)
        logs
        |> Array.Parallel.choose (fun log ->
            let idx = p.Match(log.Data)
            if idx <> -1 then
                Some(log)
            else
                None)
        |> PacketAnalyzer

    member x.GetOut() =
        logs
        |> Array.filter (fun log -> log.Direction = ">>>>>")
        |> PacketAnalyzer

    member x.GetIn() = 
        logs
        |> Array.filter (fun log -> log.Direction = "<<<<<")
        |> PacketAnalyzer

    override x.ToString() = 
        let sb = new StringBuilder()
        sb.AppendLine(sprintf "%i log(s)" logs.Length) |> ignore
        for log in logs |> Array.truncate 1 do 
            sb.AppendLine(log.ToString()) |> ignore
        sb.ToString()

    static member Load(path : string) = 
        use f = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)
        use sr =  new StreamReader(f, Encoding.Default)
        [|
            while not sr.EndOfStream do
                let line = sr.ReadLine()
                if line.Contains("PacketLogger") then
                    yield LogEntry.Parse(line)
        |]
        |> PacketAnalyzer