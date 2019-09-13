namespace LibXIVServer.Shared.Utils
open System
open System.Reflection

[<RequireQualifiedAccessAttribute>]
module PropCopier = 
    let private sourceFlags = BindingFlags.Public ||| BindingFlags.Instance
    let private targetFlags = BindingFlags.Public ||| BindingFlags.Instance// ||| BindingFlags.DeclaredOnly 
    let private getPropKey (p : PropertyInfo) = (sprintf "%s|%s" (p.Name) (p.PropertyType.Name))

    let Copy(source, target) = 
        let sourceProps = 
            source.GetType().GetProperties(sourceFlags)
            |> Array.map (fun x -> getPropKey(x), x)
        let targetProps = 
            target.GetType().GetProperties(targetFlags)
            |> Array.map (fun x -> getPropKey(x), x)
            |> readOnlyDict

        for (key, p) in sourceProps do 
                let toValue = p.GetValue(source)
                targetProps.[key].SetValue(target, toValue)