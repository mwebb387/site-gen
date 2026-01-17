module Pages

open System.IO

type SitePath =
  { Template: FileInfo option
    Pages: FileInfo list
    Components: FileInfo list
    OtherFiles: FileInfo list
    SubPaths: SitePath list
    RootPath: DirectoryInfo }

let rec private walkFs (path: string) (rootPath: DirectoryInfo) (parentTemplate: FileInfo option) =
  let directories =
    Directory.GetDirectories path
    |> List.ofSeq
    |> List.map (fun dir -> new DirectoryInfo(dir)) 

  let files =
    Directory.GetFiles path
    |> List.ofSeq
    |> List.map (fun file -> new FileInfo(file))
  
  let template = files |> List.tryFind (fun f -> f.Name = "+template.html") |> Option.orElse parentTemplate

  // Combine HTML pages with markdown pages
  let htmlPages = files |> List.filter (fun f -> not (f.Name.StartsWith "+") && f.Name.EndsWith ".html")
  let markdownPages = files |> List.filter (fun f -> not (f.Name.StartsWith "+") && f.Name.EndsWith ".md")
  let pages = htmlPages @ markdownPages

  let components = files |> List.filter (fun f -> f.Name.StartsWith "+" && f.Name <> "+template.html" && f.Name.EndsWith ".html")

  let otherFiles = files |> List.filter (fun f -> not (f.Name.EndsWith ".html") && not (f.Name.EndsWith ".md"))

  let subPaths =
    directories
    |> List.map (fun d -> d.FullName)
    |> List.map (fun dir -> walkFs dir rootPath template)

  { Template = template
    Pages = pages
    Components = components
    SubPaths = subPaths
    OtherFiles = otherFiles
    RootPath = rootPath }

let getPages (path: string) =
  walkFs path (new DirectoryInfo(path)) None
