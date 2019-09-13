module Utils
let local = true
let route = 
    if local then
        sprintf "http://127.0.0.1:5000/%s/%s"
    else
        sprintf "https://xivnet.danmaku.org/%s/%s"