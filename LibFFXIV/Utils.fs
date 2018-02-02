module LibFFXIV.Network.Utils
open System
open System.IO
open System.Text

type bytes = byte []

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

let internal IsByteArrayNotAllZero (bytes : byte[]) =
    bytes
    |> Array.exists (fun x -> x <> 0uy)

/// <summary>
/// Unix时间戳
/// </summary>
type TimeStamp(utc : DateTime) = 
    let utc = utc
    member x.GetUTCTime()  = utc
    member x.GetLocalTime() = utc.ToLocalTime()


    override x.ToString() = x.GetLocalTime().ToString()

    static member UTC = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)

    static member FromSeconds(sec : uint32) = 
        new TimeStamp(TimeStamp.UTC.AddSeconds(sec |> float))

    static member FromMilliseconds(ms : uint64) = 
        new TimeStamp(TimeStamp.UTC.AddMilliseconds(ms |> float))

type XIVBinaryReader(ms : IO.MemoryStream) = 
    inherit IO.BinaryReader(ms)

    /// <summary>
    /// 是否读取到流末尾
    /// </summary>
    member x.IsEnd() = 
        x.BaseStream.Position = x.BaseStream.Length

    /// <summary>
    /// 读取定长UTF8字符串，以0x00填充至指定长度
    /// </summary>
    /// <param name="len">长度</param>
    member x.ReadFixedUTF8(len : int) = 
        let bytes = x.ReadBytes(len)
        Text.Encoding.UTF8.GetString(bytes.[0 .. (Array.findIndex ((=) 0uy) bytes) - 1])

    /// <summary>
    /// 读取指定长度字节数组，而不提升位置
    /// </summary>
    /// <param name="len">长度</param>
    member x.PeekBytes(len : int) = 
        let origPos = x.BaseStream.Position
        let bytes   = x.ReadBytes(len)
        x.BaseStream.Position <- origPos
        bytes

    /// <summary>
    /// 读取指定长度字节为16进制字符串
    /// </summary>
    /// <param name="len">长度</param>
    member x.ReadHexString(len : int) = 
        HexString.ToHex(x.ReadBytes(len))

    /// <summary>
    /// 读取剩余部分为数组
    /// </summary>
    member x.ReadRestBytes() = 
        let bs = x.BaseStream
        let len = bs.Length - bs.Position
        x.ReadBytes(len |> int)

    /// <summary>
    /// 读取秒(uint32)单位的时间戳
    /// </summary>
    member x.ReadTimeStampSec() = 
        TimeStamp.FromSeconds(x.ReadUInt32())

    /// <summary>
    /// 读取毫秒(uint64)单位的时间戳
    /// </summary>
    member x.ReadTimeStampMillisec() = 
        TimeStamp.FromMilliseconds(x.ReadUInt64())

    /// <summary>
    /// 将剩余字节分块返回
    /// </summary>
    /// <param name="size">分块大小</param>
    /// <param name="filterZeroChunks">是否滤除全零分块</param>
    member x.ReadRestBytesAsChunk(size : int, ?filterZeroChunks : bool) = 
        let needFilter = defaultArg filterZeroChunks false
        let (chunks, tail) = 
            x.ReadRestBytes()
            |> Array.chunkBySize size
            |> Array.partition (fun x -> x.Length = size)

        let tail =  tail |> Array.tryHead 
        let chunks = 
            if needFilter then
                chunks |> Array.filter (fun x -> IsByteArrayNotAllZero(x))
            else
                chunks
        (chunks, tail)

    static member FromBytes (bytes : byte []) =
        let ms = new IO.MemoryStream(bytes)
        new XIVBinaryReader(ms)

/// 提供碎片拼接相关数据
type IQueueableItem<'TSeq, 'TData> = 
    abstract QueueCurrentIdx : 'TSeq
    abstract QueueNextIdx    : 'TSeq

    abstract IsCompleted : unit  -> bool
    abstract IsExpried   : 'TSeq -> bool
    
    /// 将两个'TData合并，并修改索引值
    abstract Combine     : 'TData -> 'TData


[<AbstractClassAttribute>]
type GeneralPacketReassemblyQueue<'TSeq, 'TItem, 'TOutData 
                                    when 'TSeq : equality
                                    and 'TItem :>IQueueableItem<'TSeq, 'TItem>>() as x = 
    let rwlock = new Threading.ReaderWriterLockSlim()
    let evt = new Event<'TOutData>()
    let d   = System.Collections.Generic.Dictionary<'TSeq, 'TItem>()
    let nlog= NLog.LogManager.GetLogger(x.GetType().FullName)


    member internal x.logger = nlog

    member internal x.dict = d

    member internal x.OnCompleted(d : 'TOutData) =
        evt.Trigger(d)

    member x.NewCompleteDataEvent = evt.Publish

    member x.GetQueuedKeys() = x.dict.Keys

    abstract processPacketCompleteness : 'TItem -> unit

    abstract preProcessPacketChain     : 'TItem -> unit

    default x.preProcessPacketChain(data : 'TItem) = ()


    member internal x.CombineItems([<ParamArray>] objs : 'TItem []) =
        objs
        |> Array.reduce (fun acc item -> acc.Combine(item))

    member private x.processPacketChain(p : 'TItem) =
        let (fwdSucc, fwdItem) = x.dict.TryGetValue(p.QueueCurrentIdx)
        let RevSearch = 
            x.dict
            |> Seq.filter (fun x -> 
                x.Value.QueueCurrentIdx = p.QueueNextIdx)
            |> Seq.tryHead

        match fwdSucc, RevSearch.IsSome with
        | true, true   ->
            let p3 = RevSearch.Value.Value
            let np = x.CombineItems(fwdItem, p, p3)
            x.dict.Remove(p.QueueCurrentIdx) |> ignore
            x.dict.Remove(RevSearch.Value.Key) |> ignore
            x.processPacketChain(np)
        | true, false  -> 
            let np = x.CombineItems(fwdItem, p)
            x.dict.Remove(p.QueueCurrentIdx) |> ignore
            x.processPacketChain(np)
        | false, true  -> 
            let np =  x.CombineItems(p, RevSearch.Value.Value)
            x.dict.Remove(RevSearch.Value.Key) |> ignore
            x.processPacketChain(np)
        | false, false -> 
            x.processPacketCompleteness(p)

    member x.Enqueue(p : 'TItem) =
        rwlock.EnterWriteLock()
        try
            x.preProcessPacketChain(p)
            let isCompleted = (p :> IQueueableItem<'TSeq, 'TItem>).IsCompleted()
            if isCompleted then
                x.processPacketCompleteness(p)
            else
                x.processPacketChain(p)
            if x.dict.Count >= GeneralPacketReassemblyQueue<_,_,_>.zombieCheckLimit then
                let toRemove = 
                    x.dict.Values
                    |> Seq.map (fun x -> x)
                    |> Seq.filter (fun x -> x.IsExpried(p.QueueCurrentIdx))
                    |> Seq.map (fun x -> x.QueueNextIdx)
                    |> Seq.toList
                toRemove
                |> List.iter (fun key -> 
                        x.logger.Info("Removed zombie packets key={0}", key)
                        x.dict.Remove(key)|> ignore)
        with
        | e -> 
            x.logger.Error("捕获到异常{0}，清空所有数据", e.ToString())
            x.dict.Clear()
        rwlock.ExitWriteLock()


    ///dict数量超过多少以后开始清理僵尸
    static member private zombieCheckLimit = 10