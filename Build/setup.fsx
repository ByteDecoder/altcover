#r "paket:
nuget Fake.Core.Environment >= 5.15.4
nuget Fake.Core.Process >= 5.15.4
nuget Fake.DotNet.Cli >= 5.15.4
nuget Fake.DotNet.NuGet >= 5.15.4
nuget Fake.IO.FileSystem >= 5.15.4 //"

open System
open System.IO

open Fake.DotNet
open Fake.DotNet.NuGet.Restore
open Fake.IO

open Microsoft.Win32

// Really bootstrap
let dotnetPath = "dotnet" |> Fake.Core.ProcessUtils.tryFindFileOnPath

let dotnetOptions (o : DotNet.Options) =
  match dotnetPath with
  | Some f -> { o with DotNetCliPath = f }
  | None -> o

DotNet.restore (fun o ->
  { o with Packages = [ "./packages" ]
           Common = dotnetOptions o.Common }) "./Build/dotnet-nuget.fsproj"
// Restore the NuGet packages used by the build and the Framework version
RestoreMSSolutionPackages id "./AltCover.sln"

let build = """// generated by dotnet fake run .\Build\setup.fsx
#r "paket:
nuget Fake.Core.Target >= 5.15.4
nuget Fake.Core.Environment >= 5.15.4
nuget Fake.Core.Process >= 5.15.4
nuget Fake.DotNet.AssemblyInfoFile >= 5.15.4
nuget Fake.DotNet.Cli >= 5.15.4
nuget Fake.DotNet.FxCop >= 5.15.4
nuget Fake.DotNet.ILMerge >= 5.15.4
nuget Fake.DotNet.MSBuild >= 5.15.4
nuget Fake.DotNet.NuGet >= 5.15.4
nuget Fake.DotNet.Testing.NUnit >= 5.15.4
nuget Fake.DotNet.Testing.OpenCover >= 5.15.4
nuget Fake.DotNet.Testing.XUnit2 >= 5.15.4
nuget Fake.IO.FileSystem >= 5.15.4
nuget Fake.Testing.ReportGenerator >= 5.15.4
nuget AltCode.Fake.DotNet.Gendarme >= 5.9.3.10
nuget BlackFox.CommandLine >= 1.0.0
nuget BlackFox.VsWhere >= 1.0.0
nuget coveralls.io >= 1.4.2
nuget FSharpLint.Core >= 0.12.2
nuget Markdown >= 2.2.1
nuget NUnit >= 3.12.0
nuget YamlDotNet >= 6.1.2 //"
#r "System.IO.Compression.FileSystem.dll"
#r "System.Xml"
#r "System.Xml.Linq"
#load "../AltCover/Primitive.fs"
#load "../AltCover/TypeSafe.fs"
#load "../AltCover/Api.fs"
#load "../AltCover.FSApi/Definitions.fs"
#load "../AltCover.Fake/Fake.fs"
#load "actions.fsx"
#load "targets.fsx"
#nowarn "988"

do ()"""

File.WriteAllText("./Build/build.fsx", build)