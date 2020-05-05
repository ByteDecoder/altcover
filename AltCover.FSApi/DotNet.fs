﻿#if RUNNER
namespace AltCover.FSApi
#else
namespace AltCoverFake.DotNet.Testing
#endif

open System
open System.Diagnostics.CodeAnalysis
open System.Linq

#if RUNNER
type PrepareParams = AltCover.OptionApi.PrepareOptions
type CollectParams = AltCover.OptionApi.CollectOptions
#endif

[<RequireQualifiedAccess>]
module DotNet =
  [<NoComparison; SuppressMessage("Microsoft.Design", "CA1034",
                                  Justification = "Idiomatic F#");
                  SuppressMessage("Gendarme.Rules.Smells",
                                  "RelaxedAvoidCodeDuplicatedInSameClassRule",
                                  Justification = "Idiomatic F#");
    AutoSerializable(false)>]
  type CLIOptions =
    | Force of bool
    | FailFast of bool
    | ShowSummary of String
    | Many of CLIOptions seq

    member self.ForceDelete =
      match self with
      | Force b -> b
      | ShowSummary _
      | FailFast _ -> false
      | Many s -> s |> Seq.exists (fun f -> f.ForceDelete)

    member self.Fast =
      match self with
      | FailFast b -> b
      | ShowSummary _
      | Force _ -> false
      | Many s -> s |> Seq.exists (fun f -> f.Fast)

    member self.Summary =
      match self with
      | ShowSummary b -> b
      | FailFast _
      | Force _ -> String.Empty
      | Many s ->
          match s
                |> Seq.map (fun f -> f.Summary)
                |> Seq.filter (String.IsNullOrWhiteSpace >> not)
                |> Seq.tryHead with
          | Some x -> x
          | _ -> String.Empty

  module internal I =
    let private arg name s = (sprintf """/p:AltCover%s="%s" """ name s).Trim()
    let private listArg name (s : String seq) =
      (sprintf """/p:AltCover%s="%s" """ name <| String.Join("|", s)).Trim()

    let private isSet s =
      s
      |> String.IsNullOrWhiteSpace
      |> not

    let private fromList name (s : String seq) = (listArg name s, s.Any())
    let internal fromArg name s = (arg name s, isSet s)
    let internal join(l : string seq) = String.Join(" ", l)

    [<SuppressMessage("Gendarme.Rules.Design.Generic", "AvoidMethodWithUnusedGenericTypeRule",
                       Justification="Compiler Generated")>]
    let internal toPrepareListArgumentList (prepare : PrepareParams) =
      [
        fromList, "SymbolDirectories", prepare.SymbolDirectories //=`"pipe '|' separated list of paths"
        fromList, "DependencyList", prepare.Dependencies //=`"pipe '|' separated list of paths"
        fromList, "Keys", prepare.Keys //=`"pipe '|' separated list of paths to strong-name keys for re-signing assemblies"
        fromList, "FileFilter", prepare.FileFilter //=`"pipe '|' separated list of file name regexes"
        fromList, "AssemblyFilter", prepare.AssemblyFilter //=`"pipe '|' separated list of names"
        fromList, "AssemblyExcludeFilter", prepare.AssemblyExcludeFilter //=`"pipe '|' separated list of names"
        fromList, "TypeFilter", prepare.TypeFilter //=`"pipe '|' separated list of names"
        fromList, "MethodFilter", prepare.MethodFilter //=`"pipe '|' separated list of names"
        fromList, "AttributeFilter", prepare.AttributeFilter //=`"pipe '|' separated list of names"
        fromList, "PathFilter", prepare.PathFilter //=`"pipe '|' separated list of file path regexes"
        fromList, "CallContext", prepare.CallContext //=`"pipe '|' separated list of names or numbers"
      ]

    let internal toPrepareFromArgArgumentList (prepare : PrepareParams) =
      [
        fromArg, "StrongNameKey", prepare.StrongNameKey //=`"path to default strong-name key for assemblies"
        fromArg, "XmlReport", prepare.XmlReport //=`"path to the xml report" default: `coverage.xml` in the project directory)
        fromArg, "ReportFormat", prepare.ReportFormat //=`"NCover" or default "OpenCover"
        fromArg, "ShowStatic", prepare.ShowStatic //=-|+|++` to mark simple code like auto-properties in the coverage file
      ]

    let internal toPrepareArgArgumentList (prepare : PrepareParams) =
      [
        (arg, "ZipFile", "false", prepare.ZipFile) //="true|false"` - set "true" to store the report in a `.zip` archive
        (arg, "MethodPoint", "false", prepare.MethodPoint)  //="true|false"` - set "true" to record only the first point of each method
        (arg, "Single", "false", prepare.Single) //="true|false"` - set "true" to record only the first visit to each point
        (arg, "LineCover", "true", prepare.LineCover) //="true|false"` - set "true" to record only line coverage in OpenCover format
        (arg, "BranchCover", "true", prepare.BranchCover)  //="true|false"` - set "true" to record only branch coverage in OpenCover format
        (arg, "SourceLink", "false", prepare.SourceLink) //=true|false` to opt for SourceLink document URLs for tracked files
        (arg, "LocalSource", "true", prepare.LocalSource) //=true|false` to ignore assemblies with `.pdb`s that don't refer to local source
        (arg, "VisibleBranches", "true", prepare.VisibleBranches) //=true|false` to ignore compiler generated internal `switch`/`match` branches
        (arg, "ShowGenerated", "true", prepare.ShowGenerated) //=true|false` to mark generated code in the coverage file
      ]

    let internal toCollectFromArgArgumentList (collect : CollectParams) =
      [
        fromArg, "LcovReport", collect.LcovReport //=`"path to lcov format result"
        fromArg, "Cobertura", collect.Cobertura //=`"path to cobertura format result"
        fromArg, "Threshold", collect.Threshold //=`"coverage threshold required"
        fromArg, "SummaryFormat", collect.SummaryFormat //=[+][B|R]` to opt for a TeamCity summary with either `B` or `R` for branch coverage accordingly, with the OpenCover format summary also present if `+` is given
      ]

    [<SuppressMessage("Gendarme.Rules.Naming", "AvoidRedundancyInMethodNameRule",
                       Justification="Internal implementation detail")>]
    let internal toCLIOptionsFromArgArgumentList (options : CLIOptions) =
      [
        fromArg, "ShowSummary", options.Summary //=true|[ConsoleColor]` to echo the coverage summary to stdout (in the colour of choice, modulo what else your build process might be doing) if the string is a valid ConsoleColor name) N.B. if this option is present, with any non-empty value then the summary will be echoed
      ]

    [<SuppressMessage("Gendarme.Rules.Naming", "AvoidRedundancyInMethodNameRule",
                       Justification="Internal implementation detail")>]
    let internal toCLIOptionsArgArgumentList (options : CLIOptions) =
      [
        arg, "Force", "true", options.ForceDelete //=true|false` to force delete any left-over `__Saved` folders from previous runs
        arg, "FailFast", "true", options.Fast //=true|false` to skip coverage collection if the unit tests fail
      ]

