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
// ## module `Primitive`
// This holds the weakly ("stringly") typed equivalent of the command line options
// ```
  [<RequireQualifiedAccess>]
  module Primitive = begin
// ```
// ### type `CollectOptions`
// ```
    [<NoComparison>]
    type CollectOptions =
      { RecorderDirectory: System.String
        WorkingDirectory: System.String
        Executable: System.String
        LcovReport: System.String
        Threshold: System.String
        Cobertura: System.String
        OutputFile: System.String
        CommandLine: seq<System.String>
        ExposeReturnCode: bool
        SummaryFormat: System.String }
      with
        static member Create : unit -> CollectOptions
      end
// ```
// `Create()` returns an instance with all values empty and `ExposeReturnCode` is `true`.
//
// Fields that are not applicable to the use case or platform are silently ignored.
//
// ### type `PrepareOptions`
// ```
    [<NoComparison>]
    type PrepareOptions =
      { InputDirectories: seq<System.String>
        OutputDirectories: seq<System.String>
        SymbolDirectories: seq<System.String>
        Dependencies: seq<System.String>
        Keys: seq<System.String>
        StrongNameKey: System.String
        XmlReport: System.String
        FileFilter: seq<System.String>
        AssemblyFilter: seq<System.String>
        AssemblyExcludeFilter: seq<System.String>
        TypeFilter: seq<System.String>
        MethodFilter: seq<System.String>
        AttributeFilter: seq<System.String>
        PathFilter: seq<System.String>
        CallContext: seq<System.String>
        ReportFormat: System.String
        InPlace: bool
        Save: bool
        ZipFile: bool
        MethodPoint: bool
        SingleVisit: bool
        LineCover: bool
        BranchCover: bool
        CommandLine: seq<System.String>
        ExposeReturnCode: bool
        SourceLink: bool
        Defer: bool
        LocalSource: bool
        VisibleBranches: bool
        ShowStatic: string
        ShowGenerated: bool }
      with
        static member Create : unit -> PrepareOptions
      end
// ```
// `Create()` returns an instance that has all empty or `false` fields except `ExposeReturnCode`, `OpenCover`, `InPlace` and `Save` are `true`, and `ShowStatic` is `-`
//
// Fields that are not applicable to the use case or platform are silently ignored.
//
#if RUNNER
// ### type `LoggingOptions`
// ```
    [<NoComparison; NoEquality>]
    type LoggingOptions =
      { Info : System.String -> unit
        Warn : System.String -> unit
        Failure : System.String -> unit
        Echo : System.String -> unit }
      with
        static member Create : unit -> LoggingOptions
      end
// ```
// `Create()` returns an instance that just discards all strings input.  For your particular use, direct message severities appropriately.  `Echo` is used to echo the synthetic command line in case of inconsistent inputs.
#endif
// ```
  end
// ```