#r "paket:
nuget Fake.Core.Target >= 5.19.1
nuget Fake.Core.Environment >= 5.19.1
nuget Fake.Core.Process >= 5.19.1
nuget Fake.DotNet.Cli >= 5.19.1
nuget Fake.DotNet.NuGet >= 5.19.1
nuget Fake.IO.FileSystem >= 5.19.1 //"

open System
open System.IO

open Fake.Core
open Fake.Core.TargetOperators
open Fake.DotNet
open Fake.DotNet.NuGet.Restore
open Fake.IO

open Microsoft.Win32

let consoleBefore = (Console.ForegroundColor, Console.BackgroundColor)

// Really bootstrap
let dotnetPath = "dotnet" |> Fake.Core.ProcessUtils.tryFindFileOnPath

let dotnetOptions (o : DotNet.Options) =
  match dotnetPath with
  | Some f -> { o with DotNetCliPath = f }
  | None -> o

DotNet.restore (fun o ->
  { o with
      Packages = [ "./packages" ]
      Common = dotnetOptions o.Common }) "./Build/dotnet-cli.csproj"

let build = """// generated by dotnet fake run .\Build\setup.fsx
#r "paket:
nuget Fake.Core.Target >= 5.19.1
nuget Fake.Core.Environment >= 5.19.1
nuget Fake.Core.Process >= 5.19.1
nuget Fake.DotNet.AssemblyInfoFile >= 5.19.1
nuget Fake.DotNet.Cli >= 5.19.1
nuget Fake.DotNet.FxCop >= 5.19.1
nuget Fake.DotNet.ILMerge >= 5.19.1
nuget Fake.DotNet.MSBuild >= 5.19.1
nuget Fake.DotNet.NuGet >= 5.19.1
nuget Fake.DotNet.Testing.NUnit >= 5.19.1
nuget Fake.DotNet.Testing.OpenCover >= 5.19.1
nuget Fake.DotNet.Testing.XUnit2 >= 5.19.1
nuget Fake.IO.FileSystem >= 5.19.1
nuget Fake.DotNet.Testing.Coverlet >= 5.19.1
nuget Fake.Testing.ReportGenerator >= 5.19.1
nuget AltCode.Fake.DotNet.Gendarme >= 5.9.3.10
nuget BlackFox.CommandLine >= 1.0.0
nuget BlackFox.VsWhere >= 1.0.0
nuget FSharpLint.Core >= 0.13.2
nuget Markdown >= 2.2.1
nuget NUnit >= 3.12.0
nuget YamlDotNet >= 8.1 //"
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

let _Target s f =
  Target.description s
  Target.create s f

let resetColours _ =
  Console.ForegroundColor <- consoleBefore |> fst
  Console.BackgroundColor <- consoleBefore |> snd

Target.description "ResetConsoleColours"
Target.createFinal "ResetConsoleColours" resetColours
Target.activateFinal "ResetConsoleColours"

// Restore the NuGet packages used by the build and the Framework version

_Target "Preparation" (fun _ ->
  RestoreMSSolutionPackages id "./MCS.sln"
)
_Target "Legacy" (fun _ ->
  if File.Exists "./AltCover.Legacy.sln"
  then RestoreMSSolutionPackages id "./AltCover.Legacy.sln"
)

let defaultTarget() =
  resetColours()
  if Environment.isWindows
  then "Preparation"
  else "Legacy"

Target.runOrDefault <| defaultTarget ()


