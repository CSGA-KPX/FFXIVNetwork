module LibFFXIV.ClientData.ContentFinderCondition
open System


type ContentFinderConditionRecord = 
    {
        Name        : string
        IsMentor    : bool
        ContentType : string
    }


let ContentFinderCondition = new Collections.Generic.HashSet<ContentFinderConditionRecord>()

#if COMPILED 
do
    let ra = Utils.Resource.ContentFinderCondition.ReadBinary<ContentFinderConditionRecord []>()
    for r in ra do 
        if not (String.IsNullOrWhiteSpace(r.Name)) then
            ContentFinderCondition.Add(r) |> ignore
    ()
#endif