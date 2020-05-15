﻿#if RUNNER
// # namespace `AltCover`
// ```
namespace AltCover
// ```
#else
// # namespace `AltCoverFake.DotNet.Testing`
// ```
namespace AltCoverFake.DotNet.Testing
// ```
#endif
// ```
open System
open System.Collections.Generic
open System.Diagnostics.CodeAnalysis
// ```
// ## module `Abstract`
// This represents the weakly ("stringly") typed equivalent of the command line options in a C# friendly manner
// as interfaces with the values expressed as read-only properties
//
// Refer to the types in C# either as
//
// ```
// using Altcover;
// Abstract.IPrepareOptions prep = ...  // or whichever
// ```
// or
// ```
// using static Altcover.Abstract;
// IPrepareOptions prep = ... // or whichever
// ```
//
//
// ```
module Abstract =
// ```
// ## interface `ICollectOptions`
//
// ```
  type ICollectOptions =
    interface
    abstract member RecorderDirectory : String with get
    abstract member WorkingDirectory : String with get
    abstract member Executable : String with get
      [<SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly",
                        Justification="Lcov is a name")>]
    abstract member LcovReport : String with get
    abstract member Threshold : String with get
      [<SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly",
                        Justification="Cobertura is a name")>]
    abstract member Cobertura : String with get
    abstract member OutputFile : String with get
    abstract member CommandLine : IEnumerable<String> with get
    abstract member ExposeReturnCode : bool with get
    abstract member SummaryFormat : String with get
  end
// ```
// ## interface `IPrepareOptions`
//
// ```
  type IPrepareOptions =
    interface
    abstract member InputDirectories : IEnumerable<String> with get
    abstract member OutputDirectories : IEnumerable<String> with get
    abstract member SymbolDirectories : IEnumerable<String> with get
    abstract member Dependencies : IEnumerable<String> with get
    abstract member Keys : IEnumerable<String> with get
    abstract member StrongNameKey : String with get
    abstract member XmlReport : String with get
    abstract member FileFilter : IEnumerable<String> with get
    abstract member AssemblyFilter : IEnumerable<String> with get
    abstract member AssemblyExcludeFilter : IEnumerable<String> with get
    abstract member TypeFilter : IEnumerable<String> with get
    abstract member MethodFilter : IEnumerable<String> with get
    abstract member AttributeFilter : IEnumerable<String> with get
    abstract member PathFilter : IEnumerable<String> with get
    abstract member TopLevel : IEnumerable<String> with get
    abstract member CallContext : IEnumerable<String> with get
    abstract member ReportFormat : String with get
    abstract member InPlace : bool with get
    abstract member Save : bool with get
    abstract member ZipFile : bool with get
    abstract member MethodPoint : bool with get
    abstract member SingleVisit : bool with get
    abstract member LineCover : bool with get
    abstract member BranchCover : bool with get
    abstract member CommandLine : IEnumerable<String> with get
    abstract member ExposeReturnCode : bool with get
    abstract member SourceLink : bool with get
    abstract member Defer : bool with get
    abstract member LocalSource : bool with get
    abstract member VisibleBranches : bool with get
    abstract member ShowStatic : string with get
    abstract member ShowGenerated : bool with get
  end
// ```
#if RUNNER
// ### interface `ILoggingOptions`
//
// ```
  type ILoggingOptions =
    interface
    abstract member Info : Action<String> with get
    abstract member Warn : Action<String> with get
    abstract member Failure : Action<String> with get
    abstract member Echo : Action<String> with get
    end
#endif