module LibFFXIV.Utils
open System

type internal HexString = 
    static member ToBytes (s : string) = 
      s
      |> Seq.windowed 2
      |> Seq.mapi (fun i j -> (i,j))
      |> Seq.filter (fun (i,j) -> i % 2=0)
      |> Seq.map (fun (_,j) -> Byte.Parse(new System.String(j),System.Globalization.NumberStyles.AllowHexSpecifier))
      |> Array.ofSeq
    static member ToHex   (bytes : byte[]) = 
        bytes 
        |> Array.map (fun (x : byte) -> System.String.Format("{0:X2}", x))
        |> String.concat System.String.Empty

let internal IsBinaryReaderEnd (reader : IO.BinaryReader) = 
    reader.BaseStream.Position = reader.BaseStream.Length

let internal IsByteArrayAllZero (bytes : byte[]) =
    bytes
    |> Array.exists (fun x -> x <> 0uy)

type TimeStamp(utc : DateTime) = 
    let utc = utc
    member x.GetUTCTime()  = utc
    member x.GetLocalTime() = utc.ToLocalTime()


    override x.ToString() = x.GetLocalTime().ToString()

    static member private UTC = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)

    static member FromSeconds(i : uint32) = 
        new TimeStamp(TimeStamp.UTC.AddSeconds(i |> float))

    static member FromMilliseconds(i : uint64) = 
        new TimeStamp(TimeStamp.UTC.AddMilliseconds(i |> float))