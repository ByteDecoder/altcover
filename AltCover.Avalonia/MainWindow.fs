namespace AltCover.Avalonia

open System
open System.Collections.Generic
open System.Globalization
open System.IO
open System.Linq
open System.Resources
open System.Reflection
open System.Xml
open System.Xml.Linq
open System.Xml.Schema
open System.Xml.XPath

open AltCover.Augment
open AltCover.Visualizer
open AltCover.Visualizer.GuiCommon

open Avalonia
open Avalonia.Controls
open Avalonia.Controls.Html
open Avalonia.Markup.Xaml
open Avalonia.Media.Imaging
open Avalonia.Threading

module UICommon =

    let GetResourceString (key:string) =
        let executingAssembly = System.Reflection.Assembly.GetExecutingAssembly()
        let resources = new ResourceManager("AltCover.Visualizer.Strings", executingAssembly)
        resources.GetString(key)

module Persistence =
  let mutable save = true

  let private DefaultDocument () =
    let doc = XDocument()
    doc.Add(XElement(XName.Get "AltCover.Visualizer"))
    doc

  let private EnsureFile () =
    let profileDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)
    let dir = Directory.CreateDirectory(Path.Combine(profileDir, ".altcover"))
    let file = Path.Combine(dir.FullName, "Visualizer.xml")
    if file |> File.Exists |> not then
        (file, DefaultDocument())
    else try
            let doc = XDocument.Load(file)
            let schemas = new XmlSchemaSet()
            use xsd = new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream("AltCover.Visualizer.config.xsd"))
            schemas.Add
              (String.Empty,
               XmlReader.Create xsd)
            |> ignore
            doc.Validate(schemas, null)
            (file, doc)
         with
         | _ -> (file, DefaultDocument())

  let saveFont (font : string) =
    let file, config = EnsureFile()
    config.XPathSelectElements("//Font")
    |> Seq.toList
    |> Seq.iter(fun x -> x.Remove())
    let inject = XElement(XName.Get "Font", font)
    match config.XPathSelectElements("//CoveragePath") |> Seq.toList with
    | [] -> (config.FirstNode :?> XElement).AddFirst(inject)
    | x::_ -> inject |> x.Add
    config.Save file

  let readFont() =
    let _, config = EnsureFile()
    match config.XPathSelectElements("//Font") |> Seq.toList with
    | [] -> "Monospace Normal 10"
    | x::_ -> x.FirstNode.ToString()

  let saveFolder (path : string) =
    let file, config = EnsureFile()
    match config.XPathSelectElements("//CoveragePath") |> Seq.toList with
    | [] -> (config.FirstNode :?> XElement).AddFirst(XElement(XName.Get "CoveragePath", path))
    | x::_ -> x.RemoveAll()
              x.Add path
    config.Save file

  let readFolder () =
    let _, config = EnsureFile()
    match config.XPathSelectElements("//CoveragePath") |> Seq.toList with
    | [] -> System.IO.Directory.GetCurrentDirectory()
    | x::_ -> x.FirstNode.ToString()

  let saveCoverageFiles (coverageFiles : string list) =
    let file, config = EnsureFile()
    config.XPathSelectElements("//RecentlyOpened") |> Seq.toList
    |> Seq.iter (fun x -> x.Remove())
    let inject = config.FirstNode :?> XElement
    coverageFiles |> Seq.iter (fun path -> inject.Add(XElement(XName.Get "RecentlyOpened", path)))
    config.Save file

  let readCoverageFiles () =
    let _, config = EnsureFile()
    config.XPathSelectElements("//RecentlyOpened")
    |> Seq.map (fun n -> n.FirstNode.ToString())
    |> Seq.toList

  let saveGeometry (w:Window) =
    let file, config = EnsureFile()
    let element= XElement(XName.Get "Geometry",
                            XAttribute(XName.Get "x", w.Position.X),
                            XAttribute(XName.Get "y", w.Position.Y),
                            XAttribute(XName.Get "width", w.Width),
                            XAttribute(XName.Get "height", w.Height))

    match config.XPathSelectElements("//RecentlyOpened") |> Seq.toList with
    | [] -> (config.FirstNode :?> XElement).Add element
    | x::_ -> x.AddBeforeSelf element
    config.Save file

  let readGeometry (w:Window) =
    let _, config = EnsureFile()
    let attribute (x:XElement) a =
        x.Attribute(XName.Get a).Value
        |> Double.TryParse |> snd
    config.XPathSelectElements("//Geometry")
    |> Seq.iter (fun e ->  let width = Math.Min(attribute e "width", 750.0)
                           let height = Math.Min(attribute e "height", 750.0)

                           let bounds = w.Screens.Primary.WorkingArea
                           let x = Math.Min(Math.Max(attribute e "x", 0.0), bounds.Width - width)
                           let y = Math.Min(Math.Max(attribute e "y", 0.0), bounds.Height - height)
                           w.Height <- height
                           w.Width <- width
                           w.Position <- Point(x, y))

  let clearGeometry () =
    let file, config = EnsureFile()
    config.XPathSelectElements("//Geometry")
    |> Seq.toList
    |> Seq.iter (fun f -> f.Remove())
    config.Save file

