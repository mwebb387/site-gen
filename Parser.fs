module Parser

open HtmlAgilityPack
open System.IO


let private loadHtml (path: string) =
    let template = new HtmlDocument()
    template.Load path
    template

let private replaceSlotContents (slot: HtmlNode, template: HtmlNode option) =
    if slot <> null then
      match template with
        | Some temp ->
          for child in Seq.rev temp.ChildNodes do
            slot.ParentNode.InsertAfter(child, slot) |> ignore
        | None -> ()
      slot.Remove ()

let private templateSelect (slot: HtmlNode) templateNodes =
    try
        Some(Seq.find (fun (t: HtmlNode) -> t.GetAttributeValue("slot", "") = slot.GetAttributeValue("name", "")) templateNodes)
    with
        _ -> None

let private buildWithTemplate (pageDoc: HtmlDocument) (templateDoc: HtmlDocument) =
    let slotNodes = templateDoc.DocumentNode.SelectNodes "//slot"
    let templateNodes = pageDoc.DocumentNode.SelectNodes "//template"

    let pairs =
        slotNodes
        |> Seq.map (fun slot -> slot, templateSelect slot templateNodes)

    for pair in pairs do
        replaceSlotContents pair

    templateDoc
    // templateDoc.DocumentNode.OuterHtml |> printfn "%s"

let private buildWithComponents (pageDoc: HtmlDocument) (componentDocs: HtmlDocument list) =
  for componentDoc in componentDocs do
    let slotNodes = pageDoc.DocumentNode.SelectNodes "//slot"
    let componentNodes = componentDoc.DocumentNode.SelectNodes "//template"

    let pairs =
        slotNodes
        |> Seq.map (fun slot -> slot, templateSelect slot componentNodes)

    for pair in pairs do
        replaceSlotContents pair

  pageDoc

let private buildPage (page: FileInfo) (components: FileInfo list) (template: FileInfo option) =
    let templateDoc =
        match template with
            | Some t -> Some(loadHtml t.FullName)
            | _ -> None
    
    // Load page as HTML or convert from markdown
    let pageDoc = 
        if page.Name.EndsWith ".md" then
            Markdown.loadMarkdownAsHtml page.FullName
        else
            loadHtml page.FullName

    let componentDocs = components |> List.map (fun c -> loadHtml c.FullName)

    let pageDoc = buildWithComponents pageDoc componentDocs

    match templateDoc with
        | Some tDoc -> buildWithTemplate pageDoc tDoc
        | None -> pageDoc

let private writePage (sourcePage: FileInfo) (rootDir: DirectoryInfo) (dest: DirectoryInfo) (pageDoc: HtmlDocument)  =
    let destFilePath = sourcePage.FullName.Replace(rootDir.FullName, dest.FullName)
    // Change .md extension to .html for output
    let destFilePath = if sourcePage.Name.EndsWith ".md" then Path.ChangeExtension(destFilePath, ".html") else destFilePath
    let destFile = new FileInfo(destFilePath)
    Directory.CreateDirectory destFile.DirectoryName |> ignore

    pageDoc.Save destFile.FullName

let rec buildSite (dest: string) (sitePath: Pages.SitePath) =
  let destDir = new DirectoryInfo(dest)
  destDir.Create()

  for page in sitePath.Pages do
    buildPage page sitePath.Components sitePath.Template
    |> writePage page sitePath.RootPath destDir

  for file in sitePath.OtherFiles do
    let destFilePath = file.FullName.Replace(sitePath.RootPath.FullName, destDir.FullName)
    let destFile = new FileInfo(destFilePath)
    Directory.CreateDirectory destFile.DirectoryName |> ignore
    File.Copy(file.FullName, destFile.FullName, true)

  for subPath in sitePath.SubPaths do
    buildSite dest subPath
