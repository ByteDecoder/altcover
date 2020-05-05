# CSApi.Primitive.PrepareOptions class

This type defines the Prepare (instrumentation) behaviour. The properties map on to the command line arguments for `altcover`

```csharp
public class PrepareOptions
```

## Public Members

| name | description |
| --- | --- |
| static [Create](CSApi.Primitive.PrepareOptions/Create-apidoc)() | Create a default instance with all members empty, except `ExposeReturnCode, InPlace, Save = true` |
| [AssemblyExcludeFilter](CSApi.Primitive.PrepareOptions/AssemblyExcludeFilter-apidoc) { get; set; } | Assembly names to exclude from instrumentation (linked to instrumented assemblies) |
| [AssemblyFilter](CSApi.Primitive.PrepareOptions/AssemblyFilter-apidoc) { get; set; } | Assembly names to exclude from instrumentation (linked by instrumented assemblies) |
| [AttributeFilter](CSApi.Primitive.PrepareOptions/AttributeFilter-apidoc) { get; set; } | Attribute names to exclude from instrumentation |
| [BranchCover](CSApi.Primitive.PrepareOptions/BranchCover-apidoc) { get; set; } | Indicate whether to omit ine coverage. Implies, and is compatible with, the -ReportFormat "opencover" option. Incompatible with `-LineCover`. |
| [CallContext](CSApi.Primitive.PrepareOptions/CallContext-apidoc) { get; set; } | Tracking either times of visits in ticks or designated method calls leading to the visits. |
| [CommandLine](CSApi.Primitive.PrepareOptions/CommandLine-apidoc) { get; set; } | Arguments for a launched process |
| [Defer](CSApi.Primitive.PrepareOptions/Defer-apidoc) { get; set; } | Indicate whether to defer writing runner-mode coverage data until process exit. |
| [Dependencies](CSApi.Primitive.PrepareOptions/Dependencies-apidoc) { get; set; } | Assembly paths to resolve missing references. |
| [ExposeReturnCode](CSApi.Primitive.PrepareOptions/ExposeReturnCode-apidoc) { get; set; } | Gets whether to report any non-zero return code from a launched process. |
| [FileFilter](CSApi.Primitive.PrepareOptions/FileFilter-apidoc) { get; set; } | Source file names to exclude from instrumentation |
| [InPlace](CSApi.Primitive.PrepareOptions/InPlace-apidoc) { get; set; } | Indicate whether to instrument the inputDirectory, rather than the outputDirectory (e.g. for `dotnet test`) |
| [InputDirectories](CSApi.Primitive.PrepareOptions/InputDirectories-apidoc) { get; set; } | Folder or folders containing assemblies to instrument (default: current directory) |
| [Keys](CSApi.Primitive.PrepareOptions/Keys-apidoc) { get; set; } | Strong name key or keys that were used to sign the inputs |
| [LineCover](CSApi.Primitive.PrepareOptions/LineCover-apidoc) { get; set; } | Indicate whether to omit branch coverage. Implies, and is compatible with, the -ReportFormat "opencover" option. Incompatible with `-BranchCover`. |
| [LocalSource](CSApi.Primitive.PrepareOptions/LocalSource-apidoc) { get; set; } | Indicate whether to exclude for instrumentation code for which the source file is not present. |
| [MethodFilter](CSApi.Primitive.PrepareOptions/MethodFilter-apidoc) { get; set; } | Method names to exclude from instrumentation |
| [MethodPoint](CSApi.Primitive.PrepareOptions/MethodPoint-apidoc) { get; set; } | Indicate whether to record only whether a method has been visited or not. Overrides the `LineCover` and `BranchCover` options. |
| [OutputDirectories](CSApi.Primitive.PrepareOptions/OutputDirectories-apidoc) { get; set; } | Folder or folders to receive the instrumented assemblies and their companions (default: sub-folder `__Instrumented` of the current directory; or `__Saved` if `-Inplace` is set). |
| [PathFilter](CSApi.Primitive.PrepareOptions/PathFilter-apidoc) { get; set; } | Source file paths to exclude from instrumentation |
| [ReportFormat](CSApi.Primitive.PrepareOptions/ReportFormat-apidoc) { get; set; } | Generate the report in the specified format (NCover or the default OpenCover) |
| [Save](CSApi.Primitive.PrepareOptions/Save-apidoc) { get; set; } | Indicate whether to write raw coverage data to file for later processing |
| [ShowGenerated](CSApi.Primitive.PrepareOptions/ShowGenerated-apidoc) { get; set; } | Indicate whether to mark generated code with a visit count of -2 (Automatic) for the Visualizer if unvisited |
| [ShowStatic](CSApi.Primitive.PrepareOptions/ShowStatic-apidoc) { get; set; } | Indicate whether to instrument and show code that is by default skipped as trivial. |
| [SingleVisit](CSApi.Primitive.PrepareOptions/SingleVisit-apidoc) { get; set; } | Indicate whether to record only the first hit at any location. Incompatible with `-CallContext`. |
| [SourceLink](CSApi.Primitive.PrepareOptions/SourceLink-apidoc) { get; set; } | Indicate whether to display sourcelink URLs rather than file paths if present. |
| [StrongNameKey](CSApi.Primitive.PrepareOptions/StrongNameKey-apidoc) { get; set; } | The default strong naming key to apply to instrumented assemblies |
| [SymbolDirectories](CSApi.Primitive.PrepareOptions/SymbolDirectories-apidoc) { get; set; } | Additional folder or folders to search for matching symbols for the assemblies in the input directory |
| [TypeFilter](CSApi.Primitive.PrepareOptions/TypeFilter-apidoc) { get; set; } | Type names to exclude from instrumentation |
| [VisibleBranches](CSApi.Primitive.PrepareOptions/VisibleBranches-apidoc) { get; set; } | Indicate whether to hide complex internal IL branching implementation details in switch/match constructs, and just show what the source level logic implies. |
| [XmlReport](CSApi.Primitive.PrepareOptions/XmlReport-apidoc) { get; set; } | The output report template file (default: coverage.xml in the current directory) |
| [ZipFile](CSApi.Primitive.PrepareOptions/ZipFile-apidoc) { get; set; } | Indicate whether to emit the XML report inside a zip archive. |
| [ToOptions](CSApi.Primitive.PrepareOptions/ToOptions-apidoc)() | Returns the F#-defined equivalent type |
| [Validate](CSApi.Primitive.PrepareOptions/Validate-apidoc)() | Check whether the parameters are sensible |
| [WhatIf](CSApi.Primitive.PrepareOptions/WhatIf-apidoc)() | Performs validation (implies `Validate`) suitable for a PowerShell `-WhatIf` query; the return type has form |

## See Also

* class [Primitive](CSApi.Primitive-apidoc)
* namespace [AltCover](../AltCover.CSApi-apidoc)

<!-- DO NOT EDIT: generated by xmldocmd for AltCover.CSApi.dll -->