type MessageType = Info = 0 | Warning = 1 | Error = 2

type MainWindow () as this =
    inherit Window()

    let mutable armed = false
    let mutable justOpened = String.Empty
    let mutable coverageFiles : string list = []
    let ofd = OpenFileDialog()

    let infoIcon = lazy(new Bitmap(
                         Assembly.GetExecutingAssembly().GetManifestResourceStream(
                           "AltCover.Visualizer.dialog-information.png")))
    let warnIcon = lazy(new Bitmap(
                         Assembly.GetExecutingAssembly().GetManifestResourceStream(
                           "AltCover.Visualizer.dialog-warning.png")))
    let errorIcon = lazy(new Bitmap(
                          Assembly.GetExecutingAssembly().GetManifestResourceStream(
                           "AltCover.Visualizer.dialog-error.png")))

    let AssemblyIcon =
        lazy (new Bitmap(Assembly.GetExecutingAssembly().GetManifestResourceStream("AltCover.Visualizer.Assembly_6212.png")))
    let NamespaceIcon =
        lazy (new Bitmap(Assembly.GetExecutingAssembly().GetManifestResourceStream("AltCover.Visualizer.Namespace_16x.png")))
    let ClassIcon =
        lazy (new Bitmap(Assembly.GetExecutingAssembly().GetManifestResourceStream("AltCover.Visualizer.class_16xLG.png")))
    let MethodIcon =
        lazy (new Bitmap(Assembly.GetExecutingAssembly().GetManifestResourceStream("AltCover.Visualizer.method_16xLG.png")))

    let branched =
        lazy (new Bitmap(Assembly.GetExecutingAssembly().GetManifestResourceStream("AltCover.Visualizer.Branch_12x_16x_grn.png")))
    let branch =
        lazy (new Bitmap(Assembly.GetExecutingAssembly().GetManifestResourceStream("AltCover.Visualizer.Branch_12x_16x_ylw.png")))
    let redbranch =
        lazy (new Bitmap(Assembly.GetExecutingAssembly().GetManifestResourceStream("AltCover.Visualizer.Branch_12x_16x_red.png")))
    let blank =
        lazy (new Bitmap(Assembly.GetExecutingAssembly().GetManifestResourceStream("AltCover.Visualizer.Blank_12x_16x.png")))

    let MakeTreeNode name icon =
        let text = new TextBlock()
        text.Text <- name // TODO
        let image = new Image()
        image.Source <- icon
        let display = new StackPanel()
        display.Orientation <- Orientation.Horizontal
        display.Children.Add image
        display.Children.Add text
        display

    do this.InitializeComponent()

    member private this.ShowMessageBox (status:MessageType) caption message =
        Dispatcher.UIThread.Post(fun _ ->
            this.FindControl<Image>("Status").Source <-
                    (match status with
                     | MessageType.Info -> infoIcon
                     | MessageType.Warning -> warnIcon
                     | _ -> errorIcon).Force()

            this.FindControl<TextBlock>("Caption").Text <- caption
            this.FindControl<TextBox>("Message").Text <- message
            this.FindControl<StackPanel>("MessageBox").IsVisible <- true
            this.FindControl<Grid>("Grid").IsVisible <- false)

    // Fill in the menu from the memory cache
    member private this.populateMenu () =
        let listitem = this.FindControl<MenuItem>("List")
        let items = listitem.Items.OfType<MenuItem>()
        // blank the whole menu
        items
        |> Seq.iter (fun (i : MenuItem) ->
             i.IsVisible <- false
             i.Header <- String.Empty)
        // fill in with the items we have
        Seq.zip coverageFiles items
        |> Seq.iter (fun (name, item) ->
             item.IsVisible <- true
             item.Header <- name)
        // set or clear the menu
        listitem.IsEnabled <- coverageFiles.Any()

    member private this.updateMRU path add =
      let casematch =
        match System.Environment.GetEnvironmentVariable("OS") with
        | "Windows_NT" -> StringComparison.OrdinalIgnoreCase
        | _ -> StringComparison.Ordinal

      let files =
        coverageFiles
        |> List.filter (fun n -> not (n.Equals(path, casematch)))
        |> Seq.truncate (9)
        |> Seq.toList

      coverageFiles <- (if add then (path :: files)
                          else files)
                         |> Seq.distinctBy (fun n ->
                              match casematch with
                              | StringComparison.Ordinal -> n
                              | _ -> n.ToUpperInvariant())
                         |> Seq.toList
      this.populateMenu ()
      Persistence.saveCoverageFiles coverageFiles
      this.FindControl<MenuItem>("Refresh").IsEnabled <- coverageFiles.Any()

    member private this.InvalidCoverageFileMessage (x : InvalidFile) =
        let caption = UICommon.GetResourceString "LoadError"
        let format = UICommon.GetResourceString "InvalidFile"
        let message = String.Format(System.Globalization.CultureInfo.CurrentCulture, format, x.File.FullName, x.Fault.Message)
        this.ShowMessageBox MessageType.Error caption message

    member private this.OutdatedCoverageFileMessage (x : FileInfo) =
        let caption = UICommon.GetResourceString "LoadWarning"
        let format = UICommon.GetResourceString "CoverageOutOfDate"
        let message = String.Format(System.Globalization.CultureInfo.CurrentCulture, format, x.FullName)
        this.ShowMessageBox MessageType.Warning caption message

    member private this.MissingSourceFileMessage (x : FileInfo) =
        let caption = UICommon.GetResourceString "LoadWarning"
        let format = UICommon.GetResourceString "MissingSourceFile"
        let message = String.Format(System.Globalization.CultureInfo.CurrentCulture, format, x.FullName)
        this.ShowMessageBox MessageType.Warning caption message

    member private this.OutdatedCoverageThisFileMessage (c : FileInfo) (s : FileInfo) =
        let caption = UICommon.GetResourceString "LoadWarning"
        let format = UICommon.GetResourceString "CoverageOutOfDateThisFile"
        let message = String.Format(System.Globalization.CultureInfo.CurrentCulture, format, c.FullName, s.FullName)
        this.ShowMessageBox MessageType.Warning caption message

    member private this.MissingSourceThisFileMessage (c : FileInfo) (s : FileInfo) =
        let caption = UICommon.GetResourceString "LoadWarning"
        let format = UICommon.GetResourceString "MissingSourceThisFile"
        let message = String.Format(System.Globalization.CultureInfo.CurrentCulture, format, c.FullName, s.FullName)
        this.ShowMessageBox MessageType.Warning caption message

    member private this.HideAboutBox _ =
        this.FindControl<StackPanel>("AboutBox").IsVisible <- false
        this.FindControl<Grid>("Grid").IsVisible <- true

    member private this.PopulateClassNode (model:List<TreeViewItem>) (row : TreeViewItem) (nodes : seq<MethodKey>) =
        let ApplyToModel (model:List<TreeViewItem>)  (theRow : TreeViewItem) (x : MethodKey) =
          let fullname = x.m.GetAttribute("fullname", String.Empty)

          let args =
            if String.IsNullOrEmpty(fullname) || x.name.IndexOf('(') > 0 then String.Empty
            else
              let bracket = fullname.IndexOf('(')
              if bracket < 0 then String.Empty
              else fullname.Substring(bracket)

          let displayname = x.name + args

          let offset =
            match displayname.LastIndexOf("::", StringComparison.Ordinal) with
            | -1 -> 0
            | o -> o + 2

          let newrow = TreeViewItem()
          let display = MakeTreeNode (displayname.Substring(offset)) <| MethodIcon.Force()
          newrow.Header <- display
          model.Add newrow

        let methods = nodes |> Seq.toArray
        Array.sortInPlaceWith MethodNameCompare methods
        methods |> Array.iter (ApplyToModel model row)

    member private this.PopulateNamespaceNode (model:List<TreeViewItem>) (row : TreeViewItem) (nodes : seq<MethodKey>) =
        let ApplyToModel (model:List<TreeViewItem>) (theRow : TreeViewItem) (group : string * seq<MethodKey>) =
          let name = fst group
          let newrow = TreeViewItem()
          let display = MakeTreeNode name <| ClassIcon.Force()
          newrow.Header <- display
          let items = List<TreeViewItem>()
          newrow.Items <- items
          this.PopulateClassNode items newrow (snd group)
          newrow

        let isNested (name : string) n =
          name.StartsWith(n + "+", StringComparison.Ordinal) || name.StartsWith(n + "/", StringComparison.Ordinal)
        nodes
        |> Seq.groupBy (fun x -> x.classname)
        |> Seq.sortBy fst
        |> Seq.fold (fun stack c ->
             let name = fst c
             let restack = stack |> List.filter (fst >> (isNested name))

             let pr =
               match restack with
               | [] -> row
               | (_, r) :: _ -> r

             let nr = ApplyToModel (pr.Items :?> List<TreeViewItem>) pr c
             (name, nr) :: restack) []
        |> ignore

    member private this.PopulateAssemblyNode   (model:List<TreeViewItem>) (row : TreeViewItem) (node : XPathNavigator) =
        // within the <module> we have <method> nodes with name="get_module" class="AltCover.Coverage.CoverageSchema.coverage"
        let ApplyToModel (model:List<TreeViewItem>) (theRow : TreeViewItem) (group : string * seq<MethodKey>) =
          let name = fst group
          let newrow = TreeViewItem()
          let display = MakeTreeNode name <| NamespaceIcon.Force()
          newrow.Header <- display
          let items = List<TreeViewItem>()
          newrow.Items <- items
          this.PopulateNamespaceNode items newrow (snd group)

        let methods =
          node.SelectChildren("method", String.Empty)
          |> Seq.cast<XPathNavigator>
          |> Seq.map (fun m ->
               let classfullname = m.GetAttribute("class", String.Empty)
               let lastdot = classfullname.LastIndexOf('.')
               { m = m
                 spacename =
                   if lastdot < 0 then String.Empty
                   else classfullname.Substring(0, lastdot)
                 classname =
                   if lastdot < 0 then classfullname
                   else classfullname.Substring(1 + lastdot)
                 name = m.GetAttribute("name", String.Empty) })
          |> Seq.groupBy (fun x -> x.spacename)
          |> Seq.sortBy fst

        methods |> Seq.map (ApplyToModel model row)

    member this.InitializeComponent() =
        AvaloniaXamlLoader.Load(this)

        Persistence.readGeometry this
        coverageFiles <- Persistence.readCoverageFiles ()
        this.populateMenu ()
        ofd.InitialDirectory <- Persistence.readFolder()
        ofd.Title <- UICommon.GetResourceString "Open Coverage File"
        ofd.AllowMultiple <- false
        let filterBits = (UICommon.GetResourceString "SelectXml").Split([| '|' |])
                         |> Seq.map (fun f -> let entry = f.Split([| '%' |])
                                              let filter = FileDialogFilter()
                                              filter.Name <- entry |> Seq.head
                                              filter.Extensions <- List(entry |> Seq.tail)
                                              filter)
        ofd.Filters <- List(filterBits)

        this.Title <- "AltCover.Visualizer"

        ["Open"; "Refresh"; "Font"; "About"; "Exit"]
        |> Seq.iter (fun n -> let item = this.FindControl<TextBlock>(n + "Text")
                              item.Text <- UICommon.GetResourceString n)

        this.FindControl<MenuItem>("Exit").Click
        |> Event.add (fun _ -> if Persistence.save then
                                  Persistence.saveGeometry this
                               Application.Current.Exit())

        this.FindControl<MenuItem>("About").Click
        |> Event.add(fun _ -> this.FindControl<StackPanel>("AboutBox").IsVisible <- true
                              this.FindControl<Grid>("Grid").IsVisible <- false)

        let openFile = new Event<String option>()
        this.FindControl<MenuItem>("Open").Click
                    |> Event.add(fun _ -> this.HideAboutBox ()
                                          async {
                                                  (ofd.ShowAsync(this)
                                                   |> Async.AwaitTask
                                                   |> Async.RunSynchronously).
                                                     FirstOrDefault()
                                                  |> Option.nullable
                                                  |> openFile.Trigger
                                          } |> Async.Start )

        let click = openFile.Publish
                    |> Event.choose id
                    |> Event.map (fun n -> ofd.InitialDirectory <- Path.GetDirectoryName n
                                           if Persistence.save then
                                              Persistence.saveFolder ofd.InitialDirectory
                                           justOpened <- n
                                           -1)
        let select =
           this.FindControl<MenuItem>("List").Items.OfType<MenuItem>()
           |> Seq.mapi (fun n (i : MenuItem) -> i.Click |> Event.map (fun _ -> n))

        // The sum of all these events -- we have explicitly selected a file
        let fileSelection = select |> Seq.fold Event.merge click

        let refresh = this.FindControl<MenuItem>("Refresh").Click
                      |> Event.map (fun _ -> 0)

        select |> Seq.fold Event.merge refresh
        |> Event.add this.HideAboutBox

        Event.merge fileSelection refresh
        |> Event.add (fun index ->
             async {
               let current =
                 FileInfo(if index < 0 then justOpened
                          else coverageFiles.[index])

               match CoverageFile.LoadCoverageFile current with
               | Left failed ->
                 this.InvalidCoverageFileMessage failed
                 Dispatcher.UIThread.Post(fun _ -> this.updateMRU current.FullName false)
               | Right coverage ->
                 // check if coverage is newer that the source files
                 let sourceFiles =
                   coverage.Document.CreateNavigator().Select("//seqpnt/@document")
                   |> Seq.cast<XPathNavigator>
                   |> Seq.map (fun x -> x.Value)
                   |> Seq.distinct

                 let missing =
                   sourceFiles
                   |> Seq.map (fun f -> new FileInfo(f))
                   |> Seq.filter (fun f -> not f.Exists)

                 if not (Seq.isEmpty missing) then this.MissingSourceFileMessage current
                 let newer =
                   sourceFiles
                   |> Seq.map (fun f -> new FileInfo(f))
                   |> Seq.filter (fun f -> f.Exists && f.LastWriteTimeUtc > current.LastWriteTimeUtc)
                 // warn if not
                 if not (Seq.isEmpty newer) then this.OutdatedCoverageFileMessage current

                 let tree = this.FindControl<TreeView>("Tree")
                 tree.Items.OfType<IDisposable>()
                 |> Seq.iter (fun x -> x.Dispose())

                 let ApplyToModel (model:List<TreeViewItem>) (group : XPathNavigator * string) =
                   let name = snd group
                   let row = TreeViewItem()
                   model.Add row
                   let display = MakeTreeNode name <| AssemblyIcon.Force()
                   row.Header <- display
                   row.Items <- this.PopulateAssemblyNode model row (fst group)

                 Dispatcher.UIThread.Post(fun _ ->
                     let items = List<TreeViewItem>()
                     coverage.Document.CreateNavigator().Select("//module") |> Seq.cast<XPathNavigator>
                     |> Seq.map (fun node -> (node, node.GetAttribute("assemblyIdentity", String.Empty).Split(',') |> Seq.head))
                     |> Seq.sortBy (fun nodepair -> snd nodepair)
                     |> Seq.iter (ApplyToModel items)
                     tree.Items <- items
                     this.FindControl<MenuItem>("Refresh").IsEnabled <- true
                     )
             }
             |> Async.Start
             )

        this.FindControl<TextBlock>("Program").Text <- "AltCover.Visualizer " + AssemblyVersionInformation.AssemblyFileVersion
        this.FindControl<TextBlock>("Description").Text <- UICommon.GetResourceString "ProgramDescription"

        let copyright = AssemblyVersionInformation.AssemblyCopyright
        this.FindControl<TextBlock>("Copyright").Text <- copyright
        this.FindControl<TextBlock>("Comments").Text <- UICommon.GetResourceString "ProgramComments"

        let link = this.FindControl<HtmlLabel>("Link")
        link.Text <- """<center><a href="http://www.github.com/SteveGilham/altcover">""" +
                      UICommon.GetResourceString "WebsiteLabel" +
                      "</a></center>"

        link.PointerPressed |> Event.add (fun _ -> armed <- true)
        link.PointerLeave |> Event.add (fun _ -> armed <- false)
        link.PointerReleased
        |> Event.add (fun _ -> ())

        // Windows -- Process Start (url)
        // Mac -- ("open", url)
        // *nix -- ("xdg-open", url)
        //Application.Instance.Open("http://www.github.com/SteveGilham/altcover"))

        this.FindControl<TextBlock>("License").Text <- UICommon.GetResourceString "AboutDialog.License"
        this.FindControl<TextBlock>("MIT").Text <- String.Format(CultureInfo.InvariantCulture,
                                                                 UICommon.GetResourceString "License",
                                                                 copyright)
        // MessageBox
        let okButton = this.FindControl<Button>("DismissMessageBox")
        okButton.Content <- "OK"
        okButton.Click
        |> Event.add(fun _ -> this.FindControl<StackPanel>("MessageBox").IsVisible <- false
                              this.FindControl<Grid>("Grid").IsVisible <- true)
        // AboutBox
        let okButton2 = this.FindControl<Button>("DismissAboutBox")
        okButton2.Content <- "OK"
        okButton2.Click
        |> Event.add(fun _ -> this.FindControl<StackPanel>("AboutBox").IsVisible <- false
                              this.FindControl<Grid>("Grid").IsVisible <- true)