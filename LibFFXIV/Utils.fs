module internal Utils
open System

type HexString = 
    static member toBytes (s : string) = 
      s
      |> Seq.windowed 2
      |> Seq.mapi (fun i j -> (i,j))
      |> Seq.filter (fun (i,j) -> i % 2=0)
      |> Seq.map (fun (_,j) -> Byte.Parse(new System.String(j),System.Globalization.NumberStyles.AllowHexSpecifier))
      |> Array.ofSeq
    static member toHex   (bytes : byte[]) = 
        bytes 
        |> Array.map (fun (x : byte) -> System.String.Format("{0:X2}", x))
        |> String.concat System.String.Empty

let IsBinaryReaderEnd (reader : IO.BinaryReader) = 
    reader.BaseStream.Position = reader.BaseStream.Length

let IsByteArrayAllZero (bytes : byte[]) =
    bytes
    |> Array.exists (fun x -> x <> 0uy)