// AssemblyAttributes
// See FSHARP1.0:832,1674,1675 and 2290
// Attribute under test:  AssemblyCopyright
//<Expects status="success"></Expects>

#light

open System
open System.Reflection;
open System.Configuration.Assemblies

let CheckAssemblyAttribute () = 
        let path_to_myself = Assembly.GetExecutingAssembly().Location
        let actualvalue      = System.Diagnostics.FileVersionInfo.GetVersionInfo(path_to_myself).LegalCopyright
        let expectedvalue    = "Copyright ? Microsoft 3456"
        if actualvalue = expectedvalue then 
                                            0
                                       else
                                            printfn "FAIL: Expected %A; Actual: %A" expectedvalue actualvalue
                                            1

[<assembly:AssemblyCopyrightAttribute("Copyright ? Microsoft 3456")>]
do CheckAssemblyAttribute () |> exit
