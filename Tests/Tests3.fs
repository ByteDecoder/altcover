namespace Tests

open System
open System.IO
open System.Reflection

open AltCover
open AltCover.Augment
open Mono.Options
open NUnit.Framework
open Mono.Cecil.Cil
#if NETCOREAPP2_1
open Microsoft.VisualStudio.TestTools.UnitTesting
#endif

[<TestFixture;TestClass>]
module AltCoverTests3 =
#if NETCOREAPP2_0
    let monoSample1 = "../_Mono/Sample1"
#else
    let recorderSnk = typeof<AltCover.Node>.Assembly.GetManifestResourceNames()
                      |> Seq.find (fun n -> n.EndsWith(".Recorder.snk", StringComparison.Ordinal))
#endif

    [<SetUp;TestInitialize>]
    let SetUp() =
      Main.init()

    // AltCover.fs and CommandLine.fs

    [<Test;TestMethod>]
    let ShouldLaunchWithExpectedOutput() =
      // Hack for running while instrumented
      let where = Assembly.GetExecutingAssembly().Location
      let path =
        Path.Combine(where.Substring(0, where.IndexOf("_Binaries")), "_Mono/Sample1")
#if NETCOREAPP2_0
      let path' =
        if Directory.Exists path then path
        else Path.Combine(where.Substring(0, where.IndexOf("_Binaries")), monoSample1)
#else
      let path' = path
