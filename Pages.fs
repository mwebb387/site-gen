module Pages

open System.IO

type SitePath =
  { Template: FileInfo option
    Pages: FileInfo list
    OtherFiles: FileInfo list
    SubPaths: SitePath list
    RootPath: DirectoryInfo }

let rec private walkFs (path: string) (rootPath: DirectoryInfo) =
  let directories =
    Directory.GetDirectories path
    |> List.ofSeq
    |> List.map (fun dir -> new DirectoryInfo(dir)) 

  let files =
    Directory.GetFiles path
    |> List.ofSeq
    |> List.map (fun file -> new FileInfo(file))
  
  let template = files |> List.tryFind (fun f -> f.Name = "+template.html")
  let pages = files |> List.filter (fun f -> f.Name <> "+template.html" && f.Name.EndsWith ".html")
  let otherFiles = files |> List.filter (fun f -> not (f.Name.EndsWith ".html"))
  let subPaths =
    directories
    |> List.map (fun d -> d.FullName)
    |> List.map (fun dir -> walkFs dir rootPath)

  { Template = template
    Pages = pages
    SubPaths = subPaths
    OtherFiles = otherFiles
    RootPath = rootPath }

let getPages (path: string) =
  walkFs path (new DirectoryInfo(path))
