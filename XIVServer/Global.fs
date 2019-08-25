module XIVServer.Global
open System
open SQLite
open MBrace.FsPickler.Json


let internal db = 
    let dbpath = 
        if Type.GetType("Mono.Runtime") <> null then
            "../static/ffxivserver.db"
        else
            "ffxivserver.db"
    let sql = new SQLiteConnection(dbpath)
    sql.Trace <- true
    sql.Tracer <- fun str -> System.Console.WriteLine(str)
    sql


let UTF8   = new System.Text.UTF8Encoding(false)
let Json = FsPickler.CreateJsonSerializer(false, true)