#endif
      let files = Directory.GetFiles(path')

      let program =
        files
        |> Seq.filter (fun x -> x.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
        |> Seq.head

      let saved = (Console.Out, Console.Error)
      let e0 = Console.Out.Encoding
      let e1 = Console.Error.Encoding
      AltCover.ToConsole()
      try
        use stdout =
          { new StringWriter() with
              member self.Encoding = e0 }

        use stderr =
          { new StringWriter() with
              member self.Encoding = e1 }

        Console.SetOut stdout
        Console.SetError stderr
        let nonWindows = System.Environment.GetEnvironmentVariable("OS") <> "Windows_NT"

        let exe, args =
          if nonWindows then ("mono", "\"" + program + "\"")
          else (program, String.Empty)

        let r =
          CommandLine.Launch exe args
            (Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location))
        NUnit.Framework.Assert.That(r, Is.EqualTo 0)
        NUnit.Framework.Assert.That(stderr.ToString(), Is.Empty)
        let result = stdout.ToString()

        let quote =
          if System.Environment.GetEnvironmentVariable("OS") = "Windows_NT" then "\""
          else String.Empty

        let expected =
          "Command line : '" + quote + exe + quote + " " + args + "\'"
          + Environment.NewLine + "Where is my rocket pack? " + Environment.NewLine
        NUnit.Framework.Assert.That(result, Is.EqualTo(expected))
      finally
        Console.SetOut(fst saved)
        Console.SetError(snd saved)

    [<Test;TestMethod>]
    let ShouldHaveExpectedOptions() =
      let options = Main.DeclareOptions()
      NUnit.Framework.Assert.That(options.Count, Is.EqualTo 28)
      NUnit.Framework.Assert.That
        (options
         |> Seq.filter (fun x -> x.Prototype <> "<>")
         |> Seq.forall (fun x -> (String.IsNullOrWhiteSpace >> not) x.Description))
      NUnit.Framework.Assert.That(options
                  |> Seq.filter (fun x -> x.Prototype = "<>")
                  |> Seq.length, Is.EqualTo 1)

    [<Test;TestMethod>]
    let ParsingJunkIsAnError() =
      let options = Main.DeclareOptions()
      let parse = CommandLine.ParseCommandLine [| "/@thisIsNotAnOption" |] options
      match parse with
      | Right _ -> NUnit.Framework.Assert.Fail()
      | Left(x, y) ->
        NUnit.Framework.Assert.That(x, Is.EqualTo "UsageError")
        NUnit.Framework.Assert.That(y, Is.SameAs options)

    [<Test;TestMethod>]
    let ParsingJunkBeforeSeparatorIsAnError() =
      let options = Main.DeclareOptions()
      let parse =
        CommandLine.ParseCommandLine
          [| "/@thisIsNotAnOption"; "--"; "this should be OK" |] options
      match parse with
      | Right _ -> NUnit.Framework.Assert.Fail()
      | Left(x, y) ->
        NUnit.Framework.Assert.That(x, Is.EqualTo "UsageError")
        NUnit.Framework.Assert.That(y, Is.SameAs options)

    [<Test;TestMethod>]
    let ParsingJunkAfterSeparatorIsExpected() =
      let options = Main.DeclareOptions()
      let input = [| "--"; "/@thisIsNotAnOption"; "this should be OK" |]
      let parse = CommandLine.ParseCommandLine input options
      match parse with
      | Left _ -> NUnit.Framework.Assert.Fail()
      | Right(x, y) ->
        NUnit.Framework.Assert.That(x, Is.EquivalentTo(input |> Seq.skip 1))
        NUnit.Framework.Assert.That(y, Is.SameAs options)

    [<Test;TestMethod>]
    let ParsingHelpGivesHelp() =
      let options = Main.DeclareOptions()
      let input = [| "--?" |]
      let parse = CommandLine.ParseCommandLine input options
      match parse with
      | Left _ -> NUnit.Framework.Assert.Fail()
      | Right(x, y) -> NUnit.Framework.Assert.That(y, Is.SameAs options)
      match CommandLine.ProcessHelpOption parse with
      | Right _ -> NUnit.Framework.Assert.Fail()
      | Left(x, y) ->
        NUnit.Framework.Assert.That(x, Is.EqualTo "HelpText")
        NUnit.Framework.Assert.That(y, Is.SameAs options)
      // a "not sticky" test
      match CommandLine.ParseCommandLine [| "/t"; "x" |] options
            |> CommandLine.ProcessHelpOption with
      | Left _ -> NUnit.Framework.Assert.Fail()
      | Right(x, y) ->
        NUnit.Framework.Assert.That(y, Is.SameAs options)
        NUnit.Framework.Assert.That(x, Is.Empty)

    [<Test;TestMethod>]
    let ParsingErrorHelpGivesHelp() =
      let options = Main.DeclareOptions()

      let input =
        [| "--o"
           Path.GetInvalidPathChars() |> String |]

      let parse = CommandLine.ParseCommandLine input options
      match parse with
      | Right _ -> NUnit.Framework.Assert.Fail()
      | Left(x, y) ->
        NUnit.Framework.Assert.That(x, Is.EqualTo "UsageError")
        NUnit.Framework.Assert.That(y, Is.SameAs options)
      match CommandLine.ProcessHelpOption parse with
      | Right _ -> NUnit.Framework.Assert.Fail()
      | Left(x, y) ->
        NUnit.Framework.Assert.That(x, Is.EqualTo "UsageError")
        NUnit.Framework.Assert.That(y, Is.SameAs options)
      // a "not sticky" test
      match CommandLine.ParseCommandLine [| "/t"; "x" |] options
            |> CommandLine.ProcessHelpOption with
      | Left _ -> NUnit.Framework.Assert.Fail()
      | Right(x, y) ->
        NUnit.Framework.Assert.That(y, Is.SameAs options)
        NUnit.Framework.Assert.That(x, Is.Empty)

    [<Test;TestMethod>]
    let ParsingAttributesGivesAttributes() =
      try
        Visitor.NameFilters.Clear()
        let options = Main.DeclareOptions()
        let input = [| "-a"; "1;a"; "--a"; "2"; "/a"; "3"; "-a=4"; "--a=5"; "/a=6" |]
        let parse = CommandLine.ParseCommandLine input options
        match parse with
        | Left _ -> NUnit.Framework.Assert.Fail()
        | Right(x, y) ->
          NUnit.Framework.Assert.That(y, Is.SameAs options)
          NUnit.Framework.Assert.That(x, Is.Empty)
        NUnit.Framework.Assert.That(Visitor.NameFilters.Count, Is.EqualTo 7)
        NUnit.Framework.Assert.That(Visitor.NameFilters
                    |> Seq.forall (fun x ->
                         match x.Scope with
                         | FilterScope.Attribute -> true
                         | _ -> false))
        NUnit.Framework.Assert.That
          (Visitor.NameFilters
           |> Seq.map (fun x ->
                match x.Scope with
                | FilterScope.Attribute -> x.Regex.ToString()
                | _ -> "*"), Is.EquivalentTo ([| "1"; "a"; "2"; "3"; "4"; "5"; "6" |] ))
        NUnit.Framework.Assert.That
          (Visitor.NameFilters |> Seq.forall (fun x -> x.Sense = Exclude))
      finally
        Visitor.NameFilters.Clear()

    [<Test;TestMethod>]
    let ParsingMethodsGivesMethods() =
      try
        Visitor.NameFilters.Clear()
        let options = Main.DeclareOptions()
        let input = [| "-m"; "1"; "--m"; "2;b;c"; "/m"; "3"; "-m=4"; "--m=5"; "/m=6" |]
        let parse = CommandLine.ParseCommandLine input options
        match parse with
        | Left _ -> NUnit.Framework.Assert.Fail()
        | Right(x, y) ->
          NUnit.Framework.Assert.That(y, Is.SameAs options)
          NUnit.Framework.Assert.That(x, Is.Empty)
        NUnit.Framework.Assert.That(Visitor.NameFilters.Count, Is.EqualTo 8)
        NUnit.Framework.Assert.That(Visitor.NameFilters
                    |> Seq.forall (fun x ->
                         match x.Scope with
                         | FilterScope.Method -> true
                         | _ -> false))
        NUnit.Framework.Assert.That
          (Visitor.NameFilters
           |> Seq.map (fun x ->
                match x.Scope with
                | FilterScope.Method -> x.Regex.ToString()
                | _ -> "*"), Is.EquivalentTo ([| "1"; "2"; "b"; "c"; "3"; "4"; "5"; "6" |] ))
        NUnit.Framework.Assert.That
          (Visitor.NameFilters |> Seq.forall (fun x -> x.Sense = Exclude))
      finally
        Visitor.NameFilters.Clear()

    [<Test;TestMethod>]
    let ParsingTypesGivesTypes() =
      try
        Visitor.NameFilters.Clear()
        let options = Main.DeclareOptions()
        let input = [| "-t"; "1"; "--t"; "2"; "/t"; "3;x;y;z"; "-t=4"; "--t=5"; "/t=6" |]
        let parse = CommandLine.ParseCommandLine input options
        match parse with
        | Left _ -> NUnit.Framework.Assert.Fail()
        | Right(x, y) ->
          NUnit.Framework.Assert.That(y, Is.SameAs options)
          NUnit.Framework.Assert.That(x, Is.Empty)
        NUnit.Framework.Assert.That(Visitor.NameFilters.Count, Is.EqualTo 9)
        NUnit.Framework.Assert.That(Visitor.NameFilters
                    |> Seq.forall (fun x ->
                         match x.Scope with
                         | FilterScope.Type -> true
                         | _ -> false))
        NUnit.Framework.Assert.That
          (Visitor.NameFilters
           |> Seq.map (fun x ->
                match x.Scope with
                | FilterScope.Type -> x.Regex.ToString()
                | _ -> "*"),
           Is.EquivalentTo ([| "1"; "2"; "3"; "x"; "y"; "z"; "4"; "5"; "6" |] ))
        NUnit.Framework.Assert.That
          (Visitor.NameFilters |> Seq.forall (fun x -> x.Sense = Exclude))
      finally
        Visitor.NameFilters.Clear()

    [<Test;TestMethod>]
    let ParsingAssembliesGivesAssemblies() =
      try
        Visitor.NameFilters.Clear()
        let options = Main.DeclareOptions()
        let input = [| "-s"; "?1"; "--s"; "2"; "/s"; "3"; "-s=4;p;q"; "--s=5"; "/s=6" |]
        let parse = CommandLine.ParseCommandLine input options
        match parse with
        | Left _ -> NUnit.Framework.Assert.Fail()
        | Right(x, y) ->
          NUnit.Framework.Assert.That(y, Is.SameAs options)
          NUnit.Framework.Assert.That(x, Is.Empty)
        NUnit.Framework.Assert.That(Visitor.NameFilters.Count, Is.EqualTo 8)
        NUnit.Framework.Assert.That(Visitor.NameFilters
                    |> Seq.forall (fun x ->
                         match x.Scope with
                         | FilterScope.Assembly -> true
                         | _ -> false))
        NUnit.Framework.Assert.That
          (Visitor.NameFilters
           |> Seq.map (fun x ->
                match x.Scope with
                | FilterScope.Assembly -> x.Regex.ToString()
                | _ -> "*"),
           Is.EquivalentTo ([| "1"; "2"; "3"; "4"; "p"; "q"; "5"; "6" |]) )
        NUnit.Framework.Assert.That
           (Visitor.NameFilters
            |> Seq.map (fun x -> if x.Sense = Include then 1 else 0),
            Is.EquivalentTo ([| 1; 0; 0; 0; 0; 0;  0; 0 |]) )
      finally
        Visitor.NameFilters.Clear()

    [<Test;TestMethod>]
    let ParsingEscapeCasesWork() =
      try
        Visitor.NameFilters.Clear()
        let options = Main.DeclareOptions()
        let input = [| "-s"; "1\u0001a"; "--s"; "\u0000d"; "/s"; "3"; "-s=4;;p;q"; "--s=5"; "/s=6" |]
        let parse = CommandLine.ParseCommandLine input options
        match parse with
        | Left _ -> NUnit.Framework.Assert.Fail()
        | Right(x, y) ->
          NUnit.Framework.Assert.That(y, Is.SameAs options)
          NUnit.Framework.Assert.That(x, Is.Empty)
        NUnit.Framework.Assert.That(Visitor.NameFilters.Count, Is.EqualTo 7)
        NUnit.Framework.Assert.That(Visitor.NameFilters
                    |> Seq.forall (fun x ->
                         match x.Scope with
                         | FilterScope.Assembly -> true
                         | _ -> false))
        NUnit.Framework.Assert.That
          (Visitor.NameFilters
           |> Seq.map (fun x ->
                match x.Scope with
                | FilterScope.Assembly -> x.Regex.ToString()
                | _ -> "*"), Is.EquivalentTo ([| "1|a"; "\\d"; "3"; "4;p"; "q"; "5"; "6" |] ))
        NUnit.Framework.Assert.That
          (Visitor.NameFilters |> Seq.forall (fun x -> x.Sense = Exclude))
      finally
        Visitor.NameFilters.Clear()

    [<Test;TestMethod>]
    let ParsingModulesGivesModules() =
      try
        Visitor.NameFilters.Clear()
        let options = Main.DeclareOptions()
        let input = [| "-e"; "1"; "--e"; "2"; "/e"; "3"; "-e=4;p;q"; "--e=5"; "/e=6" |]
        let parse = CommandLine.ParseCommandLine input options
        match parse with
        | Left _ -> NUnit.Framework.Assert.Fail()
        | Right(x, y) ->
          NUnit.Framework.Assert.That(y, Is.SameAs options)
          NUnit.Framework.Assert.That(x, Is.Empty)
        NUnit.Framework.Assert.That(Visitor.NameFilters.Count, Is.EqualTo 8)
        NUnit.Framework.Assert.That(Visitor.NameFilters
                    |> Seq.forall (fun x ->
                         match x.Scope with
                         | FilterScope.Module -> true
                         | _ -> false))
        NUnit.Framework.Assert.That
          (Visitor.NameFilters
           |> Seq.map (fun x ->
                match x.Scope with
                | FilterScope.Module -> x.Regex.ToString()
                | _ -> "*"), Is.EquivalentTo ([| "1"; "2"; "3"; "4"; "p"; "q"; "5"; "6" |] ))
        NUnit.Framework.Assert.That
          (Visitor.NameFilters |> Seq.forall (fun x -> x.Sense = Exclude))
      finally
        Visitor.NameFilters.Clear()

    [<Test;TestMethod>]
    let ParsingFilesGivesFiles() =
      try
        Visitor.NameFilters.Clear()
        let options = Main.DeclareOptions()
        let input = [| "-f"; "1"; "--f"; "2"; "/f"; "3"; "-f=4"; "--f=5;m;n"; "/f=6" |]
        let parse = CommandLine.ParseCommandLine input options
        match parse with
        | Left _ -> NUnit.Framework.Assert.Fail()
        | Right(x, y) ->
          NUnit.Framework.Assert.That(y, Is.SameAs options)
          NUnit.Framework.Assert.That(x, Is.Empty)
        NUnit.Framework.Assert.That(Visitor.NameFilters.Count, Is.EqualTo 8)
        NUnit.Framework.Assert.That(Visitor.NameFilters
                    |> Seq.forall (fun x ->
                         match x.Scope with
                         | FilterScope.File -> true
                         | _ -> false))
        NUnit.Framework.Assert.That
          (Visitor.NameFilters
           |> Seq.map (fun x ->
                match x.Scope with
                | FilterScope.File -> x.Regex.ToString()
                | _ -> "*"), Is.EquivalentTo ([| "1"; "2"; "3"; "4"; "5"; "m"; "n"; "6" |] ))
        NUnit.Framework.Assert.That
          (Visitor.NameFilters |> Seq.forall (fun x -> x.Sense = Exclude))
      finally
        Visitor.NameFilters.Clear()

    [<Test;TestMethod>]
    let ParsingPathsGivesPaths() =
      try
        Visitor.NameFilters.Clear()
        let options = Main.DeclareOptions()
        let input = [| "-p"; "1"; "--p"; "2"; "/p"; "3"; "-p=4"; "--p=5;m;n"; "/p=6" |]
        let parse = CommandLine.ParseCommandLine input options
        match parse with
        | Left _ -> NUnit.Framework.Assert.Fail()
        | Right(x, y) ->
          NUnit.Framework.Assert.That(y, Is.SameAs options)
          NUnit.Framework.Assert.That(x, Is.Empty)
        NUnit.Framework.Assert.That(Visitor.NameFilters.Count, Is.EqualTo 8)
        NUnit.Framework.Assert.That(Visitor.NameFilters
                    |> Seq.forall (fun x ->
                         match x.Scope with
                         | FilterScope.Path -> true
                         | _ -> false))
        NUnit.Framework.Assert.That
          (Visitor.NameFilters
           |> Seq.map (fun x ->
                match x.Scope with
                | FilterScope.Path -> x.Regex.ToString()
                | _ -> "*"), Is.EquivalentTo ([| "1"; "2"; "3"; "4"; "5"; "m"; "n"; "6" |] ))
        NUnit.Framework.Assert.That
          (Visitor.NameFilters |> Seq.forall (fun x -> x.Sense = Exclude))
      finally
        Visitor.NameFilters.Clear()

    [<Test;TestMethod>]
    let ParsingXmlGivesXml() =
      try
        Visitor.reportPath <- None
        let options = Main.DeclareOptions()
        let unique = Guid.NewGuid().ToString()
        let where = Assembly.GetExecutingAssembly().Location
        let path = Path.Combine(Path.GetDirectoryName(where), unique)
        let input = [| "-x"; path |]
        let parse = CommandLine.ParseCommandLine input options
        match parse with
        | Left _ -> NUnit.Framework.Assert.Fail()
        | Right(x, y) ->
          NUnit.Framework.Assert.That(y, Is.SameAs options)
          NUnit.Framework.Assert.That(x, Is.Empty)
        match Visitor.reportPath with
        | None -> NUnit.Framework.Assert.Fail()
        | Some x -> NUnit.Framework.Assert.That(Path.GetFileName x, Is.EqualTo unique)
      finally
        Visitor.reportPath <- None

    [<Test;TestMethod>]
    let ParsingMultipleXmlGivesFailure() =
      try
        Visitor.reportPath <- None
        let options = Main.DeclareOptions()
        let unique = Guid.NewGuid().ToString()

        let input =
          [| "-x"
             unique
             "/x"
             unique.Replace("-", "+") |]

        let parse = CommandLine.ParseCommandLine input options
        match parse with
        | Right _ -> NUnit.Framework.Assert.Fail()
        | Left(x, y) ->
          NUnit.Framework.Assert.That(y, Is.SameAs options)
          NUnit.Framework.Assert.That(x, Is.EqualTo "UsageError")
          NUnit.Framework.Assert.That(CommandLine.error |> Seq.head, Is.EqualTo "--xmlReport : specify this only once")
      finally
        Visitor.reportPath <- None

    [<Test;TestMethod>]
    let ParsingBadXmlGivesFailure() =
      try
        Visitor.reportPath <- None
        let options = Main.DeclareOptions()
        let unique = Guid.NewGuid().ToString()

        let input =
          [| "-x"
             unique.Replace("-", Path.GetInvalidPathChars() |> String) |]

        let parse = CommandLine.ParseCommandLine input options
        match parse with
        | Right _ -> NUnit.Framework.Assert.Fail()
        | Left(x, y) ->
          NUnit.Framework.Assert.That(y, Is.SameAs options)
          NUnit.Framework.Assert.That(x, Is.EqualTo "UsageError")
      finally
        Visitor.reportPath <- None

    [<Test;TestMethod>]
    let ParsingNoXmlGivesFailure() =
      try
        Visitor.reportPath <- None
        let options = Main.DeclareOptions()
        let unique = Guid.NewGuid().ToString()
        let input = [| "-x" |]
        let parse = CommandLine.ParseCommandLine input options
        match parse with
        | Right _ -> NUnit.Framework.Assert.Fail()
        | Left(x, y) ->
          NUnit.Framework.Assert.That(y, Is.SameAs options)
          NUnit.Framework.Assert.That(x, Is.EqualTo "UsageError")
      finally
        Visitor.reportPath <- None

    [<Test;TestMethod>]
    let ParsingEmptyXmlGivesFailure() =
      try
        Visitor.reportPath <- None
        let options = Main.DeclareOptions()
        let unique = Guid.NewGuid().ToString()
        let input = [| "-x"; " " |]
        let parse = CommandLine.ParseCommandLine input options
        match parse with
        | Right _ -> NUnit.Framework.Assert.Fail()
        | Left(x, y) ->
          NUnit.Framework.Assert.That(y, Is.SameAs options)
          NUnit.Framework.Assert.That(x, Is.EqualTo "UsageError")
      finally
        Visitor.reportPath <- None

    [<Test;TestMethod>]
    let ParsingInputGivesInput() =
      try
        Visitor.inputDirectories.Clear()
        let options = Main.DeclareOptions()
        let unique = Path.GetFullPath(".")
        let input = [| "-i"; unique |]
        let parse = CommandLine.ParseCommandLine input options
        match parse with
        | Left _ -> NUnit.Framework.Assert.Fail()
        | Right(x, y) ->
          NUnit.Framework.Assert.That(y, Is.SameAs options)
          NUnit.Framework.Assert.That(x, Is.Empty)
        match Visitor.inputDirectories |> Seq.toList with
        | [ x ] -> NUnit.Framework.Assert.That(x, Is.EqualTo unique)
        | _ -> NUnit.Framework.Assert.Fail()
      finally
        Visitor.inputDirectories.Clear()

    [<Test;TestMethod>]
    let ParsingMultipleInputIsOKToo() =
      try
        Visitor.inputDirectories.Clear()
        Visitor.outputDirectories.Clear()
        Visitor.inplace := false
        let options = Main.DeclareOptions()

        let input =
          [| "-i"
             Path.GetFullPath(".")
             "/i"
             Path.GetFullPath("..") |]

        let parse = CommandLine.ParseCommandLine input options
        let pcom a b = Path.Combine(b,a) |> Path.GetFullPath
        match parse with
        | Left _ -> NUnit.Framework.Assert.Fail()
        | _ -> Visitor.inputDirectories |> Seq.toList
               |> List.zip ([ "."; ".." ] |> List.map Path.GetFullPath)
               |> List.iter NUnit.Framework.Assert.AreEqual
               Visitor.OutputDirectories() |> Seq.toList
               |> List.zip ([ "."; ".." ] |> List.map (pcom "__Instrumented") )
               |> List.iter NUnit.Framework.Assert.AreEqual

               Visitor.inplace := true
               Visitor.outputDirectories.Add "maybe"
               Visitor.OutputDirectories() |> Seq.toList
               |> List.zip [ Path.GetFullPath "maybe"; ".." |> (pcom "__Saved")]
               |> List.iter NUnit.Framework.Assert.AreEqual

      finally
        Visitor.outputDirectories.Clear()
        Visitor.inputDirectories.Clear()
        Visitor.inplace := false

    [<Test;TestMethod>]
    let ParsingDuplicateInputGivesFailure() =
      try
        Visitor.inputDirectories.Clear()
        let options = Main.DeclareOptions()
        let here = Path.GetFullPath(".")
        let input = [| "-i"; here; "-i"; here |]
        let parse = CommandLine.ParseCommandLine input options
        match parse with
        | Right _ -> NUnit.Framework.Assert.Fail()
        | Left(x, y) ->
          NUnit.Framework.Assert.That(y, Is.SameAs options)
          NUnit.Framework.Assert.That(x, Is.EqualTo "UsageError")
          NUnit.Framework.Assert.That(CommandLine.error,
                      Is.EquivalentTo ([here + " was already specified for --inputDirectory"]))
      finally
        Visitor.inputDirectories.Clear()

    [<Test;TestMethod>]
    let ParsingBadInputGivesFailure() =
      try
        Visitor.inputDirectories.Clear()
        let options = Main.DeclareOptions()
        let unique = Guid.NewGuid().ToString().Replace("-", "*")
        let input = [| "-i"; unique |]
        let parse = CommandLine.ParseCommandLine input options
        match parse with
        | Right _ -> NUnit.Framework.Assert.Fail()
        | Left(x, y) ->
          NUnit.Framework.Assert.That(y, Is.SameAs options)
          NUnit.Framework.Assert.That(x, Is.EqualTo "UsageError")
      finally
        Visitor.inputDirectories.Clear()

    [<Test;TestMethod>]
    let ParsingNoInputGivesFailure() =
      try
        Visitor.inputDirectories.Clear()
        let options = Main.DeclareOptions()
        let input = [| "-i" |]
        let parse = CommandLine.ParseCommandLine input options
        match parse with
        | Right _ -> NUnit.Framework.Assert.Fail()
        | Left(x, y) ->
          NUnit.Framework.Assert.That(y, Is.SameAs options)
          NUnit.Framework.Assert.That(x, Is.EqualTo "UsageError")
      finally
        Visitor.inputDirectories.Clear()

    [<Test;TestMethod>]
    let ParsingOutputGivesOutput() =
      try
        Visitor.outputDirectories.Clear()
        let options = Main.DeclareOptions()
        let unique = Guid.NewGuid().ToString()
        let input = [| "-o"; unique |]
        let parse = CommandLine.ParseCommandLine input options
        match parse with
        | Left _ -> NUnit.Framework.Assert.Fail()
        | Right(x, y) ->
          NUnit.Framework.Assert.That(y, Is.SameAs options)
          NUnit.Framework.Assert.That(x, Is.Empty)
        match Visitor.outputDirectories |> Seq.toList with
        | [ x ] -> NUnit.Framework.Assert.That(Path.GetFileName x, Is.EqualTo unique)
        | _ -> NUnit.Framework.Assert.Fail()
      finally
        Visitor.outputDirectories.Clear()

    [<Test;TestMethod>]
    let ParsingDuplicateOutputGivesFailure() =
      try
        Visitor.outputDirectories.Clear()
        let options = Main.DeclareOptions()
        let unique = Guid.NewGuid().ToString()
        let input = [| "-o"; unique; "-o"; unique |]
        let parse = CommandLine.ParseCommandLine input options
        match parse with
        | Right _ -> NUnit.Framework.Assert.Fail()
        | Left(x, y) ->
          NUnit.Framework.Assert.That(y, Is.SameAs options)
          NUnit.Framework.Assert.That(x, Is.EqualTo "UsageError")
          NUnit.Framework.Assert.That(CommandLine.error,
                      Is.EquivalentTo ([Path.GetFullPath(unique) + " was already specified for --outputDirectory"]))
      finally
        Visitor.outputDirectories.Clear()

    [<Test;TestMethod>]
    let ParsingMultipleOutputIsOK() =
      try
        Visitor.inputDirectories.Clear()
        Visitor.outputDirectories.Clear()
        let options = Main.DeclareOptions()
        let unique = Guid.NewGuid().ToString()
        let u2 = unique.Replace("-", "+")
        let outs = [ unique; u2 ] |> List.map Path.GetFullPath

        let input =
          [| "-o"
             unique
             "/o"
             u2
          |]

        let parse = CommandLine.ParseCommandLine input options
        match parse with
        | Left _ -> NUnit.Framework.Assert.Fail()
        | _ -> NUnit.Framework.Assert.That (Visitor.outputDirectories, Is.EquivalentTo outs)
               NUnit.Framework.Assert.That (Visitor.OutputDirectories(), Is.EquivalentTo (outs |> Seq.take 1))
      finally
        Visitor.outputDirectories.Clear()

    [<Test;TestMethod>]
    let ParsingBadOutputGivesFailure() =
      try
        Visitor.outputDirectories.Clear()
        let options = Main.DeclareOptions()
        let unique = Guid.NewGuid().ToString()

        let input =
          [| "-o"
             unique.Replace("-", Path.GetInvalidPathChars() |> String) |]

        let parse = CommandLine.ParseCommandLine input options
        match parse with
        | Right _ -> NUnit.Framework.Assert.Fail()
        | Left(x, y) ->
          NUnit.Framework.Assert.That(y, Is.SameAs options)
          NUnit.Framework.Assert.That(x, Is.EqualTo "UsageError")
      finally
        Visitor.outputDirectories.Clear()

    [<Test;TestMethod>]
    let ParsingNoOutputGivesFailure() =
      try
        Visitor.outputDirectories.Clear()
        let options = Main.DeclareOptions()
        let input = [| "-o" |]
        let parse = CommandLine.ParseCommandLine input options
        match parse with
        | Right _ -> NUnit.Framework.Assert.Fail()
        | Left(x, y) ->
          NUnit.Framework.Assert.That(y, Is.SameAs options)
          NUnit.Framework.Assert.That(x, Is.EqualTo "UsageError")
      finally
        Visitor.outputDirectories.Clear()

    [<Test;TestMethod>]
    let ParsingEmptyOutputGivesFailure() =
      try
        Visitor.outputDirectories.Clear()
        let options = Main.DeclareOptions()
        let input = [| "-o"; " " |]
        let parse = CommandLine.ParseCommandLine input options
        match parse with
        | Right _ -> NUnit.Framework.Assert.Fail()
        | Left(x, y) ->
          NUnit.Framework.Assert.That(y, Is.SameAs options)
          NUnit.Framework.Assert.That(x, Is.EqualTo "UsageError")
      finally
        Visitor.outputDirectories.Clear()

    [<Test;TestMethod>]
    let ParsingSymbolGivesSymbol() =
      try
        ProgramDatabase.SymbolFolders.Clear()
        let options = Main.DeclareOptions()
        let unique = Path.GetFullPath(".")
        let Symbol = [| "-y"; unique |]
        let parse = CommandLine.ParseCommandLine Symbol options
        match parse with
        | Left _ -> NUnit.Framework.Assert.Fail()
        | Right(x, y) ->
          NUnit.Framework.Assert.That(y, Is.SameAs options)
          NUnit.Framework.Assert.That(x, Is.Empty)
        match ProgramDatabase.SymbolFolders.Count with
        | 1 -> NUnit.Framework.Assert.That(ProgramDatabase.SymbolFolders, Is.EquivalentTo [ unique ])
        | _ -> NUnit.Framework.Assert.Fail()
      finally
        ProgramDatabase.SymbolFolders.Clear()

    [<Test;TestMethod>]
    let ParsingMultipleSymbolGivesOK() =
      try
        ProgramDatabase.SymbolFolders.Clear()
        let options = Main.DeclareOptions()

        let Symbol =
          [| "-y"
             Path.GetFullPath(".")
             "/y"
             Path.GetFullPath("..") |]

        let parse = CommandLine.ParseCommandLine Symbol options
        match parse with
        | Left _ -> NUnit.Framework.Assert.Fail()
        | Right(x, y) ->
          NUnit.Framework.Assert.That(y, Is.SameAs options)
          NUnit.Framework.Assert.That(x, Is.Empty)
        match ProgramDatabase.SymbolFolders.Count with
        | 2 ->
          NUnit.Framework.Assert.That
            (ProgramDatabase.SymbolFolders,
             Is.EquivalentTo(Symbol |> Seq.filter (fun x -> x.Length > 2)))
        | _ -> NUnit.Framework.Assert.Fail()
      finally
        ProgramDatabase.SymbolFolders.Clear()

    [<Test;TestMethod>]
    let ParsingBadSymbolGivesFailure() =
      try
        ProgramDatabase.SymbolFolders.Clear()
        let options = Main.DeclareOptions()
        let unique = Guid.NewGuid().ToString().Replace("-", "*")
        let Symbol = [| "-y"; unique |]
        let parse = CommandLine.ParseCommandLine Symbol options
        match parse with
        | Right _ -> NUnit.Framework.Assert.Fail()
        | Left(x, y) ->
          NUnit.Framework.Assert.That(y, Is.SameAs options)
          NUnit.Framework.Assert.That(x, Is.EqualTo "UsageError")
      finally
        ProgramDatabase.SymbolFolders.Clear()

    [<Test;TestMethod>]
    let ParsingNoSymbolGivesFailure() =
      try
        ProgramDatabase.SymbolFolders.Clear()
        let options = Main.DeclareOptions()
        let Symbol = [| "-y" |]
        let parse = CommandLine.ParseCommandLine Symbol options
        match parse with
        | Right _ -> NUnit.Framework.Assert.Fail()
        | Left(x, y) ->
          NUnit.Framework.Assert.That(y, Is.SameAs options)
          NUnit.Framework.Assert.That(x, Is.EqualTo "UsageError")
      finally
        ProgramDatabase.SymbolFolders.Clear()

    [<Test;TestMethod>]
    let ParsingMultipleDependencyIsOk() =
      try
        Instrument.ResolutionTable.Clear()
        let options = Main.DeclareOptions()
        let here = Assembly.GetExecutingAssembly().Location
        let next = Path.Combine(Path.GetDirectoryName here, "AltCover.Recorder.dll")
        let input = [| "-d"; here; "/d"; next |]
        let parse = CommandLine.ParseCommandLine input options
        match parse with
        | Left _ -> NUnit.Framework.Assert.Fail()
        | Right(x, y) ->
          NUnit.Framework.Assert.That(y, Is.SameAs options)
          NUnit.Framework.Assert.That(x, Is.Empty)
        let expected =
          Instrument.ResolutionTable.Keys
          |> Seq.map (fun a -> Instrument.ResolutionTable.[a].Name.Name)
          |> Seq.sort
        NUnit.Framework.Assert.That
          (String.Join(" ", expected), Is.EqualTo("AltCover.Recorder AltCover.Tests"))
      finally
        Instrument.ResolutionTable.Clear()

    let ParsingNoDependencyGivesFailure() =
      try
        Instrument.ResolutionTable.Clear()
        let options = Main.DeclareOptions()
        let input = [| "-d" |]
        let parse = CommandLine.ParseCommandLine input options
        match parse with
        | Right _ -> NUnit.Framework.Assert.Fail()
        | Left(x, y) ->
          NUnit.Framework.Assert.That(y, Is.SameAs options)
          NUnit.Framework.Assert.That(x, Is.EqualTo "UsageError")
      finally
        Instrument.ResolutionTable.Clear()

    [<Test;TestMethod>]
    let ParsingBadDependencyGivesFailure() =
      try
        Instrument.ResolutionTable.Clear()
        let options = Main.DeclareOptions()
        let unique = Guid.NewGuid().ToString().Replace("-", "*")
        let input = [| "-d"; unique |]
        let parse = CommandLine.ParseCommandLine input options
        match parse with
        | Right _ -> NUnit.Framework.Assert.Fail()
        | Left(x, y) ->
          NUnit.Framework.Assert.That(y, Is.SameAs options)
          NUnit.Framework.Assert.That(x, Is.EqualTo "UsageError")
      finally
        Instrument.ResolutionTable.Clear()

    [<Test;TestMethod>]
    let ParsingNonDependencyGivesFailure() =
      try
        Visitor.defaultStrongNameKey <- None
        Visitor.keys.Clear()
        let options = Main.DeclareOptions()
        let unique = Assembly.GetExecutingAssembly().Location + ".txt"
        let input = [| "-d"; unique |]
        let parse = CommandLine.ParseCommandLine input options
        match parse with
        | Right _ -> NUnit.Framework.Assert.Fail()
        | Left(x, y) ->
          NUnit.Framework.Assert.That(y, Is.SameAs options)
          NUnit.Framework.Assert.That(x, Is.EqualTo "UsageError")
      finally
        Visitor.defaultStrongNameKey <- None
        Visitor.keys.Clear()

    [<Test;TestMethod>]
    let ParsingStrongNameGivesStrongName() =
      try
        Visitor.defaultStrongNameKey <- None
        Visitor.keys.Clear()
        let options = Main.DeclareOptions ()
        let input = [| "-sn"; Path.Combine(SolutionRoot.location, "Build/Infrastructure.snk") |]
        let parse = CommandLine.ParseCommandLine input options
        match parse with
        | Left _ -> NUnit.Framework.Assert.Fail()
        | Right (x, y) -> NUnit.Framework.Assert.That (y, Is.SameAs options)
                          NUnit.Framework.Assert.That (x, Is.Empty)
        match Visitor.defaultStrongNameKey with
        | None -> NUnit.Framework.Assert.Fail()
        | Some x -> let token = x
                                |> KeyStore.TokenOfKey
                                |> List.map (fun x -> x.ToString("x2"))
                    NUnit.Framework.Assert.That (String.Join (String.Empty, token), Is.EqualTo("c02b1a9f5b7cade8"))
      finally
        Visitor.defaultStrongNameKey <- None
        Visitor.keys.Clear()

    [<Test;TestMethod>]
    let ParsingMultipleStrongNameGivesFailure() =
      try
        Visitor.defaultStrongNameKey <- None
        Visitor.keys.Clear()
        let options = Main.DeclareOptions ()
        let path = SolutionRoot.location
        let input = [| "-sn"; Path.Combine(path, "Build/Infrastructure.snk") ;
                       "/sn"; Path.Combine(path, "Build/Recorder.snk") |]
        let parse = CommandLine.ParseCommandLine input options
        match parse with
        | Right _ -> NUnit.Framework.Assert.Fail()
        | Left (x, y) -> NUnit.Framework.Assert.That (y, Is.SameAs options)
                         NUnit.Framework.Assert.That (x, Is.EqualTo "UsageError")
                         NUnit.Framework.Assert.That(CommandLine.error |> Seq.head, Is.EqualTo "--strongNameKey : specify this only once")
      finally
        Visitor.defaultStrongNameKey <- None
        Visitor.keys.Clear()

    [<Test;TestMethod>]
    let ParsingBadStrongNameGivesFailure() =
      try
        Visitor.defaultStrongNameKey <- None
        Visitor.keys.Clear()
        let options = Main.DeclareOptions ()
        let unique = Guid.NewGuid().ToString().Replace("-", "*")
        let input = [| "-sn"; unique |]
        let parse = CommandLine.ParseCommandLine input options
        match parse with
        | Right _ -> NUnit.Framework.Assert.Fail()
        | Left (x, y) -> NUnit.Framework.Assert.That (y, Is.SameAs options)
                         NUnit.Framework.Assert.That (x, Is.EqualTo "UsageError")
      finally
        Visitor.defaultStrongNameKey <- None
        Visitor.keys.Clear()

    [<Test;TestMethod>]
    let ParsingNonStrongNameGivesFailure() =
      try
        Visitor.defaultStrongNameKey <- None
        Visitor.keys.Clear()
        let options = Main.DeclareOptions ()
        let unique = Assembly.GetExecutingAssembly().Location
        let input = [| "-sn"; unique |]
        let parse = CommandLine.ParseCommandLine input options
        match parse with
        | Right _ -> NUnit.Framework.Assert.Fail()
        | Left (x, y) -> NUnit.Framework.Assert.That (y, Is.SameAs options)
                         NUnit.Framework.Assert.That (x, Is.EqualTo "UsageError")
      finally
        Visitor.defaultStrongNameKey <- None
        Visitor.keys.Clear()

    [<Test;TestMethod>]
    let ParsingNoStrongNameGivesFailure() =
      try
        Visitor.defaultStrongNameKey <- None
        Visitor.keys.Clear()
        let options = Main.DeclareOptions ()
        let input = [| "-sn" |]
        let parse = CommandLine.ParseCommandLine input options
        match parse with
        | Right _ -> NUnit.Framework.Assert.Fail()
        | Left (x, y) -> NUnit.Framework.Assert.That (y, Is.SameAs options)
                         NUnit.Framework.Assert.That (x, Is.EqualTo "UsageError")
      finally
        Visitor.defaultStrongNameKey <- None
        Visitor.keys.Clear()

    [<Test;TestMethod>]
    let ParsingMultipleAltStrongNameIsOk() =
      try
        Visitor.defaultStrongNameKey <- None
        Visitor.keys.Clear()
        let options = Main.DeclareOptions ()
        let path = SolutionRoot.location
        let input = [| "-k"; Path.Combine(path, "Build/Infrastructure.snk");
                       "/k"; Path.Combine(path, "Build/Recorder.snk") |]
        let parse = CommandLine.ParseCommandLine input options
        match parse with
        | Left _ -> NUnit.Framework.Assert.Fail()
        | Right (x, y) -> NUnit.Framework.Assert.That (y, Is.SameAs options)
                          NUnit.Framework.Assert.That (x, Is.Empty)
        let expected = Visitor.keys.Keys
                       |> Seq.map (fun x -> String.Join(String.Empty,
                                                        BitConverter.GetBytes(x)
                                                        |> Seq.map (fun x -> x.ToString("x2"))))
                       |> Seq.sort
        NUnit.Framework.Assert.That (String.Join(" ", expected), Is.EqualTo ("4ebffcaabf10ce6a c02b1a9f5b7cade8"))
      finally
        Visitor.defaultStrongNameKey <- None
        Visitor.keys.Clear()

    [<Test;TestMethod>]
    let ParsingNoAltStrongNameGivesFailure() =
      try
        Visitor.defaultStrongNameKey <- None
        Visitor.keys.Clear()
        let options = Main.DeclareOptions ()
        let input = [| "-k" |]
        let parse = CommandLine.ParseCommandLine input options
        match parse with
        | Right _ -> NUnit.Framework.Assert.Fail()
        | Left (x, y) -> NUnit.Framework.Assert.That (y, Is.SameAs options)
                         NUnit.Framework.Assert.That (x, Is.EqualTo "UsageError")
      finally
      Visitor.defaultStrongNameKey <- None
      Visitor.keys.Clear()

    [<Test;TestMethod>]
    let ParsingBadAltStrongNameGivesFailure() =
      try
        Visitor.defaultStrongNameKey <- None
        Visitor.keys.Clear()
        let options = Main.DeclareOptions ()
        let unique = Guid.NewGuid().ToString().Replace("-", "*")
        let input = [| "-k"; unique |]
        let parse = CommandLine.ParseCommandLine input options
        match parse with
        | Right _ -> NUnit.Framework.Assert.Fail()
        | Left (x, y) -> NUnit.Framework.Assert.That (y, Is.SameAs options)
                         NUnit.Framework.Assert.That (x, Is.EqualTo "UsageError")
      finally
        Visitor.defaultStrongNameKey <- None
        Visitor.keys.Clear()

    [<Test;TestMethod>]
    let ParsingNonAltsStrongNameGivesFailure() =
      try
        Visitor.defaultStrongNameKey <- None
        Visitor.keys.Clear()
        let options = Main.DeclareOptions ()
        let unique = Assembly.GetExecutingAssembly().Location
        let input = [| "-k"; unique |]
        let parse = CommandLine.ParseCommandLine input options
        match parse with
        | Right _ -> NUnit.Framework.Assert.Fail()
        | Left (x, y) -> NUnit.Framework.Assert.That (y, Is.SameAs options)
                         NUnit.Framework.Assert.That (x, Is.EqualTo "UsageError")
      finally
        Visitor.defaultStrongNameKey <- None
        Visitor.keys.Clear()

    [<Test;TestMethod>]
    let ParsingLocalGivesLocal() =
      try
        Visitor.local := false
        let options = Main.DeclareOptions()
        let input = [| "--localSource" |]
        let parse = CommandLine.ParseCommandLine input options
        match parse with
        | Left _ -> NUnit.Framework.Assert.Fail()
        | Right(x, y) ->
          NUnit.Framework.Assert.That(y, Is.SameAs options)
          NUnit.Framework.Assert.That(x, Is.Empty)
        NUnit.Framework.Assert.That(!Visitor.local, Is.True)
      finally
        Visitor.local := false

    [<Test;TestMethod>]
    let ParsingMultipleLocalGivesFailure() =
      try
        Visitor.local := false
        let options = Main.DeclareOptions()
        let input = [| "-l"; "--localSource" |]
        let parse = CommandLine.ParseCommandLine input options
        match parse with
        | Right _ -> NUnit.Framework.Assert.Fail()
        | Left(x, y) ->
          NUnit.Framework.Assert.That(y, Is.SameAs options)
          NUnit.Framework.Assert.That(x, Is.EqualTo "UsageError")
          NUnit.Framework.Assert.That(CommandLine.error |> Seq.head, Is.EqualTo "--localSource : specify this only once")
      finally
        Visitor.local := false

    [<Test;TestMethod>]
    let ParsingVisibleGivesVisible() =
      try
        Visitor.coalesceBranches := false
        let options = Main.DeclareOptions()
        let input = [| "--visibleBranches" |]
        let parse = CommandLine.ParseCommandLine input options
        match parse with
        | Left _ -> NUnit.Framework.Assert.Fail()
        | Right(x, y) ->
          NUnit.Framework.Assert.That(y, Is.SameAs options)
          NUnit.Framework.Assert.That(x, Is.Empty)
        NUnit.Framework.Assert.That(!Visitor.coalesceBranches, Is.True)
      finally
        Visitor.coalesceBranches := false

    [<Test;TestMethod>]
    let ParsingMultipleVisibleGivesFailure() =
      try
        Visitor.coalesceBranches := false
        let options = Main.DeclareOptions()
        let input = [| "-v"; "--visibleBranches" |]
        let parse = CommandLine.ParseCommandLine input options
        match parse with
        | Right _ -> NUnit.Framework.Assert.Fail()
        | Left(x, y) ->
          NUnit.Framework.Assert.That(y, Is.SameAs options)
          NUnit.Framework.Assert.That(x, Is.EqualTo "UsageError")
          NUnit.Framework.Assert.That(CommandLine.error |> Seq.head, Is.EqualTo "--visibleBranches : specify this only once")
      finally
        Visitor.coalesceBranches := false

    [<Test;TestMethod>]
    let ParsingTimeGivesTime() =
      try
        Visitor.TrackingNames.Clear()
        Visitor.interval <- None
        let options = Main.DeclareOptions()
        let input = [| "-c"; "5" |]
        let parse = CommandLine.ParseCommandLine input options
        match parse with
        | Left _ -> NUnit.Framework.Assert.Fail()
        | Right(x, y) ->
          NUnit.Framework.Assert.That(y, Is.SameAs options)
          NUnit.Framework.Assert.That(x, Is.Empty)
        NUnit.Framework.Assert.That(Visitor.Interval(), Is.EqualTo 100)
      finally
        Visitor.interval <- None
        Visitor.TrackingNames.Clear()

    [<Test;TestMethod>]
    let ParsingOnlyArabicNumeralsNotThatSortofArabicNumeralsGivesTime() =
      try
        Visitor.TrackingNames.Clear()
        Visitor.interval <- None
        let options = Main.DeclareOptions()
        let input = [| "-c"; "٣" |]
        let parse = CommandLine.ParseCommandLine input options
        match parse with
        | Right _ -> NUnit.Framework.Assert.Fail()
        | Left(x, y) ->
          NUnit.Framework.Assert.That(y, Is.SameAs options)
          NUnit.Framework.Assert.That(x, Is.EqualTo "UsageError")
        NUnit.Framework.Assert.That(Visitor.Interval(), Is.EqualTo 0)
      finally
        Visitor.interval <- None
        Visitor.TrackingNames.Clear()

    [<Test;TestMethod>]
    let ParsingMultipleTimesGivesFailure() =
      try
        Visitor.interval <- None
        Visitor.TrackingNames.Clear()
        let options = Main.DeclareOptions()
        let path = SolutionRoot.location
        let input = [| "-c"; "3"; "/c"; "5" |]
        let parse = CommandLine.ParseCommandLine input options
        match parse with
        | Right _ -> NUnit.Framework.Assert.Fail()
        | Left(x, y) ->
          NUnit.Framework.Assert.That(y, Is.SameAs options)
          NUnit.Framework.Assert.That(x, Is.EqualTo "UsageError")
          NUnit.Framework.Assert.That(Visitor.Interval(), Is.EqualTo 10000)
          NUnit.Framework.Assert.That(CommandLine.error |> Seq.head, Is.EqualTo "--callContext : specify this only once")
      finally
        Visitor.interval <- None
        Visitor.TrackingNames.Clear()

    [<Test;TestMethod>]
    let ParsingTimeAndNamesGivesOK() =
      try
        Visitor.interval <- None
        Visitor.TrackingNames.Clear()
        let options = Main.DeclareOptions()
        let path = SolutionRoot.location
        let input = [| "-c"; "3"; "/c"; "x"; "--callContext"; "Hello, World!" |]
        let parse = CommandLine.ParseCommandLine input options
        match parse with
        | Left _ -> NUnit.Framework.Assert.Fail()
        | Right(x, y) ->
          NUnit.Framework.Assert.That(y, Is.SameAs options)
          NUnit.Framework.Assert.That(x, Is.Empty)
        NUnit.Framework.Assert.That(Visitor.Interval(), Is.EqualTo 10000)
        NUnit.Framework.Assert.That(Visitor.TrackingNames, Is.EquivalentTo [ "x"; "Hello, World!" ])
      finally
        Visitor.interval <- None
        Visitor.TrackingNames.Clear()

    [<Test;TestMethod>]
    let ParsingBadTimeGivesNoOp() =
      try
        Visitor.interval <- None
        Visitor.TrackingNames.Clear()
        let options = Main.DeclareOptions()
        let unique = Guid.NewGuid().ToString().Replace("-", "*")
        let input = [| "-c"; "9" |]
        let parse = CommandLine.ParseCommandLine input options
        match parse with
        | Left _ -> NUnit.Framework.Assert.Fail()
        | Right(x, y) ->
          NUnit.Framework.Assert.That(y, Is.SameAs options)
          NUnit.Framework.Assert.That(x, Is.Empty)
        NUnit.Framework.Assert.That(Visitor.Interval(), Is.EqualTo 0)
      finally
        Visitor.interval <- None
        Visitor.TrackingNames.Clear()

    [<Test;TestMethod>]
    let ParsingNonTimeGivesFailure() = //TODO
      try
        Visitor.interval <- None
        Visitor.TrackingNames.Clear()
        let options = Main.DeclareOptions()
        let unique = Assembly.GetExecutingAssembly().Location
        let input = [| "-c"; "99" |]
        let parse = CommandLine.ParseCommandLine input options
        match parse with
        | Right _ -> NUnit.Framework.Assert.Fail()
        | Left(x, y) ->
          NUnit.Framework.Assert.That(y, Is.SameAs options)
          NUnit.Framework.Assert.That(x, Is.EqualTo "UsageError")
      finally
        Visitor.interval <- None
        Visitor.TrackingNames.Clear()

    [<Test;TestMethod>]
    let ParsingNoTimeGivesFailure() =
      try
        Visitor.interval <- None
        Visitor.TrackingNames.Clear()
        let options = Main.DeclareOptions()
        let input = [| "-c"; " " |]
        let parse = CommandLine.ParseCommandLine input options
        match parse with
        | Right _ -> NUnit.Framework.Assert.Fail()
        | Left(x, y) ->
          NUnit.Framework.Assert.That(y, Is.SameAs options)
          NUnit.Framework.Assert.That(x, Is.EqualTo "UsageError")
      finally
        Visitor.interval <- None
        Visitor.TrackingNames.Clear()

    [<Test;TestMethod>]
    let ParsingAfterSingleGivesFailure() =
      try
        Visitor.single <- true
        Visitor.interval <- None
        Visitor.TrackingNames.Clear()
        let options = Main.DeclareOptions()
        let input = [| "-c"; "3"; "/c"; "x"; "--callContext"; "Hello, World!" |]
        let parse = CommandLine.ParseCommandLine input options
        match parse with
        | Right _ -> NUnit.Framework.Assert.Fail()
        | Left(x, y) ->
          NUnit.Framework.Assert.That(y, Is.SameAs options)
          NUnit.Framework.Assert.That(x, Is.EqualTo "UsageError")
      finally
        Visitor.single <- false
        Visitor.interval <- None
        Visitor.TrackingNames.Clear()

    [<Test;TestMethod>]
    let ParsingOpenCoverGivesOpenCover() =
      try
        Visitor.reportFormat <- None
        let options = Main.DeclareOptions()
        let input = [| "--opencover" |]
        let parse = CommandLine.ParseCommandLine input options
        match parse with
        | Left _ -> NUnit.Framework.Assert.Fail()
        | Right(x, y) ->
          NUnit.Framework.Assert.That(y, Is.SameAs options)
          NUnit.Framework.Assert.That(x, Is.Empty)
        match Visitor.reportFormat with
        | None -> NUnit.Framework.Assert.Fail()
        | Some x -> NUnit.Framework.Assert.That(x, Is.EqualTo AltCover.Base.ReportFormat.OpenCover)
      finally
        Visitor.reportFormat <- None

    [<Test;TestMethod>]
    let ParsingMultipleOpenCoverGivesFailure() =
      try
        Visitor.reportFormat <- None
        let options = Main.DeclareOptions()
        let input = [| "--opencover"; "--opencover" |]
        let parse = CommandLine.ParseCommandLine input options
        match parse with
        | Right _ -> NUnit.Framework.Assert.Fail()
        | Left(x, y) ->
          NUnit.Framework.Assert.That(y, Is.SameAs options)
          NUnit.Framework.Assert.That(x, Is.EqualTo "UsageError")
          NUnit.Framework.Assert.That(CommandLine.error |> Seq.head, Is.EqualTo "--opencover : specify this only once")
      finally
        Visitor.reportFormat <- None

    [<Test;TestMethod>]
    let ParsingInPlaceGivesInPlace() =
      try
        Visitor.inplace := false
        let options = Main.DeclareOptions()
        let input = [| "--inplace" |]
        let parse = CommandLine.ParseCommandLine input options
        match parse with
        | Left _ -> NUnit.Framework.Assert.Fail()
        | Right(x, y) ->
          NUnit.Framework.Assert.That(y, Is.SameAs options)
          NUnit.Framework.Assert.That(x, Is.Empty)
        NUnit.Framework.Assert.That(!Visitor.inplace, Is.True)
      finally
        Visitor.inplace := false

    [<Test;TestMethod>]
    let ParsingMultipleInPlaceGivesFailure() =
      try
        Visitor.inplace := false
        let options = Main.DeclareOptions()
        let input = [| "--inplace"; "--inplace" |]
        let parse = CommandLine.ParseCommandLine input options
        match parse with
        | Right _ -> NUnit.Framework.Assert.Fail()
        | Left(x, y) ->
          NUnit.Framework.Assert.That(y, Is.SameAs options)
          NUnit.Framework.Assert.That(x, Is.EqualTo "UsageError")
          NUnit.Framework.Assert.That(CommandLine.error |> Seq.head, Is.EqualTo "--inplace : specify this only once")
      finally
        Visitor.inplace := false

    [<Test;TestMethod>]
    let ParsingSaveGivesSave() =
      try
        Visitor.collect := false
        let options = Main.DeclareOptions()
        let input = [| "--save" |]
        let parse = CommandLine.ParseCommandLine input options
        match parse with
        | Left _ -> NUnit.Framework.Assert.Fail()
        | Right(x, y) ->
          NUnit.Framework.Assert.That(y, Is.SameAs options)
          NUnit.Framework.Assert.That(x, Is.Empty)
          NUnit.Framework.Assert.That(!Visitor.collect, Is.True)
      finally
        Visitor.collect := false

    [<Test;TestMethod>]
    let ParsingMultipleSaveGivesFailure() =
      try
        Visitor.collect := false
        let options = Main.DeclareOptions()
        let input = [| "--save"; "--save" |]
        let parse = CommandLine.ParseCommandLine input options
        match parse with
        | Right _ -> NUnit.Framework.Assert.Fail()
        | Left(x, y) ->
          NUnit.Framework.Assert.That(y, Is.SameAs options)
          NUnit.Framework.Assert.That(x, Is.EqualTo "UsageError")
          NUnit.Framework.Assert.That(CommandLine.error |> Seq.head, Is.EqualTo "--save : specify this only once")
      finally
        Visitor.collect := false

    [<Test;TestMethod>]
    let ParsingSingleGivesSingle() =
      try
        Visitor.single <- false
        let options = Main.DeclareOptions()
        let input = [| "--single" |]
        let parse = CommandLine.ParseCommandLine input options
        match parse with
        | Left _ -> NUnit.Framework.Assert.Fail()
        | Right(x, y) ->
          NUnit.Framework.Assert.That(y, Is.SameAs options)
          NUnit.Framework.Assert.That(x, Is.Empty)
        NUnit.Framework.Assert.That(Visitor.single, Is.True)
      finally
        Visitor.single <- false

    [<Test;TestMethod>]
    let ParsingMultipleSingleGivesFailure() =
      try
        Visitor.single <- false
        let options = Main.DeclareOptions()
        let input = [| "--single"; "--single" |]
        let parse = CommandLine.ParseCommandLine input options
        match parse with
        | Right _ -> NUnit.Framework.Assert.Fail()
        | Left(x, y) ->
          NUnit.Framework.Assert.That(y, Is.SameAs options)
          NUnit.Framework.Assert.That(x, Is.EqualTo "UsageError")
          NUnit.Framework.Assert.That(CommandLine.error |> Seq.head, Is.EqualTo "--single : specify this only once")
      finally
        Visitor.single <- false

    [<Test;TestMethod>]
    let ParsingSingleAfterContextGivesFailure() =
      try
        Visitor.single <- false
        Visitor.interval <- Some 0
        let options = Main.DeclareOptions()
        let input = [| "--single" |]
        let parse = CommandLine.ParseCommandLine input options
        match parse with
        | Right _ -> NUnit.Framework.Assert.Fail()
        | Left(x, y) ->
          NUnit.Framework.Assert.That(y, Is.SameAs options)
          NUnit.Framework.Assert.That(x, Is.EqualTo "UsageError")
      finally
        Visitor.single <- false
        Visitor.interval <- None

    [<Test;TestMethod>]
    let ParsingLineCoverGivesLineCover() =
      try
        Visitor.coverstyle <- CoverStyle.All
        let options = Main.DeclareOptions()
        let input = [| "--linecover" |]
        let parse = CommandLine.ParseCommandLine input options
        match parse with
        | Left _ -> NUnit.Framework.Assert.Fail()
        | Right(x, y) ->
          NUnit.Framework.Assert.That(y, Is.SameAs options)
          NUnit.Framework.Assert.That(x, Is.Empty)
        NUnit.Framework.Assert.That(Visitor.coverstyle, Is.EqualTo CoverStyle.LineOnly)
        match Visitor.reportFormat with
        | None -> NUnit.Framework.Assert.Pass()
        | Some x -> NUnit.Framework.Assert.Fail()
      finally
        Visitor.coverstyle <- CoverStyle.All

    [<Test;TestMethod>]
    let OpenCoverIsCompatibleWithLineCover() =
      try
        Visitor.coverstyle <- CoverStyle.All
        Visitor.reportFormat <- None
        let options = Main.DeclareOptions()
        let input = [| "--linecover"; "--opencover" |]
        let parse = CommandLine.ParseCommandLine input options
        match parse with
        | Left _ -> NUnit.Framework.Assert.Fail()
        | Right(x, y) ->
          NUnit.Framework.Assert.That(y, Is.SameAs options)
          NUnit.Framework.Assert.That(x, Is.Empty)
        NUnit.Framework.Assert.That(Visitor.coverstyle, Is.EqualTo CoverStyle.LineOnly)
        match Visitor.reportFormat with
        | None -> NUnit.Framework.Assert.Fail()
        | Some x -> NUnit.Framework.Assert.That(x, Is.EqualTo AltCover.Base.ReportFormat.OpenCover)
      finally
        Visitor.reportFormat <- None
        Visitor.coverstyle <- CoverStyle.All

    [<Test;TestMethod>]
    let LineCoverIsCompatibleWithOpenCover() =
      try
        Visitor.coverstyle <- CoverStyle.All
        Visitor.reportFormat <- None
        let options = Main.DeclareOptions()
        let input = [| "--opencover"; "--linecover" |]
        let parse = CommandLine.ParseCommandLine input options
        match parse with
        | Left _ -> NUnit.Framework.Assert.Fail()
        | Right(x, y) ->
          NUnit.Framework.Assert.That(y, Is.SameAs options)
          NUnit.Framework.Assert.That(x, Is.Empty)
        NUnit.Framework.Assert.That(Visitor.coverstyle, Is.EqualTo CoverStyle.LineOnly)
        match Visitor.reportFormat with
        | None -> NUnit.Framework.Assert.Fail()
        | Some x -> NUnit.Framework.Assert.That(x, Is.EqualTo AltCover.Base.ReportFormat.OpenCover)
      finally
        Visitor.reportFormat <- None
        Visitor.coverstyle <- CoverStyle.All

    [<Test;TestMethod>]
    let ParsingMultipleLineCoverGivesFailure() =
      try
        Visitor.coverstyle <- CoverStyle.All
        let options = Main.DeclareOptions()
        let input = [| "--linecover"; "--linecover" |]
        let parse = CommandLine.ParseCommandLine input options
        match parse with
        | Right _ -> NUnit.Framework.Assert.Fail()
        | Left(x, y) ->
          NUnit.Framework.Assert.That(y, Is.SameAs options)
          NUnit.Framework.Assert.That(x, Is.EqualTo "UsageError")
          NUnit.Framework.Assert.That(CommandLine.error |> Seq.head, Is.EqualTo "--linecover : specify this only once")
      finally
        Visitor.coverstyle <- CoverStyle.All

    [<Test;TestMethod>]
    let LineCoverIsNotCompatibleWithBranchCover() =
      try
        Visitor.coverstyle <- CoverStyle.All
        let options = Main.DeclareOptions()
        let input = [| "--linecover"; "--branchcover" |]
        let parse = CommandLine.ParseCommandLine input options
        match parse with
        | Right _ -> NUnit.Framework.Assert.Fail()
        | Left(x, y) ->
          NUnit.Framework.Assert.That(y, Is.SameAs options)
          NUnit.Framework.Assert.That(x, Is.EqualTo "UsageError")
      finally
        Visitor.coverstyle <- CoverStyle.All

    [<Test;TestMethod>]
    let ParsingBranchCoverGivesBranchCover() =
      try
        Visitor.coverstyle <- CoverStyle.All
        let options = Main.DeclareOptions()
        let input = [| "--branchcover" |]
        let parse = CommandLine.ParseCommandLine input options
        match parse with
        | Left _ -> NUnit.Framework.Assert.Fail()
        | Right(x, y) ->
          NUnit.Framework.Assert.That(y, Is.SameAs options)
          NUnit.Framework.Assert.That(x, Is.Empty)
        NUnit.Framework.Assert.That(Visitor.coverstyle, Is.EqualTo CoverStyle.BranchOnly)
        match Visitor.reportFormat with
        | None -> NUnit.Framework.Assert.Pass()
        | Some x -> NUnit.Framework.Assert.Fail()
      finally
        Visitor.coverstyle <- CoverStyle.All

    [<Test;TestMethod>]
    let OpenCoverIsCompatibleWithBranchCover() =
      try
        Visitor.reportFormat <- None
        Visitor.coverstyle <- CoverStyle.All
        let options = Main.DeclareOptions()
        let input = [| "--branchcover"; "--opencover" |]
        let parse = CommandLine.ParseCommandLine input options
        match parse with
        | Left _ -> NUnit.Framework.Assert.Fail()
        | Right(x, y) ->
          NUnit.Framework.Assert.That(y, Is.SameAs options)
          NUnit.Framework.Assert.That(x, Is.Empty)
        NUnit.Framework.Assert.That(Visitor.coverstyle, Is.EqualTo CoverStyle.BranchOnly)
        match Visitor.reportFormat with
        | None -> NUnit.Framework.Assert.Fail()
        | Some x -> NUnit.Framework.Assert.That(x, Is.EqualTo AltCover.Base.ReportFormat.OpenCover)
      finally
        Visitor.reportFormat <- None
        Visitor.coverstyle <- CoverStyle.All

    [<Test;TestMethod>]
    let BranchCoverIsCompatibleWithOpenCover() =
      try
        Visitor.reportFormat <- None
        Visitor.coverstyle <- CoverStyle.All
        let options = Main.DeclareOptions()
        let input = [| "--opencover"; "--branchcover" |]
        let parse = CommandLine.ParseCommandLine input options
        match parse with
        | Left _ -> NUnit.Framework.Assert.Fail()
        | Right(x, y) ->
          NUnit.Framework.Assert.That(y, Is.SameAs options)
          NUnit.Framework.Assert.That(x, Is.Empty)
        NUnit.Framework.Assert.That(Visitor.coverstyle, Is.EqualTo CoverStyle.BranchOnly)
        match Visitor.reportFormat with
        | None -> NUnit.Framework.Assert.Fail()
        | Some x -> NUnit.Framework.Assert.That(x, Is.EqualTo AltCover.Base.ReportFormat.OpenCover)
      finally
        Visitor.coverstyle <- CoverStyle.All
        Visitor.reportFormat <- None

    [<Test;TestMethod>]
    let ParsingMultipleBranchCoverGivesFailure() =
      try
        Visitor.coverstyle <- CoverStyle.All
        let options = Main.DeclareOptions()
        let input = [| "--branchcover"; "--branchcover" |]
        let parse = CommandLine.ParseCommandLine input options
        match parse with
        | Right _ -> NUnit.Framework.Assert.Fail()
        | Left(x, y) ->
          NUnit.Framework.Assert.That(y, Is.SameAs options)
          NUnit.Framework.Assert.That(x, Is.EqualTo "UsageError")
          NUnit.Framework.Assert.That(CommandLine.error |> Seq.head, Is.EqualTo "--branchcover : specify this only once")
      finally
        Visitor.coverstyle <- CoverStyle.All

    [<Test;TestMethod>]
    let BranchCoverIsNotCompatibleWithLineCover() =
      try
        Visitor.coverstyle <- CoverStyle.All
        let options = Main.DeclareOptions()
        let input = [| "--branchcover"; "--linecover" |]
        let parse = CommandLine.ParseCommandLine input options
        match parse with
        | Right _ -> NUnit.Framework.Assert.Fail()
        | Left(x, y) ->
          NUnit.Framework.Assert.That(y, Is.SameAs options)
          NUnit.Framework.Assert.That(x, Is.EqualTo "UsageError")
      finally
        Visitor.coverstyle <- CoverStyle.All

    [<Test;TestMethod>]
    let ParsingDropGivesDrop() =
      try
        CommandLine.dropReturnCode := false
        let options = Main.DeclareOptions()
        let input = [| "--dropReturnCode" |]
        let parse = CommandLine.ParseCommandLine input options
        match parse with
        | Left _ -> NUnit.Framework.Assert.Fail()
        | Right(x, y) ->
          NUnit.Framework.Assert.That(y, Is.SameAs options)
          NUnit.Framework.Assert.That(x, Is.Empty)
        NUnit.Framework.Assert.That(!CommandLine.dropReturnCode, Is.True)
      finally
        CommandLine.dropReturnCode := false

    [<Test;TestMethod>]
    let ParsingMultipleDropGivesFailure() =
      try
        CommandLine.dropReturnCode := false
        let options = Main.DeclareOptions()
        let input = [| "--dropReturnCode"; "--dropReturnCode" |]
        let parse = CommandLine.ParseCommandLine input options
        match parse with
        | Right _ -> NUnit.Framework.Assert.Fail()
        | Left(x, y) ->
          NUnit.Framework.Assert.That(y, Is.SameAs options)
          NUnit.Framework.Assert.That(x, Is.EqualTo "UsageError")
          NUnit.Framework.Assert.That(CommandLine.error |> Seq.head, Is.EqualTo "--dropReturnCode : specify this only once")
      finally
        CommandLine.dropReturnCode := false

    [<Test;TestMethod>]
    let ParsingDeferWorks() =
      try
        Visitor.defer := None
        let options = Main.DeclareOptions()
        let input = [| "--defer" |]
        let parse = CommandLine.ParseCommandLine input options
        match parse with
        | Left _ -> NUnit.Framework.Assert.Fail()
        | Right(x, y) ->
          NUnit.Framework.Assert.That(y, Is.SameAs options)
          NUnit.Framework.Assert.That(x, Is.Empty)
        NUnit.Framework.Assert.That(Option.isSome !Visitor.defer)
        NUnit.Framework.Assert.That(Option.get !Visitor.defer)
        NUnit.Framework.Assert.That(Visitor.deferOpCode(), Is.EqualTo OpCodes.Ldc_I4_1)
      finally
        Visitor.defer := None

    [<Test;TestMethod>]
    let ParsingDeferPlusWorks() =
      try
        Visitor.defer := None
        let options = Main.DeclareOptions()
        let input = [| "--defer:+" |]
        let parse = CommandLine.ParseCommandLine input options
        match parse with
        | Left _ -> NUnit.Framework.Assert.Fail()
        | Right(x, y) ->
          NUnit.Framework.Assert.That(y, Is.SameAs options)
          NUnit.Framework.Assert.That(x, Is.Empty)
        NUnit.Framework.Assert.That(Option.isSome !Visitor.defer)
        NUnit.Framework.Assert.That(Option.get !Visitor.defer)
        NUnit.Framework.Assert.That(Visitor.deferOpCode(), Is.EqualTo OpCodes.Ldc_I4_1)
      finally
        Visitor.defer := None

    [<Test;TestMethod>]
    let ParsingDeferMinusWorks() =
      try
        Visitor.defer := None
        let options = Main.DeclareOptions()
        let input = [| "--defer:-" |]
        let parse = CommandLine.ParseCommandLine input options
        match parse with
        | Left _ -> NUnit.Framework.Assert.Fail()
        | Right(x, y) ->
          NUnit.Framework.Assert.That(y, Is.SameAs options)
          NUnit.Framework.Assert.That(x, Is.Empty)
        NUnit.Framework.Assert.That(Option.isSome !Visitor.defer)
        NUnit.Framework.Assert.That(Option.get !Visitor.defer, Is.False)
        NUnit.Framework.Assert.That(Visitor.deferOpCode(), Is.EqualTo OpCodes.Ldc_I4_0)
      finally
        Visitor.defer := None

    [<Test;TestMethod>]
    let ParsingDeferJunkGivesFailure() =
      try
        Visitor.defer := None
        let options = Main.DeclareOptions()
        let input = [| "--defer:junk" |]
        let parse = CommandLine.ParseCommandLine input options
        match parse with
        | Right _ -> NUnit.Framework.Assert.Fail()
        | Left(x, y) ->
          NUnit.Framework.Assert.That(y, Is.SameAs options)
          NUnit.Framework.Assert.That(x, Is.EqualTo "UsageError")
      finally
        Visitor.defer := None

    [<Test;TestMethod>]
    let ParsingMultipleDeferGivesFailure() =
      try
        Visitor.defer := None
        let options = Main.DeclareOptions()
        let input = [| "--defer"; "--defer" |]
        let parse = CommandLine.ParseCommandLine input options
        match parse with
        | Right _ -> NUnit.Framework.Assert.Fail()
        | Left(x, y) ->
          NUnit.Framework.Assert.That(y, Is.SameAs options)
          NUnit.Framework.Assert.That(x, Is.EqualTo "UsageError")
          NUnit.Framework.Assert.That(CommandLine.error |> Seq.head, Is.EqualTo "--defer : specify this only once")

      finally
        Visitor.defer := None

    [<Test;TestMethod>]
    let OutputLeftPassesThrough() =
      let arg = (Guid.NewGuid().ToString(), Main.DeclareOptions())
      let fail = Left arg
      match Main.ProcessOutputLocation fail with
      | Right _ -> NUnit.Framework.Assert.Fail()
      | Left x -> NUnit.Framework.Assert.That(x, Is.SameAs arg)

    [<Test;TestMethod>]
    let OutputInPlaceFails() =
      let options = Main.DeclareOptions()
      let saved = (Console.Out, Console.Error)
      try
        use stdout = new StringWriter()
        use stderr = new StringWriter()
        Console.SetOut stdout
        Console.SetError stderr
        let here = Path.GetDirectoryName (Assembly.GetExecutingAssembly().Location)
        Visitor.inputDirectories.Clear()
        Visitor.inputDirectories.Add here
        Visitor.outputDirectories.Clear()
        Visitor.outputDirectories.AddRange Visitor.inputDirectories
        let arg = ([], options)
        let fail = Right arg
        match Main.ProcessOutputLocation fail with
        | Right _ -> NUnit.Framework.Assert.Fail()
        | Left(x, y) ->
          NUnit.Framework.Assert.That(y, Is.SameAs options)
          NUnit.Framework.Assert.That(x, Is.EqualTo "UsageError")
          NUnit.Framework.Assert.That(stderr.ToString(), Is.Empty)
          NUnit.Framework.Assert.That
            (CommandLine.error,
             Is.EquivalentTo [ "From and to directories " +
                               here + " are identical" ])
          NUnit.Framework.Assert.That(stdout.ToString(), Is.Empty)
      finally
        Console.SetOut(fst saved)
        Console.SetError(snd saved)

    [<Test;TestMethod>]
    let OutputToNewPlaceIsOK() =
      let options = Main.DeclareOptions()
      let saved = (Console.Out, Console.Error)
      AltCover.ToConsole()
      CommandLine.error <- []
      try
        use stdout = new StringWriter()
        use stderr = new StringWriter()
        Console.SetOut stdout
        Console.SetError stderr
        let here = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
        Visitor.inputDirectories.Clear()
        Visitor.inputDirectories.Add here
        Visitor.outputDirectories.Clear()
        Visitor.outputDirectories.Add (Path.GetDirectoryName here)
        let rest = [ Guid.NewGuid().ToString() ]
        let arg = (rest, options)
        let ok = Right arg
        match Main.ProcessOutputLocation ok with
        | Left _ -> NUnit.Framework.Assert.Fail()
        | Right(x, y, z, t) ->
          NUnit.Framework.Assert.That(x, Is.SameAs rest)
          y |> Seq.iter (fun y' -> NUnit.Framework.Assert.That(y'.FullName, Is.EqualTo here))
          z |> Seq.iter (fun z' -> NUnit.Framework.Assert.That(z'.FullName, Is.EqualTo(Path.GetDirectoryName here)))
          t
          |> Seq.zip y
          |> Seq.iter (fun (t', y') -> NUnit.Framework.Assert.That(t'.FullName, Is.EqualTo y'.FullName))
          NUnit.Framework.Assert.That
            (stdout.ToString().Replace("\r", String.Empty),
             Is.EqualTo
               ("Instrumenting files from " + here + "\nWriting files to "
                + (Path.GetDirectoryName here) + "\n"))
          NUnit.Framework.Assert.That(stderr.ToString(), Is.Empty)
      finally
        Console.SetOut(fst saved)
        Console.SetError(snd saved)

    [<Test;TestMethod>]
    let OutputToReallyNewPlaceIsOK() =
      let options = Main.DeclareOptions()
      AltCover.ToConsole()
      let saved = (Console.Out, Console.Error)
      CommandLine.error <- []
      try
        use stdout = new StringWriter()
        use stderr = new StringWriter()
        Console.SetOut stdout
        Console.SetError stderr
        let here = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
        let there = Path.Combine(here, Guid.NewGuid().ToString())
        Visitor.outputDirectories.Clear()
        Visitor.inputDirectories.Clear()

        Visitor.inputDirectories.Add here
        Visitor.outputDirectories.Add there
        let rest = [ Guid.NewGuid().ToString() ]
        let arg = (rest, options)
        let ok = Right arg
        NUnit.Framework.Assert.That(Directory.Exists there, Is.False)
        match Main.ProcessOutputLocation ok with
        | Left _ -> NUnit.Framework.Assert.Fail()
        | Right(x, y, z, t) ->
          NUnit.Framework.Assert.That(x, Is.SameAs rest)
          y |> Seq.iter (fun y' -> NUnit.Framework.Assert.That(y'.FullName, Is.EqualTo here))
          z |> Seq.iter (fun z' -> NUnit.Framework.Assert.That(z'.FullName, Is.EqualTo there))
          t |> Seq.iter (fun t' -> NUnit.Framework.Assert.That(t'.FullName, Is.EqualTo here))
          NUnit.Framework.Assert.That
            (stdout.ToString().Replace("\r", String.Empty),
             Is.EqualTo
               ("Creating folder " + there + "\nInstrumenting files from " + here
                + "\nWriting files to " + there + "\n"))
          NUnit.Framework.Assert.That(stderr.ToString(), Is.Empty)
          NUnit.Framework.Assert.That(Directory.Exists there)
      finally
        Console.SetOut(fst saved)
        Console.SetError(snd saved)

    [<Test;TestMethod>]
    let InPlaceToExistingPlaceFails() =
      let options = Main.DeclareOptions()
      let saved = (Console.Out, Console.Error)
      CommandLine.error <- []
      Visitor.inplace := true
      try
        use stdout = new StringWriter()
        use stderr = new StringWriter()
        Console.SetOut stdout
        Console.SetError stderr
        let here = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
        Visitor.outputDirectories.Clear()
        Visitor.inputDirectories.Clear()
        Visitor.inputDirectories.Add here
        Visitor.outputDirectories.Add (Path.GetDirectoryName here)
        let rest = [ Guid.NewGuid().ToString() ]
        let arg = (rest, options)
        let ok = Right arg
        match Main.ProcessOutputLocation ok with
        | Right _ -> NUnit.Framework.Assert.Fail()
        | Left _ ->
          NUnit.Framework.Assert.That(stdout.ToString(), Is.Empty)
          NUnit.Framework.Assert.That(stderr.ToString(), Is.Empty)
          NUnit.Framework.Assert.That
            (CommandLine.error,
             Is.EquivalentTo
               [ "Output directory for saved files " + (Visitor.OutputDirectories() |> Seq.head)
                 + " already exists" ])
      finally
        Visitor.inplace := false
        Console.SetOut(fst saved)
        Console.SetError(snd saved)

    [<Test;TestMethod>]
    let InPlaceOperationIsAsExpected() =
      let options = Main.DeclareOptions()
      let saved = (Console.Out, Console.Error)
      CommandLine.error <- []
      Visitor.inplace := true
      try
        use stdout = new StringWriter()
        use stderr = new StringWriter()
        Console.SetOut stdout
        Console.SetError stderr
        let here = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
        let there = Path.Combine(here, Guid.NewGuid().ToString())
        Visitor.outputDirectories.Clear()
        Visitor.inputDirectories.Clear()
        Visitor.inputDirectories.Add here
        Visitor.outputDirectories.Add there
        let rest = [ Guid.NewGuid().ToString() ]
        let arg = (rest, options)
        let ok = Right arg
        NUnit.Framework.Assert.That(Directory.Exists there, Is.False)
        match Main.ProcessOutputLocation ok with
        | Left _ -> NUnit.Framework.Assert.Fail()
        | Right(x, y, z, t) ->
          NUnit.Framework.Assert.That(x, Is.SameAs rest)
          y |> Seq.iter (fun y' -> NUnit.Framework.Assert.That(y'.FullName, Is.EqualTo here))
          z |> Seq.iter (fun z' -> NUnit.Framework.Assert.That(z'.FullName, Is.EqualTo there))
          t |> Seq.iter (fun t' -> NUnit.Framework.Assert.That(t'.FullName, Is.EqualTo there))
          NUnit.Framework.Assert.That
            (stdout.ToString().Replace("\r", String.Empty),
             Is.EqualTo
               ("Creating folder " + there + "\nSaving files to " + there
                + "\nInstrumenting files in " + here + "\n"))
          NUnit.Framework.Assert.That(stderr.ToString(), Is.Empty)
          NUnit.Framework.Assert.That(Directory.Exists there)
          NUnit.Framework.Assert.That(Visitor.SourceDirectories() |> Seq.head, Is.EqualTo there)
          NUnit.Framework.Assert.That(Visitor.InstrumentDirectories() |> Seq.head, Is.EqualTo here)
      finally
        Visitor.inplace := false
        Console.SetOut(fst saved)
        Console.SetError(snd saved)

    [<Test;TestMethod>]
    let ImageLoadResilientPassesThrough() =
      let one = ref false
      let two = ref false
      Main.ImageLoadResilient (fun () -> one := true) (fun () -> two := true)
      NUnit.Framework.Assert.That(!one)
      NUnit.Framework.Assert.That(!two, Is.False)

    [<Test;TestMethod>]
    let ResilientHandlesIOException() =
      let one = ref false
      let two = ref false
      Main.ImageLoadResilient (fun () ->
        IOException("fail") |> raise
        one := true) (fun () -> two := true)
      NUnit.Framework.Assert.That(!one, Is.False)
      NUnit.Framework.Assert.That(!two)

    [<Test;TestMethod>]
    let ResilientHandlesBadImageFormatException() =
      let one = ref false
      let two = ref false
      Main.ImageLoadResilient (fun () ->
        BadImageFormatException("fail") |> raise
        one := true) (fun () -> two := true)
      NUnit.Framework.Assert.That(!one, Is.False)
      NUnit.Framework.Assert.That(!two)

    [<Test;TestMethod>]
    let ResilientHandlesArgumentException() =
      let one = ref false
      let two = ref false
      Main.ImageLoadResilient (fun () ->
        ArgumentException("fail") |> raise
        one := true) (fun () -> two := true)
      NUnit.Framework.Assert.That(!one, Is.False)
      NUnit.Framework.Assert.That(!two)

    [<Test;TestMethod>]
    let PreparingNewPlaceShouldCopyEverything() =
      let monoRuntime =
        "Mono.Runtime"
        |> Type.GetType
        |> isNull
        |> not
      // because mono symbol-writing is broken, work around trying to
      // examine the instrumented files in a self-test run.
      let here = if monoRuntime
                 then Path.Combine(SolutionRoot.location, "_Binaries/AltCover/Debug+AnyCPU")
                 else Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
      let there = Path.Combine(here, Guid.NewGuid().ToString())
      let toInfo = [ Directory.CreateDirectory there ]
      let fromInfo = [ DirectoryInfo(here) ]
      let (x, y) = Main.PrepareTargetFiles fromInfo toInfo fromInfo [there]
      Seq.zip fromInfo toInfo
      |> Seq.iter (fun (f,t) ->
        NUnit.Framework.Assert.That
          (t.EnumerateFiles() |> Seq.map (fun x -> x.Name),
           Is.EquivalentTo(f.EnumerateFiles() |> Seq.map (fun x -> x.Name)),
           "Simple to-from comparison failed")
        NUnit.Framework.Assert.That
          (x
           |> Seq.filter (fun (_,l) -> l |> List.exists (fun i -> i = t.FullName))
           |> Seq.map fst
           |> Seq.filter
                (fun f -> f.EndsWith(".dl_", StringComparison.OrdinalIgnoreCase) |> not)
           |> Seq.filter
                (fun f -> (f |> Path.GetFileName).StartsWith("xunit.", StringComparison.OrdinalIgnoreCase) |> not)
           |> Seq.filter
                (fun f -> (f |> Path.GetFileName).StartsWith("FSharp.", StringComparison.OrdinalIgnoreCase) |> not),
           Is.EquivalentTo
             (f.EnumerateFiles()
              |> Seq.map (fun x -> x.FullName)
              |> Seq.filter
                   (fun f ->
                   f.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
                   || f.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
              |> Seq.filter
                   (fun f ->
                   File.Exists(Path.ChangeExtension(f, ".pdb")) ||
                   File.Exists(f + ".mdb") ||
                   f |> Path.GetFileNameWithoutExtension = "Sample8")),
           "First list mismatch with from files")
        NUnit.Framework.Assert.That(y,
                    Is.EquivalentTo(x
                                    |> Seq.map (fst >> Path.GetFileNameWithoutExtension)),
                                    "Second list mismatch"))

    [<Test;TestMethod>]
    let ShouldProcessTrailingArguments() =
      // Hack for running while instrumented
      let where = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
      let path =
        Path.Combine(where.Substring(0, where.IndexOf("_Binaries")), "_Mono/Sample1")
#if NETCOREAPP2_0

      let path' =
        if Directory.Exists path then path
        else Path.Combine(where.Substring(0, where.IndexOf("_Binaries")), monoSample1)
#else
      let path' = path
#endif
      let files = Directory.GetFiles(path')

      let program =
        files
        |> Seq.filter (fun x -> x.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
        |> Seq.head

      let saved = (Console.Out, Console.Error)
      let e0 = Console.Out.Encoding
      let e1 = Console.Error.Encoding
      AltCover.ToConsole()
      try
        use stdout =
          { new StringWriter() with
              member self.Encoding = e0 }

        use stderr =
          { new StringWriter() with
              member self.Encoding = e1 }

        Console.SetOut stdout
        Console.SetError stderr
        let u1 = Guid.NewGuid().ToString()
        let u2 = Guid.NewGuid().ToString()
        let baseArgs = [ program; u1; u2 ]
        let nonWindows = System.Environment.GetEnvironmentVariable("OS") <> "Windows_NT"

        let args =
          if nonWindows then "mono" :: baseArgs
          else baseArgs

        let r = CommandLine.ProcessTrailingArguments args (DirectoryInfo(where))
        NUnit.Framework.Assert.That(r, Is.EqualTo 0)
        NUnit.Framework.Assert.That(stderr.ToString(), Is.Empty)
        let result = stdout.ToString()

        let quote =
          if System.Environment.GetEnvironmentVariable("OS") = "Windows_NT" then "\""
          else String.Empty

        let expected =
          "Command line : '" + quote + args.Head + quote + " "
          + String.Join(" ", args.Tail) + "'" + Environment.NewLine
          + "Where is my rocket pack? " + u1 + "*" + u2 + Environment.NewLine
        NUnit.Framework.Assert.That(result, Is.EqualTo expected)
      finally
        Console.SetOut(fst saved)
        Console.SetError(snd saved)

    [<Test;TestMethod>]
    let StoresAsExpected() =
      Api.store <- String.Empty
      Api.LogToStore.Info "23"
      NUnit.Framework.Assert.That(Api.store, Is.EqualTo "23")

    [<Test;TestMethod>]
    let IpmoIsAsExpected() =
      AltCover.ToConsole()
      let saved = Console.Out
      try
        use stdout = new StringWriter()
        Console.SetOut stdout
        let rc = AltCover.Main.EffectiveMain [| "i" |]
        NUnit.Framework.Assert.That(rc, Is.EqualTo 0)
        let result = stdout.ToString().Replace("\r\n", "\n")
        let expected = "Import-Module \""
                       + Path.Combine
                           (Assembly.GetExecutingAssembly().Location
                            |> Path.GetDirectoryName, "AltCover.PowerShell.dll") + """"
"""
        NUnit.Framework.Assert.That
          (result.Replace("\r\n", "\n"), Is.EqualTo(expected.Replace("\r\n", "\n")))
      finally
        Console.SetOut saved

    [<Test;TestMethod>]
    let VersionIsAsExpected() =
      AltCover.ToConsole()
      let saved = Console.Out
      try
        use stdout = new StringWriter()
        Console.SetOut stdout
        let rc = AltCover.Main.EffectiveMain [| "v" |]
        NUnit.Framework.Assert.That(rc, Is.EqualTo 0)
        let result = stdout.ToString().Replace("\r\n", "\n")
        let expected = "AltCover version "
                       + AssemblyVersionInformation.AssemblyFileVersion + """
"""
        NUnit.Framework.Assert.That
          (result.Replace("\r\n", "\n"), Is.EqualTo(expected.Replace("\r\n", "\n")))
      finally
        Console.SetOut saved

    [<Test;TestMethod>]
    let UsageIsAsExpected() =
      let options = Main.DeclareOptions()
      let saved = Console.Error
      try
        use stderr = new StringWriter()
        Console.SetError stderr
        let empty = OptionSet()
        CommandLine.Usage { Intro = "UsageError"; Options = options; Options2 = empty }
        let result = stderr.ToString().Replace("\r\n", "\n")
        let expected = """Error - usage is:
  -i, --inputDirectory=VALUE Optional, multiple: A folder containing assemblies
                               to instrument (default: current directory)
  -o, --outputDirectory=VALUE
                             Optional, multiple: A folder to receive the
                               instrumented assemblies and their companions (
                               default: sub-folder '__Instrumented' of the
                               current directory; or '__Saved' if '--inplace'
                               is set).
                               See also '--inplace'
  -y, --symbolDirectory=VALUE
                             Optional, multiple: Additional directory to search
                               for matching symbols for the assemblies in the
                               input directory
  -d, --dependency=VALUE     Optional, multiple: assembly path to resolve
                               missing reference.
  -k, --key=VALUE            Optional, multiple: any other strong-name key to
                               use
      --sn, --strongNameKey=VALUE
                             Optional: The default strong naming key to apply
                               to instrumented assemblies (default: None)
  -x, --xmlReport=VALUE      Optional: The output report template file (default:
                                coverage.xml in the current directory)
  -f, --fileFilter=VALUE     Optional, multiple: source file name to exclude
                               from instrumentation
  -p, --pathFilter=VALUE     Optional, multiple: source file path to exclude
                               from instrumentation
  -s, --assemblyFilter=VALUE Optional, multiple: assembly name to exclude from
                               instrumentation
  -e, --assemblyExcludeFilter=VALUE
                             Optional, multiple: assembly which links other
                               instrumented assemblies but for which internal
                               details may be excluded
  -t, --typeFilter=VALUE     Optional, multiple: type name to exclude from
                               instrumentation
  -m, --methodFilter=VALUE   Optional, multiple: method name to exclude from
                               instrumentation
  -a, --attributeFilter=VALUE
                             Optional, multiple: attribute name to exclude from
                               instrumentation
  -l, --localSource          Don't instrument code for which the source file is
                               not present.
  -c, --callContext=VALUE    Optional, multiple: Tracking either times of
                               visits in ticks or designated method calls
                               leading to the visits.
                                   A single digit 0-7 gives the number of
                               decimal places of seconds to report; everything
                               else is at the mercy of the system clock
                               information available through DateTime.UtcNow
                                   A string in brackets "[]" is interpreted as
                               an attribute type name (the trailing "Attribute"
                               is optional), so [Test] or [TestAttribute] will
                               match; if the name contains one or more ".",
                               then it will be matched against the full name of
                               the attribute type.
                                   Other strings are interpreted as method
                               names (fully qualified if the string contains
                               any "." characters).
                                   Incompatible with --single
      --opencover            Optional: Generate the report in OpenCover format
      --inplace              Optional: Instrument the inputDirectory, rather
                               than the outputDirectory (e.g. for dotnet test)
      --save                 Optional: Write raw coverage data to file for
                               later processing
      --single               Optional: only record the first hit at any
                               location.
                                   Incompatible with --callContext.
      --linecover            Optional: Do not record branch coverage.  Implies,
                               and is compatible with, the --opencover option.
                                   Incompatible with --branchcover.
      --branchcover          Optional: Do not record line coverage.  Implies,
                               and is compatible with, the --opencover option.
                                   Incompatible with --linecover.
      --dropReturnCode       Optional: Do not report any non-zero return code
                               from a launched process.
      --sourcelink           Optional: Display sourcelink URLs rather than file
                               paths if present.
      --defer[=VALUE]        Optional, defers writing runner-mode coverage data
                               until process exit.
  -v, --visibleBranches      Hide complex internal IL branching implementation
                               details in switch/match constructs, and just
                               show what the source level logic implies.
  -?, --help, -h             Prints out the options.
or
  ipmo                       Prints out the PowerShell script to import the
                               associated PowerShell module
or
  version                    Prints out the AltCover build version
"""
        NUnit.Framework.Assert.That
          (result.Replace("\r\n", "\n"), Is.EqualTo(expected.Replace("\r\n", "\n")))
      finally
        Console.SetError saved

    [<Test;TestMethod>]
    let ErrorResponseIsAsExpected() =
      let saved = Console.Error
      try
        use stderr = new StringWriter()
        Console.SetError stderr
        let unique = Guid.NewGuid().ToString()
        let main =
          typeof<Node>.Assembly.GetType("AltCover.AltCover")
            .GetMethod("Main", BindingFlags.NonPublic ||| BindingFlags.Static)
        let returnCode = main.Invoke(null, [| [| "-i"; unique |] |])
        NUnit.Framework.Assert.That(returnCode, Is.EqualTo 255)
        let result = stderr.ToString().Replace("\r\n", "\n")
        let expected = "\"-i\" \"" + unique + "\"\n" + "--inputDirectory : Directory "
                       + unique + " not found\n" + """Error - usage is:
  -i, --inputDirectory=VALUE Optional, multiple: A folder containing assemblies
                               to instrument (default: current directory)
  -o, --outputDirectory=VALUE
                             Optional, multiple: A folder to receive the
                               instrumented assemblies and their companions (
                               default: sub-folder '__Instrumented' of the
                               current directory; or '__Saved' if '--inplace'
                               is set).
                               See also '--inplace'
  -y, --symbolDirectory=VALUE
                             Optional, multiple: Additional directory to search
                               for matching symbols for the assemblies in the
                               input directory
  -d, --dependency=VALUE     Optional, multiple: assembly path to resolve
                               missing reference.
  -k, --key=VALUE            Optional, multiple: any other strong-name key to
                               use
      --sn, --strongNameKey=VALUE
                             Optional: The default strong naming key to apply
                               to instrumented assemblies (default: None)
  -x, --xmlReport=VALUE      Optional: The output report template file (default:
                                coverage.xml in the current directory)
  -f, --fileFilter=VALUE     Optional, multiple: source file name to exclude
                               from instrumentation
  -p, --pathFilter=VALUE     Optional, multiple: source file path to exclude
                               from instrumentation
  -s, --assemblyFilter=VALUE Optional, multiple: assembly name to exclude from
                               instrumentation
  -e, --assemblyExcludeFilter=VALUE
                             Optional, multiple: assembly which links other
                               instrumented assemblies but for which internal
                               details may be excluded
  -t, --typeFilter=VALUE     Optional, multiple: type name to exclude from
                               instrumentation
  -m, --methodFilter=VALUE   Optional, multiple: method name to exclude from
                               instrumentation
  -a, --attributeFilter=VALUE
                             Optional, multiple: attribute name to exclude from
                               instrumentation
  -l, --localSource          Don't instrument code for which the source file is
                               not present.
  -c, --callContext=VALUE    Optional, multiple: Tracking either times of
                               visits in ticks or designated method calls
                               leading to the visits.
                                   A single digit 0-7 gives the number of
                               decimal places of seconds to report; everything
                               else is at the mercy of the system clock
                               information available through DateTime.UtcNow
                                   A string in brackets "[]" is interpreted as
                               an attribute type name (the trailing "Attribute"
                               is optional), so [Test] or [TestAttribute] will
                               match; if the name contains one or more ".",
                               then it will be matched against the full name of
                               the attribute type.
                                   Other strings are interpreted as method
                               names (fully qualified if the string contains
                               any "." characters).
                                   Incompatible with --single
      --opencover            Optional: Generate the report in OpenCover format
      --inplace              Optional: Instrument the inputDirectory, rather
                               than the outputDirectory (e.g. for dotnet test)
      --save                 Optional: Write raw coverage data to file for
                               later processing
      --single               Optional: only record the first hit at any
                               location.
                                   Incompatible with --callContext.
      --linecover            Optional: Do not record branch coverage.  Implies,
                               and is compatible with, the --opencover option.
                                   Incompatible with --branchcover.
      --branchcover          Optional: Do not record line coverage.  Implies,
                               and is compatible with, the --opencover option.
                                   Incompatible with --linecover.
      --dropReturnCode       Optional: Do not report any non-zero return code
                               from a launched process.
      --sourcelink           Optional: Display sourcelink URLs rather than file
                               paths if present.
      --defer[=VALUE]        Optional, defers writing runner-mode coverage data
                               until process exit.
  -v, --visibleBranches      Hide complex internal IL branching implementation
                               details in switch/match constructs, and just
                               show what the source level logic implies.
  -?, --help, -h             Prints out the options.
or
  Runner
  -r, --recorderDirectory=VALUE
                             The folder containing the instrumented code to
                               monitor (including the AltCover.Recorder.g.dll
                               generated by previous a use of the .net core
                               AltCover).
  -w, --workingDirectory=VALUE
                             Optional: The working directory for the
                               application launch
  -x, --executable=VALUE     The executable to run e.g. dotnet
      --collect              Optional: Process previously saved raw coverage
                               data, rather than launching a process.
  -l, --lcovReport=VALUE     Optional: File for lcov format version of the
                               collected data
  -t, --threshold=VALUE      Optional: minimum acceptable coverage percentage (
                               integer, 0 to 100).  If the coverage result is
                               below threshold, the return code of the process
                               is (threshold - actual) rounded up to the
                               nearest integer.
  -c, --cobertura=VALUE      Optional: File for Cobertura format version of the
                               collected data
  -o, --outputFile=VALUE     Optional: write the recorded coverage to this file
                               rather than overwriting the original report file.
      --dropReturnCode       Optional: Do not report any non-zero return code
                               from a launched process.
      --teamcity[=VALUE]     Optional: Show summary in TeamCity format as well
                               as/instead of the OpenCover summary
  -?, --help, -h             Prints out the options.
"""
        NUnit.Framework.Assert.That
          (result.Replace("\r\n", "\n"), Is.EqualTo(expected.Replace("\r\n", "\n")))
      finally
        Console.SetError saved

    // Tasks.fs
    [<Test;TestMethod>]
    let LoggingCanBeExercised() =
      NUnit.Framework.Assert.That(FSApi.Logging.ActionAdapter null, Is.Not.Null)
      (FSApi.Logging.ActionAdapter null) "23"
      NUnit.Framework.Assert.That(FSApi.Logging.ActionAdapter(new Action<String>(ignore)), Is.Not.Null)
      let mutable x = String.Empty
      let f = (fun s -> x <- s)
      (FSApi.Logging.ActionAdapter(new Action<String>(f))) "42"
      NUnit.Framework.Assert.That(x, Is.EqualTo "42")
      FSApi.Logging.Create().Info "32"
      FSApi.Logging.Create().Warn "32"
      FSApi.Logging.Create().Error "32"
      FSApi.Logging.Create().Echo "32"

    [<Test;TestMethod>]
    let EmptyInstrumentIsJustTheDefaults() =
      let subject = Prepare()
      let save = Main.EffectiveMain
      let mutable args = [| "some junk " |]
      let saved = (Output.Info, Output.Error)
      try
        subject.ACLog <- Some <| FSApi.Logging.Create()
        Main.EffectiveMain <- (fun a ->
        args <- a
        255)
        let result = subject.Execute()
        NUnit.Framework.Assert.That(result, Is.False)
        NUnit.Framework.Assert.That(args, Is.EquivalentTo [ "--opencover"; "--inplace"; "--save" ])
      finally
        Main.EffectiveMain <- save
        Output.Info <- fst saved
        Output.Error <- snd saved

    [<Test;TestMethod>]
    let NonDefaultInstrumentObsoleteIsOK() =
      let subject = Prepare()
      let save = Main.EffectiveMain
      let mutable args = [| "some junk " |]
      let saved = (Output.Info, Output.Error)
      try
        subject.ACLog <- Some <| FSApi.Logging.Create()
        Main.EffectiveMain <- (fun a ->
        args <- a
        0)
        subject.OpenCover <- false
        subject.CommandLine <- [| "testing"; "1"; "2"; "3" |]
        subject.SymbolDirectories <- [| "a"; "b" |]
        let result = subject.Execute()
        NUnit.Framework.Assert.That(result, Is.True)
        NUnit.Framework.Assert.That
          (args,
           Is.EquivalentTo
             [ "-y"; "a"; "-y"; "b"; "--inplace"; "--save"; "--"; "testing"; "1"; "2"; "3" ])
      finally
        Main.EffectiveMain <- save
        Output.Info <- fst saved
        Output.Error <- snd saved

    [<Test;TestMethod>]
    let NonDefaultInstrumentIsOK() =
      let subject = Prepare()
      let save = Main.EffectiveMain
      let mutable args = [| "some junk " |]
      let saved = (Output.Info, Output.Error)
      try
        Main.EffectiveMain <- (fun a ->
        args <- a
        0)
        subject.OpenCover <- false
        subject.CommandLine <- [| "testing"; "1"; "2"; "3" |]
        subject.SymbolDirectories <- [| "a"; "b" |]
        let result = subject.Execute()
        NUnit.Framework.Assert.That(result, Is.True)
        NUnit.Framework.Assert.That
          (args,
           Is.EquivalentTo
             [ "-y"; "a"; "-y"; "b"; "--inplace"; "--save"; "--"; "testing"; "1"; "2"; "3" ])
        NUnit.Framework.Assert.Throws<InvalidOperationException>(fun () -> subject.Message "x") |> ignore
        NUnit.Framework.Assert.Throws<InvalidOperationException>(fun () -> Output.Info "x") |> ignore
        NUnit.Framework.Assert.Throws<InvalidOperationException>(fun () -> Output.Warn "x") |> ignore
        NUnit.Framework.Assert.Throws<InvalidOperationException>(fun () -> Output.Error "x") |> ignore
      finally
        Main.EffectiveMain <- save
        Output.Info <- fst saved
        Output.Error <- snd saved

    [<Test;TestMethod>]
    let EmptyCollectIsJustTheDefaults() =
      let subject = Collect()
      let save = Main.EffectiveMain
      let mutable args = [| "some junk " |]
      let saved = (Output.Info, Output.Error)
      try
        subject.ACLog <- Some <| FSApi.Logging.Create()
        Main.EffectiveMain <- (fun a ->
        args <- a
        255)
        let result = subject.Execute()
        NUnit.Framework.Assert.That(result, Is.False)
        NUnit.Framework.Assert.That(args, Is.EquivalentTo [ "Runner"; "--collect" ])
      finally
        Main.EffectiveMain <- save
        Output.Info <- fst saved
        Output.Error <- snd saved

    [<Test;TestMethod>]
    let CollectWithExeIsNotCollecting() =
      let subject = Collect()
      let save = Main.EffectiveMain
      let mutable args = [| "some junk " |]
      let saved = (Output.Info, Output.Error)
      try
        Main.EffectiveMain <- (fun a ->
        args <- a
        0)
        subject.Executable <- "dotnet"
        subject.CommandLine <- [| "test" |]
        let result = subject.Execute()
        NUnit.Framework.Assert.That(result, Is.True)
        NUnit.Framework.Assert.That(args, Is.EquivalentTo [ "Runner"; "-x"; "dotnet"; "--"; "test" ])
        NUnit.Framework.Assert.Throws<InvalidOperationException>(fun () -> subject.Message "x") |> ignore
        NUnit.Framework.Assert.Throws<InvalidOperationException>(fun () -> Output.Info "x") |> ignore
        NUnit.Framework.Assert.Throws<InvalidOperationException>(fun () -> Output.Warn "x") |> ignore
        NUnit.Framework.Assert.Throws<InvalidOperationException>(fun () -> Output.Error "x") |> ignore
      finally
        Main.EffectiveMain <- save
        Output.Info <- fst saved
        Output.Error <- snd saved

    [<Test;TestMethod>]
    let EmptyPowerShellIsJustTheDefaults() =
      let subject = PowerShell()
      let save = Main.EffectiveMain
      let mutable args = [| "some junk " |]
      let saved = (Output.Info, Output.Error)
      let warned = Output.Warn
      NUnit.Framework.Assert.Throws<InvalidOperationException>(fun () -> subject.IO.Warn "x") |> ignore
      NUnit.Framework.Assert.Throws<InvalidOperationException>(fun () -> subject.IO.Error "x") |> ignore
      subject.IO <- FSApi.Logging.Create()
      try
        Main.EffectiveMain <- (fun a ->
        args <- a
        0)
        let result = subject.Execute()
        NUnit.Framework.Assert.That(result, Is.True)
        NUnit.Framework.Assert.That(args, Is.EquivalentTo [ "ipmo" ])
        Output.Warn "x"
        Output.Error "x"
      finally
        Main.EffectiveMain <- save
        Output.Info <- fst saved
        Output.Error <- snd saved
        Output.Warn <- warned

    [<Test;TestMethod>]
    let EmptyVersionIsJustTheDefaults() =
      let subject = GetVersion()
      let save = Main.EffectiveMain
      let mutable args = [| "some junk " |]
      let saved = (Output.Info, Output.Error)
      let warned = Output.Warn
      NUnit.Framework.Assert.Throws<InvalidOperationException>(fun () -> subject.IO.Warn "x") |> ignore
      NUnit.Framework.Assert.Throws<InvalidOperationException>(fun () -> subject.IO.Error "x") |> ignore
      subject.IO <- FSApi.Logging.Create()
      try
        Main.EffectiveMain <- (fun a ->
        args <- a
        0)
        let result = subject.Execute()
        NUnit.Framework.Assert.That(result, Is.True)
        NUnit.Framework.Assert.That(args, Is.EquivalentTo [ "version" ])
        Output.Warn "x"
        Output.Error "x"
      finally
        Main.EffectiveMain <- save
        Output.Info <- fst saved
        Output.Error <- snd saved
        Output.Warn <- warned

    [<Test;TestMethod>]
    let EchoWorks() =
      let saved = (Console.Out, Console.Error)
      let e0 = Console.Out.Encoding
      let e1 = Console.Error.Encoding
      let before = Console.ForegroundColor

      try
        use stdout =
          { new StringWriter() with
              member self.Encoding = e0 }

        use stderr =
          { new StringWriter() with
              member self.Encoding = e1 }

        Console.SetOut stdout
        Console.SetError stderr

        let subject = Echo()
        let unique = Guid.NewGuid().ToString()
        subject.Text <- unique
        subject.Colour <- "cyan"
        NUnit.Framework.Assert.That (subject.Execute(), Is.True)
        NUnit.Framework.Assert.That (Console.ForegroundColor, Is.EqualTo before)
        NUnit.Framework.Assert.That (stderr.ToString(), Is.Empty)
        NUnit.Framework.Assert.That (stdout.ToString(), Is.EqualTo (unique + Environment.NewLine))
      finally
        Console.SetOut(fst saved)
        Console.SetError(snd saved)

#if NETCOREAPP2_0
    [<Test;TestMethod>]
    let RunSettingsFailsIfCollectorNotFound() =
      let subject = RunSettings()
      subject.DataCollector <- Guid.NewGuid().ToString();
      NUnit.Framework.Assert.That (subject.Execute(), Is.False)
      NUnit.Framework.Assert.That (subject.Extended, Is.Empty)

    let template = """<?xml version="1.0" encoding="utf-8"?>
<RunSettings>
{1}  <InProcDataCollectionRunSettings>
    <InProcDataCollectors>
      <InProcDataCollector friendlyName="AltCover" uri="InProcDataCollector://AltCover/Recorder/1.0.0.0" assemblyQualifiedName="AltCover.DataCollector, AltCover.Tests, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null" codebase="{0}">
        <Configuration>
          <Offload>true</Offload>
        </Configuration>
      </InProcDataCollector>
    </InProcDataCollectors>
  </InProcDataCollectionRunSettings>
</RunSettings>"""

    [<Test;TestMethod>]
    let RunSettingsWorksIfOK() =
      let subject = RunSettings()
      subject.DataCollector <- Assembly.GetExecutingAssembly().Location
      NUnit.Framework.Assert.That (subject.Execute(), Is.True)
      NUnit.Framework.Assert.That (subject.Extended.EndsWith(".altcover.runsettings"))
      let result = subject.Extended
                   |> File.ReadAllText
      NUnit.Framework.Assert.That (result.Replace("\r", String.Empty),
                    Is.EqualTo ((String.Format(template,
                                               Assembly.GetExecutingAssembly().Location,
                                               String.Empty)).Replace("\r", String.Empty)))

    [<Test;TestMethod>]
    let RunSettingsExtendsOK() =
      let subject = RunSettings()
      subject.DataCollector <- Assembly.GetExecutingAssembly().Location
      let settings = Path.GetTempFileName()
      File.WriteAllText(settings, "<RunSettings><stuff /></RunSettings>")
      subject.TestSetting <- settings
      NUnit.Framework.Assert.That (subject.Execute(), Is.True)
      NUnit.Framework.Assert.That (subject.Extended.EndsWith(".altcover.runsettings"))
      let result = subject.Extended
                   |> File.ReadAllText
      NUnit.Framework.Assert.That (result.Replace("\r", String.Empty),
                    Is.EqualTo ((String.Format(template,
                                               Assembly.GetExecutingAssembly().Location,
                                               "  <stuff />\r\n")).Replace("\r", String.Empty)))

    [<Test;TestMethod>]
    let RunSettingsRecoversOK() =
      let subject = RunSettings()
      subject.DataCollector <- Assembly.GetExecutingAssembly().Location
      let settings = Path.GetTempFileName()
      File.WriteAllText(settings, "<Not XML")
      subject.TestSetting <- settings
      NUnit.Framework.Assert.That (subject.Execute(), Is.True)
      NUnit.Framework.Assert.That (subject.Extended.EndsWith(".altcover.runsettings"))
      let result = subject.Extended
                   |> File.ReadAllText
      NUnit.Framework.Assert.That (result.Replace("\r", String.Empty),
                    Is.EqualTo ((String.Format(template,
                                               Assembly.GetExecutingAssembly().Location,
                                               String.Empty)).Replace("\r", String.Empty)))
#endif
  // Recorder.fs => Recorder.Tests