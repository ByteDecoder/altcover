﻿// Both the .net framework/mono and .net core releases publish MSBuld tasks from the main assembly (AltCover.exe or AltCover.dll, respectively) that wrap the command-line functionality (as documented here under [Usage](https://github.com/SteveGilham/altcover/wiki/Usage)).
//
// # namespace `AltCover`
//
// For the C# programmer, `member [Name] : [type] with get, set` is a `[type]` valued property called `[Name]`; and `string array` is just `string[]` spelled out longhand.
//
// ```
namespace AltCover
open Microsoft.Build.Framework
open Microsoft.Build.Utilities
// ```
// ## Task `AltCover.Prepare`
// This is the instrumentation mode with `--opencover --save --inplace` as default.  Associated parameters are
// ```
type Prepare =
  class
    inherit Task
    new : unit -> Prepare
    override Execute : unit -> bool
    member Message : text:string -> unit
    member AssemblyExcludeFilter : string array with get, set
    member AssemblyFilter : string array with get, set
    member AttributeFilter : string array with get, set
    member BranchCover : bool with get, set
    member CallContext : string array with get, set
    member CommandLine : string array with get, set
    member Defer : bool with get, set
    member Dependencies : string array with get, set
    member ExposeReturnCode : bool with get, set
    member FileFilter : string array with get, set
    member InPlace : bool with get, set
    member InputDirectories : string array with get, set
    member Keys : string array with get, set
    member LineCover : bool with get, set
    member LocalSource : bool with get, set
    member MethodFilter : string array with get, set
    member MethodPoint : bool with get, set
    member OutputDirectories : string array with get, set
    member PathFilter : string array with get, set
    member TopLevel : string array with get, set
    member ReportFormat : string with get, set
    member Save : bool with get, set
    member ShowGenerated : bool with get, set
    member ShowStatic : string with get, set
    member SingleVisit : bool with get, set
    member SourceLink : bool with get, set
    member StrongNameKey : string with get, set
    member SymbolDirectories : string array with get, set
    member TypeFilter : string array with get, set
    member VisibleBranches : bool with get, set
    member XmlReport : string with get, set
    member ZipFile : bool with get, set
  end
// ```
// ## Task `AltCover.Collect`
// This is `runner` mode with `--collect` as default.  Associated parameters are
// ```
type Collect =
  class
    inherit Task
    new : unit -> Collect
    override Execute : unit -> bool
    member Message : text:string -> unit
    [<Output>]
    member Summary : string
    member Cobertura : string with get, set
    member CommandLine : string array with get, set
    member Executable : string with get, set
    member ExposeReturnCode : bool with get, set
    member LcovReport : string with get, set
    member OutputFile : string with get, set
    [<Required>]
    member RecorderDirectory : string with get, set
    member SummaryFormat : string with get, set
    member Threshold : string with get, set
    member WorkingDirectory : string with get, set
  end
// ```
// ## Task `AltCover.PowerShell`
// This is the `ImportModule` option; it takes no parameters.
// ```
type PowerShell =
  class
    inherit Task
    new : unit -> PowerShell
    override Execute : unit -> bool
  end
// ```
// ## Task `AltCover.GetVersion`
// This is the `Version` option; it takes no parameters.
// ```
type GetVersion =
  class
    inherit Task
    new : unit -> GetVersion
    override Execute : unit -> bool
  end
// ```
// ## Task `AltCover.Echo`
// Outputs a possibly coloured string of text to `stdout`.
// ```
type Echo =
  class
    inherit Task
    new : unit -> Echo
    override Execute : unit -> bool
    member Colour : string with get, set
    [<Required>]
    member Text : string with get, set
  end
// ```
#if NETCOREAPP2_0
// ## Task `AltCover.RunSettings`
// Used by the .net core implementation to inject an altcover datacollector, by creating a temporary tun settings file that includes AltCover as well as any user-defined settings.
//
// Not intended for general use, but see the `AltCover.targets` file for how it is used around the test stage.
// ```
type RunSettings =
  class
    inherit Task
    new : unit -> RunSettings
    override Execute : unit -> bool
    [<Output>]
    member Extended : string with get, set
    member TestSetting : string with get, set
  end
#endif
// ```
// ## General
// The task parameters match the command line arguments in name and function, except that `SymbolDirectories` is pluralised, and the deprecated `CommandLine` is everything after a `--` as one single string.  If `AltCover.Collect`'s `Executable` parameter is set, that switches the virtual `--collect` flag off.