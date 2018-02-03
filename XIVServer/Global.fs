module LibXIVServer.Global
open SQLite
open MBrace.FsPickler.Json


let internal db = 
    let sql = new SQLiteConnection("ffxivserver.db")
    sql.Trace <- true
    sql.Tracer <- fun str -> System.Console.WriteLine(str)
    sql


let UTF8   = new System.Text.UTF8Encoding(false)
let Json = FsPickler.CreateJsonSerializer(false, true)