// "ImportModule" //=true` to emit the `Import-Module` command needed to register the `pwsh` support
// "GetVersion" //=true|false` to emit the current AltCover version

#if RUNNER
  let ToTestArgumentList (prepare : AltCover.OptionApi.PrepareOptions)
      (collect : AltCover.OptionApi.CollectOptions) (options : CLIOptions) =
#else
  let internal toTestArgumentList (prepare : PrepareParams)
      (collect : CollectParams)
      (options : CLIOptions) =
#endif
    [
      [ I.fromArg String.Empty "true" ]
      prepare
      |> I.toPrepareListArgumentList
      |> List.map(fun (f,n,a) -> f n a)
      prepare
      |> I.toPrepareFromArgArgumentList
      |> List.map(fun (f,n,a) -> f n a)
      prepare
      |> I.toPrepareArgArgumentList
      |> List.map(fun (f,n,a,x) -> (f n a,x))

      collect
      |> I.toCollectFromArgArgumentList
      |> List.map(fun (f,n,a) -> f n a)

      options
      |> I.toCLIOptionsFromArgArgumentList
      |> List.map(fun (f,n,a) -> f n a)

      options
      |> I.toCLIOptionsArgArgumentList
      |> List.map(fun (f,n,a,x) -> (f n a,x))
    ]
    |> List.concat
    |> List.filter snd
    |> List.map fst

#if RUNNER
  let ToTestArguments (prepare : AltCover.OptionApi.PrepareOptions)
      (collect : AltCover.OptionApi.CollectOptions) (options : CLIOptions) =
    ToTestArgumentList prepare collect options |> I.join
#else
  let internal toTestArguments (prepare : PrepareParams)
      (collect : CollectParams)
      (options : CLIOptions) =
    toTestArgumentList prepare collect options |> I.join
#endif