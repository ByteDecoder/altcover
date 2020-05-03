﻿#if RUNNER
namespace AltCover
#else
namespace AltCoverFake.DotNet.Testing
#endif

open System
open System.Diagnostics.CodeAnalysis

#if RUNNER
type PrepareParams = AltCover.FSApi.PrepareOptions
type CollectParams = AltCover.FSApi.CollectOptions
#else
type PrepareParams = AltCoverFake.DotNet.Testing.AltCover.PrepareOptions
type CollectParams = AltCoverFake.DotNet.Testing.AltCover.CollectOptions
#endif

[<SuppressMessage("Gendarme.Rules.Smells",
                                  "RelaxedAvoidCodeDuplicatedInSameClassRule",
                                  Justification = "Not worth trying to unify these functions")>]
module private ArgsHelper =
  let item a x =
    if x |> String.IsNullOrWhiteSpace then [] else [ a; x ]

  let optionalItem a x l =
    if x
        |> String.IsNullOrWhiteSpace
        || l |> List.exists (fun i -> i = x) then
      []
    else
      [ a + ":" + x ]

module internal Args =
  let internal itemList a x =
    if x |> isNull then
      []
    else
      x
      |> Seq.collect (fun i -> [ a; i ])
      |> Seq.toList

  let private flag a x =
    if x then [ a ] else []

  let internal listItems(args : PrepareParams) =
    [ ("-i", args.InputDirectories)
      ("-o", args.OutputDirectories)
      ("-y", args.SymbolDirectories)
      ("-d", args.Dependencies)
      ("-k", args.Keys)
      ("-f", args.FileFilter)
      ("-s", args.AssemblyFilter)
      ("-e", args.AssemblyExcludeFilter)
      ("-t", args.TypeFilter)
      ("-m", args.MethodFilter)
      ("-a", args.AttributeFilter)
      ("-p", args.PathFilter)
      ("-c", args.CallContext) ]

  let internal itemLists(args : PrepareParams) =
    args
    |> listItems
    |> List.collect (fun (a, b) -> itemList a b)

  let internal plainItems(args : PrepareParams) =
    [ ("--sn", args.StrongNameKey)
      ("--reportFormat", args.ReportFormat)
      ("-x", args.XmlReport) ]

  let internal items(args : PrepareParams) =
    args
    |> plainItems
    |> List.collect (fun (a, b) -> ArgsHelper.item a b)

  let internal options(args : PrepareParams) =
    [ ("--showstatic", args.ShowStatic, [ "-" ]) ]

  let internal optItems(args : PrepareParams) =
    args
    |> options
    |> List.collect (fun (a, b, c) -> ArgsHelper.optionalItem a b c)

  let internal flagItems(args : PrepareParams) =
    [ ("--inplace", args.InPlace)
      ("--save", args.Save)
      ("--zipfile", args.ZipFile)
      ("--methodpoint", args.MethodPoint)
      ("--single", args.Single)
      ("--linecover", args.LineCover)
      ("--branchcover", args.BranchCover)
      ("--dropReturnCode", (args.ExposeReturnCode |> not))
      ("--sourcelink", args.SourceLink)
      ("--defer", args.Defer)
      ("--localSource", args.LocalSource)
      ("--visibleBranches", args.VisibleBranches)
      ("--showGenerated", args.ShowGenerated) ]

  let internal flags(args : PrepareParams) =
    args
    |> flagItems
    |> List.collect (fun (a, b) -> flag a b)

  let prepare(args : PrepareParams) =
    let argsList = args.CommandLine |> Seq.toList

    let trailing =
      if List.isEmpty argsList then [] else "--" :: argsList

    let parameters =
      [ itemLists; items; optItems; flags ] |> List.collect (fun f -> f args)

    [ parameters; trailing ] |> List.concat

  let internal buildCollect(args : CollectParams) =
    let argsList = args.CommandLine |> Seq.toList

    let trailing =
      if List.isEmpty argsList then [] else "--" :: argsList

    let exe = args.Executable

    [ [ "Runner" ]
      ArgsHelper.item "-r" args.RecorderDirectory
      ArgsHelper.item "-w" args.WorkingDirectory
      ArgsHelper.item "-x" exe
      ArgsHelper.item "-l" args.LcovReport
      ArgsHelper.item "-t" args.Threshold
      ArgsHelper.item "-c" args.Cobertura
      ArgsHelper.item "-o" args.OutputFile
      flag "--collect" (exe |> String.IsNullOrWhiteSpace)
      flag "--dropReturnCode" (args.ExposeReturnCode |> not)
      ArgsHelper.optionalItem "--teamcity" args.SummaryFormat []
      trailing ]

  let collect(args : CollectParams) =
    args
    |> buildCollect
    |> List.concat