module LibFFXIV.Utils
open System

type HexString = 
    //This is rarely used. No need for faster method
    static member ToBytes (s : string) = 
      s
      |> Seq.windowed 2
      |> Seq.mapi (fun i j -> (i,j))
      |> Seq.filter (fun (i,j) -> i % 2=0)
      |> Seq.map (fun (_,j) -> Byte.Parse(new System.String(j),System.Globalization.NumberStyles.AllowHexSpecifier))
      |> Array.ofSeq

    // from https://blogs.msdn.microsoft.com/blambert/2009/02/22/blambertcodesnip-fast-byte-array-to-hex-string-conversion/
    static member private HexTable =
        [|  "00"; "01"; "02"; "03"; "04"; "05"; "06"; "07"; "08"; "09"; "0A"; "0B"; "0C"; "0D"; "0E"; "0F";
            "10"; "11"; "12"; "13"; "14"; "15"; "16"; "17"; "18"; "19"; "1A"; "1B"; "1C"; "1D"; "1E"; "1F";
            "20"; "21"; "22"; "23"; "24"; "25"; "26"; "27"; "28"; "29"; "2A"; "2B"; "2C"; "2D"; "2E"; "2F";
            "30"; "31"; "32"; "33"; "34"; "35"; "36"; "37"; "38"; "39"; "3A"; "3B"; "3C"; "3D"; "3E"; "3F";
            "40"; "41"; "42"; "43"; "44"; "45"; "46"; "47"; "48"; "49"; "4A"; "4B"; "4C"; "4D"; "4E"; "4F";
            "50"; "51"; "52"; "53"; "54"; "55"; "56"; "57"; "58"; "59"; "5A"; "5B"; "5C"; "5D"; "5E"; "5F";
            "60"; "61"; "62"; "63"; "64"; "65"; "66"; "67"; "68"; "69"; "6A"; "6B"; "6C"; "6D"; "6E"; "6F";
            "70"; "71"; "72"; "73"; "74"; "75"; "76"; "77"; "78"; "79"; "7A"; "7B"; "7C"; "7D"; "7E"; "7F";
            "80"; "81"; "82"; "83"; "84"; "85"; "86"; "87"; "88"; "89"; "8A"; "8B"; "8C"; "8D"; "8E"; "8F";
            "90"; "91"; "92"; "93"; "94"; "95"; "96"; "97"; "98"; "99"; "9A"; "9B"; "9C"; "9D"; "9E"; "9F";
            "A0"; "A1"; "A2"; "A3"; "A4"; "A5"; "A6"; "A7"; "A8"; "A9"; "AA"; "AB"; "AC"; "AD"; "AE"; "AF";
            "B0"; "B1"; "B2"; "B3"; "B4"; "B5"; "B6"; "B7"; "B8"; "B9"; "BA"; "BB"; "BC"; "BD"; "BE"; "BF";
            "C0"; "C1"; "C2"; "C3"; "C4"; "C5"; "C6"; "C7"; "C8"; "C9"; "CA"; "CB"; "CC"; "CD"; "CE"; "CF";
            "D0"; "D1"; "D2"; "D3"; "D4"; "D5"; "D6"; "D7"; "D8"; "D9"; "DA"; "DB"; "DC"; "DD"; "DE"; "DF";
            "E0"; "E1"; "E2"; "E3"; "E4"; "E5"; "E6"; "E7"; "E8"; "E9"; "EA"; "EB"; "EC"; "ED"; "EE"; "EF";
            "F0"; "F1"; "F2"; "F3"; "F4"; "F5"; "F6"; "F7"; "F8"; "F9"; "FA"; "FB"; "FC"; "FD"; "FE"; "FF"  |]
    static member ToHex (bytes : byte[]) = 
        let hexTable = HexString.HexTable
        let sb = new Text.StringBuilder(bytes.Length * 2)
        for b in bytes do 
            sb.Append(hexTable.[(int)b]) |> ignore
        sb.ToString()


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

type XIVBinaryReader(ms : IO.MemoryStream) = 
    inherit IO.BinaryReader(ms)

    member x.IsEnd() = 
        x.BaseStream.Position = x.BaseStream.Length

    member x.ReadFixedUTF8(len : int) = 
        let bytes = x.ReadBytes(len)
        Text.Encoding.UTF8.GetString(bytes.[0 .. (Array.findIndex ((=) 0uy) bytes) - 1])

    member x.PeekHexString(len : int) = 
        let origPos = x.BaseStream.Position
        let hex     = HexString.ToHex(x.ReadBytes(len))
        x.BaseStream.Position <- origPos
        hex

    member x.ReadRestBytes() = 
        let bs = x.BaseStream
        let len = bs.Length - bs.Position
        x.ReadBytes(len |> int)

    member x.ReadTimeStampSec() = 
        TimeStamp.FromSeconds(x.ReadUInt32())

    member x.ReadTimeStampMillisec() = 
        TimeStamp.FromMilliseconds(x.ReadUInt64())

    member x.ReadRestBytesAsChunk(size : int, ?filterZeroChunks : bool) = 
        let needFilter = defaultArg filterZeroChunks true
        let (chunks, tail) = 
            x.ReadRestBytes()
            |> Array.chunkBySize size
            |> Array.partition (fun x -> x.Length = size)

        let tail =  tail |> Array.tryHead 
        let chunks = 
            if needFilter then
                chunks |> Array.filter (fun x -> IsByteArrayAllZero(x))
            else
                chunks
        (chunks, tail)

    static member FromBytes (bytes : byte []) =
        let ms = new IO.MemoryStream(bytes)
        new XIVBinaryReader(ms)