open System.IO

let src = "./src"
let dest = "./build"

let args =
    List.ofArray(System.Environment.GetCommandLineArgs())
    |> List.skip 1

let build () =
    printfn "Building site..."
    Pages.getPages src
    |> Parser.buildSite dest

let watch () =
    let watchDir = new DirectoryInfo(src)
    let watcher = new FileSystemWatcher(watchDir.FullName)
    watcher.NotifyFilter <- 
        NotifyFilters.Attributes
        ||| NotifyFilters.CreationTime
        ||| NotifyFilters.DirectoryName
        ||| NotifyFilters.FileName
        ||| NotifyFilters.LastWrite
    watcher.Changed.Add (fun _ -> build())
    watcher.Created.Add (fun _ -> build())
    watcher.Deleted.Add (fun _ -> build())
    watcher.Renamed.Add (fun _ -> build())

    watcher.IncludeSubdirectories <- true
    watcher.EnableRaisingEvents <- true

    printfn "Watching..."
    printfn "Hit any key to exit."
    System.Console.ReadLine() |> ignore


// if List.contains "-w" args || List.contains "--watch" then

build()
watch()

// printfn "Done!"
