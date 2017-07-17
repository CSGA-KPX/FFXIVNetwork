module Utils
open System
open System.Net
open System.Net.Sockets

type HexString = LibFFXIV.Utils.HexString

let DictionaryAddOrUpdate (dict : Collections.Generic.Dictionary<_,_> , key, value) = 
    if dict.ContainsKey(key) then
        dict.[key] <- value
    else
        dict.Add(key, value)

let LobbyServerIP = 
    Net.Dns.GetHostAddresses("ffxivlobby01.ff14.sdo.com").[0].ToString()

let LocalIPAddress = 
    let host = Dns.GetHostEntry(Dns.GetHostName())
    host.AddressList
    |> Seq.filter (fun ip -> ip.AddressFamily = AddressFamily.InterNetwork)
    |> Seq.map (fun x -> x.ToString())
    |> Seq.head