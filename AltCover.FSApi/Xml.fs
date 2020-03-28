namespace AltCover

open System
open System.Diagnostics.CodeAnalysis
open System.IO
open System.Linq
open System.Reflection
open System.Xml
open System.Xml.Linq
open System.Xml.Schema
open System.Xml.Xsl

open Augment

module XmlExtensions =
  type System.Xml.Linq.XElement with
    member self.SetAttribute(name: string, value : string) =
      let attr = self.Attribute(XName.Get name)
      if attr |> isNull
      then self.Add(XAttribute(XName.Get name, value))
      else attr.Value <- value
    member self.GetAttribute(name: string) =
      let attr = self.Attribute(XName.Get name)
      if attr |> isNull
      then String.Empty
      else attr.Value

[<RequireQualifiedAccess>]
module XmlUtilities =
  [<SuppressMessage("Microsoft.Design", "CA1059",
                    Justification = "converts concrete types")>]
  let ToXmlDocument(document : XDocument) =
    let xmlDocument = XmlDocument()
    use xmlReader = document.CreateReader()
    xmlDocument.Load(xmlReader)

    let xDeclaration = document.Declaration
    if xDeclaration.IsNotNull
    then
      let xmlDeclaration =
        xmlDocument.CreateXmlDeclaration
          (xDeclaration.Version, xDeclaration.Encoding, xDeclaration.Standalone)

      xmlDocument.InsertBefore(xmlDeclaration, xmlDocument.FirstChild) |> ignore
    xmlDocument

  [<SuppressMessage("Microsoft.Design", "CA1059",
                    Justification = "converts concrete types")>]
  [<System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Gendarme.Rules.Maintainability", "AvoidUnnecessarySpecializationRule",
    Justification = "AvoidSpeculativeGenerality too")>]
  let ToXDocument(xmlDocument : XmlDocument) =
    use nodeReader = new XmlNodeReader(xmlDocument)
    nodeReader.MoveToContent() |> ignore // skips leading comments
    let xdoc = XDocument.Load(nodeReader)
    let cn = xmlDocument.ChildNodes
    let decl' = cn.OfType<XmlDeclaration>() |> Seq.tryHead
    match decl' with
    | None -> ()
    | Some decl ->
        xdoc.Declaration <- XDeclaration(decl.Version, decl.Encoding, decl.Standalone)
    cn.OfType<XmlProcessingInstruction>()
    |> Seq.rev
    |> Seq.iter
         (fun func -> xdoc.AddFirst(XProcessingInstruction(func.Target, func.Data)))
    xdoc

  // Approved way is ugly -- https://docs.microsoft.com/en-us/visualstudio/code-quality/ca2202?view=vs-2019
  // Also, this rule is deprecated
  let internal loadSchema(format : AltCover.Base.ReportFormat) =
    let schemas = new XmlSchemaSet()

    let resource =
      match format with
      | AltCover.Base.ReportFormat.NCover -> "AltCover.FSApi.xsd.NCover.xsd"
      | _ -> "AltCover.FSApi.xsd.OpenCover.xsd"

    use stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resource)
    use reader = new StreamReader(stream)
    use xreader = XmlReader.Create(reader)
    schemas.Add(String.Empty, xreader) |> ignore
    schemas

  let internal loadTransform(name : string) =
    let transform = new XslCompiledTransform()
    use stream =
      Assembly.GetExecutingAssembly()
              .GetManifestResourceStream("AltCover.FSApi.xsl." + name + ".xsl")
    use reader = new StreamReader(stream)
    use xreader = XmlReader.Create(reader)
    transform.Load(xreader, XsltSettings.TrustedXslt, XmlUrlResolver())
    transform

  [<SuppressMessage("Microsoft.Design", "CA1059",
                    Justification = "converts concrete types")>]
  let internal discoverFormat(xmlDocument : XmlDocument) =
    let format =
      if xmlDocument.SelectNodes("/CoverageSession").OfType<XmlNode>().Any()
      then AltCover.Base.ReportFormat.OpenCover
      else AltCover.Base.ReportFormat.NCover

    let schema = loadSchema format
    xmlDocument.Schemas <- schema
    xmlDocument.Validate(null)
    format

  let internal assemblyNameWithFallback path fallback =
    try
      AssemblyName.GetAssemblyName(path).FullName
    with
    | :? ArgumentException
    | :? FileNotFoundException
    | :? System.Security.SecurityException
    | :? BadImageFormatException
    | :? FileLoadException -> fallback

  [<SuppressMessage("Microsoft.Design", "CA1059", Justification = "Implies concrete type")>]
  let internal prependDeclaration(x : XmlDocument) =
    let xmlDeclaration = x.CreateXmlDeclaration("1.0", "utf-8", null)
    x.InsertBefore(xmlDeclaration, x.FirstChild) |> ignore