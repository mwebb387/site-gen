module Markdown

open HtmlAgilityPack
open System.IO
open System.Collections.Generic
open Markdig
open Markdig.Syntax
open Markdig.Extensions.Yaml
open YamlDotNet.Serialization
open YamlDotNet.Serialization.NamingConventions

type FrontMatter =
    { Title: string option
      Date: System.DateTime option
      Tags: string list option
      Description: string option
    }

let private markdownPipeline = 
    MarkdownPipelineBuilder()
        .UseYamlFrontMatter()
        .Build()

let private extractYamlContent (block: YamlFrontMatterBlock) =
    let lines = 
        block.Lines.Lines
        |> Seq.map (fun line -> line.ToString())
        |> Seq.filter (fun line -> line.Trim() <> "---")
        |> String.concat "\n"
    lines.Trim()

let private mapDictToFrontMatter (d: Dictionary<string, obj>) : FrontMatter =
    let getString key =
        if d.ContainsKey(key) then
            match d.[key] with
            | :? string as s -> Some s
            | other when other <> null -> Some (string other)
            | _ -> None
        else None
    let getDate key =
        if d.ContainsKey(key) then
            match d.[key] with
            | :? System.DateTime as dt -> Some dt
            | :? string as s -> 
                match System.DateTime.TryParse(s) with
                | true, dt -> Some dt
                | _ -> None
            | _ -> None
        else None
    let getStringList key =
        if d.ContainsKey(key) then
            match d.[key] with
            | :? System.Collections.IEnumerable as ie when ie <> null ->
                Some (ie |> Seq.cast<obj> |> Seq.map string |> List.ofSeq)
            | _ -> None
        else None
    { Title = getString "title"
      Date = getDate "date"
      Tags = getStringList "tags"
      Description = getString "description" }

let loadMarkdownAsHtml (path: string) =
    let markdownContent = File.ReadAllText(path)
    let document = Markdown.Parse(markdownContent, markdownPipeline)
    
    // Extract front matter if present
    let frontMatter = 
        document.Descendants<YamlFrontMatterBlock>()
        |> Seq.tryHead
        |> Option.map (fun block ->
            let yamlContent = extractYamlContent block
            let deserializer = 
                DeserializerBuilder()
                    .WithNamingConvention(CamelCaseNamingConvention.Instance)
                    .IgnoreUnmatchedProperties()
                    .Build()
            try
                let dict = deserializer.Deserialize<Dictionary<string, obj>>(new StringReader(yamlContent))
                if dict = null then { Title = None; Date = None; Tags = None; Description = None }
                else mapDictToFrontMatter dict
            with
            | _ -> { Title = None; Date = None; Tags = None; Description = None }
        )
    
    // Convert document to HTML (front matter is automatically excluded)
    let htmlContent = "<template>" + document.ToHtml(markdownPipeline) + "</template>"
    
    // Inject front matter into the HTML document as template elements
    let fmTemplates =
      match frontMatter with
      | Some fm ->
        "<template slot=\"head\"><title>" + Option.defaultValue "" fm.Title + "</title></template>" +
        "<template slot=\"meta\"><description>" + Option.defaultValue "" fm.Description + "</description>\n" +
        "<date>" + (Option.defaultValue System.DateTime.Today fm.Date).ToString("yyyy-MM-dd") + "</date>\n" +
        "<tags>" + (Option.defaultValue List.Empty fm.Tags |> String.concat ",") + "</tags>\n</template>"
      | None -> ""

    let htmlDoc = new HtmlDocument()
    htmlDoc.LoadHtml(fmTemplates + htmlContent)
    
    htmlDoc