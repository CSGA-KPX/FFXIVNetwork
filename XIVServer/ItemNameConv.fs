module ItemNameConv
open System
open System.IO
open Newtonsoft.Json
open System.Collections.Generic
open Nancy.Responses
open System.Text


[<CLIMutableAttribute>]
type XIVDb =
    {
        Id       : int
        Name_En  : string
        Name_Ja  : string
        Name_Chs : string
    }

type ItemNameConv() as x = 
    inherit Nancy.NancyModule()
    let xivDb = 
        let dic = new Dictionary<string, XIVDb>()
        let addItem (name, id) = 
            if not <| dic.ContainsKey(name) then
                dic.Add(name, id)
        let str = File.ReadAllText("../static/dmfxivdb.json")
        let json = JsonConvert.DeserializeObject<XIVDb []>(str)
        for item in json do 
            addItem(item.Name_En.ToLower() , item)
            //addItem(item.Name_Chs, item)
            addItem(item.Name_Ja.ToLower() , item)
        dic
    do
        x.Get("/itemNameConv", fun parms -> x.View.["ItemNameConv.html"])
        x.Post("/itemNameConv", fun parms ->
            let form = x.Request.Form :?> Nancy.DynamicDictionary
            let ret = 
                let sb = new StringBuilder()
                sb  .AppendFormat("{0}\r\n", "This is a tab-separated values (TSV) file. Copy & paste to excel to view/edit.")
                    .AppendFormat("{0}\t{1}\r\n", "Date", DateTimeOffset.Now)
                    .AppendFormat("{0}\t{1}\r\n", "<Input>", "<Output>") |> ignore
                if form.ContainsKey("Text1") then
                    let lines = 
                        let str = form.["Text1"].ToString()
                        str.Split([|"\r\n"; "\r"; "\n"|], StringSplitOptions.RemoveEmptyEntries)
                        |> Array.map (fun str -> str.Trim())
                    for line in lines do 
                        let result = 
                            let key = line.ToLower()
                            if xivDb.ContainsKey(key) then
                                xivDb.[key].Name_Chs
                            else
                                line
                        sb.AppendFormat("{0}\t{1}\r\n", line, result) |> ignore
                sb.ToString()
            ret :> obj)