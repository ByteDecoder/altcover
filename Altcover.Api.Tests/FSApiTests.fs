namespace Tests

open System
open System.IO
open System.Reflection
open System.Xml
open System.Xml.Linq
open System.Xml.Schema

open AltCover
open Swensen.Unquote

#if NETCOREAPP3_0
[<AttributeUsage(AttributeTargets.Method)>]
type TestAttribute() = class
    inherit Attribute()
end
#else
type TestAttribute = NUnit.Framework.TestAttribute
#endif

module FSApiTests =
  let SolutionDir() =
    SolutionRoot.location

  [<Test>]
  let FormatFromCoverletMeetsSpec() =
    let probe = typeof<Tests.DU.MyClass>.Assembly.Location
    let resource =
      Assembly.GetExecutingAssembly().GetManifestResourceNames()
      |> Seq.find
            (fun n -> n.EndsWith("OpenCoverForPester.coverlet.xml", StringComparison.Ordinal))
    use stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resource)
    let baseline = XDocument.Load(stream)
    let transformed = AltCover.OpenCoverUtilities.FormatFromCoverlet baseline [| probe |]
    test <@ transformed |> isNull |> not @>

    let schemata = XmlSchemaSet()
    let sresource =
      Assembly.GetExecutingAssembly().GetManifestResourceNames()
      |> Seq.find
            (fun n -> n.EndsWith("OpenCoverStrict.xsd", StringComparison.Ordinal))
    use sstream = Assembly.GetExecutingAssembly().GetManifestResourceStream(sresource)
    use sreader = new StreamReader(sstream)
    use xsreader = System.Xml.XmlReader.Create(sreader)
    schemata.Add(String.Empty, xsreader) |> ignore

    transformed.Validate(schemata, null)

    test <@ transformed |> isNull |> not @>

  [<Test>]
  let OpenCoverToLcov() =
    let doc = XmlDocument()
    use stream=
        Assembly.GetExecutingAssembly().GetManifestResourceStream("altcover.api.tests.core.HandRolledMonoCoverage.xml")
    doc.Load stream
    use stream2 = new MemoryStream()
    AltCover.CoverageFormats.ConvertToLcov doc stream2
    use stream2a = new MemoryStream(stream2.GetBuffer())
    use rdr = new StreamReader(stream2a)
    let result = rdr.ReadToEnd()

    use stream3 =
        Assembly.GetExecutingAssembly().GetManifestResourceStream("altcover.api.tests.core.HandRolledMonoCoverage.lcov")
    use rdr2 = new StreamReader(stream3)
    let expected = rdr2.ReadToEnd()

    test <@ result = expected @>