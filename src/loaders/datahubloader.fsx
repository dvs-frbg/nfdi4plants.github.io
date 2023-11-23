#load "../globals.fsx"

open Globals
open System.IO

type Layout = 
    | TextOnly
    | ImageOnly
    | TextTopImageBottom
    | TextBottomImageTop
    | TextLeftImageRight
    | TextRightImageLeft

    static member ofString (str:string) =
        match str.Trim().ToLowerInvariant() with
        | "text-only"               -> TextOnly
        | "image-only"              -> ImageOnly
        | "text-top-image-bottom"   -> TextTopImageBottom
        | "text-bottom-image-top"   -> TextBottomImageTop
        | "text-left-image-right"   -> TextLeftImageRight
        | "text-right-image-left"   -> TextRightImageLeft
        | _ -> failwithf "Layout type %s does not exist." str


type DatahubHero = {
    Heading:        string
    Title:          string
    BgColor:        string
    EmphasisColor:  string
    Image:          string
    Layout:         Layout
    Body:           string
} with

    static member create heading title bgColor emphasisColor image layout body = 
        {
            Heading         = heading      
            Title           = title        
            BgColor         = bgColor      
            EmphasisColor   = emphasisColor
            Image           = image        
            Layout          = layout     
            Body            = body  
        }

    static member fromFile heroMarkdownPath = 
        
        let text = System.IO.File.ReadAllText heroMarkdownPath

        let config = MarkdownProcessing.getFrontMatter text
        let content = MarkdownProcessing.getMarkdownContent text

        let heading         = config |> Map.find "heading" |> MarkdownProcessing.trimString
        let title           = config |> Map.find "title" |> MarkdownProcessing.trimString
        let bgColor         = config |> Map.find "bg-color" |> MarkdownProcessing.trimString
        let emphasisColor   = config |> Map.find "emphasis-color" |> MarkdownProcessing.trimString
        let image           = config |> Map.find "image" |> MarkdownProcessing.trimString
        let layout          = config |> Map.find "layout" |> MarkdownProcessing.trimString |> Layout.ofString
        let body            = content 

        DatahubHero.create heading title bgColor emphasisColor image layout body 

type DatahubCard = {
    Title           : string
    BgColor         : string
    BorderColor     : string
    EmphasisColor   : string
    Image           : string
    Layout          : Layout
    Index           : int
    Body            : string
} with

    static member create title bgColor borderColor emphasisColor image layout index body = 
        {
            Title           = title        
            BgColor         = bgColor      
            BorderColor     = borderColor
            EmphasisColor   = emphasisColor
            Image           = image        
            Layout          = layout     
            Index           = index
            Body            = body  
        }

    static member fromFile heroMarkdownPath = 
        
        let text = System.IO.File.ReadAllText heroMarkdownPath

        let config = MarkdownProcessing.getFrontMatter text
        let content = MarkdownProcessing.getMarkdownContent text

        let title           = config |> Map.find "title" |> MarkdownProcessing.trimString
        let bgColor         = config |> Map.find "bg-color" |> MarkdownProcessing.trimString
        let borderColor     = config |> Map.find "border-color" |> MarkdownProcessing.trimString
        let emphasisColor   = config |> Map.find "emphasis-color" |> MarkdownProcessing.trimString
        let image           = config |> Map.find "image" |> MarkdownProcessing.trimString
        let layout          = config |> Map.find "layout" |> MarkdownProcessing.trimString |> Layout.ofString
        let index           = config |> Map.find "index" |> MarkdownProcessing.trimString |> int
        let body            = content 

        DatahubCard.create title bgColor borderColor emphasisColor image layout index body 

type DatahubPage = {
    Path        : string
    OutputName  : string
    Hero        : DatahubHero
    Cards       : DatahubCard []
} with

    static member create path outName hero cards = {
        Path        = path 
        OutputName  = outName
        Hero        = hero 
        Cards       = cards
    }

    static member fromFolder (rootDir:string) (folderPath:string) =
        
        let relPath = folderPath.Replace(rootDir,"")
        let outName = DirectoryInfo(folderPath).Name

        let content = 
            System.IO.Directory.GetFiles folderPath
            |> Array.filter (Predicates.isMarkdownFile)

        let hero = 
            content 
            |> Array.tryFind (fun c -> Path.GetFileNameWithoutExtension c = "hero")
            |> Option.map DatahubHero.fromFile

        let cards = 
            content 
            |> Array.filter (fun c -> Path.GetFileNameWithoutExtension c <> "hero")
            |> Array.map DatahubCard.fromFile

        match hero with
        | Some h -> 
            printfn "[Datahub-Loader]: Found hero at %s" folderPath
            DatahubPage.create relPath outName h cards
        | None -> failwithf "[Datahub-Loader] directory %s does not contain a hero.md file." folderPath


let contentDir = "content/datahub"

let loader (projectRoot: string) (siteContent: SiteContents) =
    let DatahubPath = System.IO.Path.Combine(projectRoot, contentDir)
    try 
        siteContent.Add (DatahubPage.fromFolder projectRoot DatahubPath)
    with e as exn -> 
        siteContent.AddError {Path = DatahubPath; Message = (sprintf "Unable to load mainpagecard %s. \n%s" DatahubPath e.Message); Phase = Loading}

    printfn "[Datahub-Loader]: Done loading Datahub pages"
    siteContent