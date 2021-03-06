# ConvertToBarChartCommand class

Generates a simple HTML report from coverage data.

The report produced is based on the old NCover 1.5.8 XSLT, for both NCover and OpenCover coverage format data. The input is as a file name or an `XDocument` from the pipeline, the output is to the pipeline as an `XDocument`, and, optionally, to a file.

```csharp
    $xml = ConvertTo-BarChart -InputFile "./Tests/HandRolledMonoCoverage.xml" -OutputFile "./_Packaging/HandRolledMonoCoverage.html"
```

```csharp
public class ConvertToBarChartCommand : PSCmdlet
```

## Public Members

| name | description |
| --- | --- |
| [ConvertToBarChartCommand](ConvertToBarChartCommand/ConvertToBarChartCommand-apidoc)() | The default constructor. |
| [InputFile](ConvertToBarChartCommand/InputFile-apidoc) { get; set; } | Input as file path |
| [OutputFile](ConvertToBarChartCommand/OutputFile-apidoc) { get; set; } | Output as file path |
| [XDocument](ConvertToBarChartCommand/XDocument-apidoc) { get; set; } | Input as `XDocument` value |
| override [ProcessRecord](ConvertToBarChartCommand/ProcessRecord-apidoc)() | Create transformed document |

## See Also

* namespace [AltCover.Commands](../AltCover.PowerShell-apidoc)

<!-- DO NOT EDIT: generated by xmldocmd for AltCover.PowerShell.dll -->
