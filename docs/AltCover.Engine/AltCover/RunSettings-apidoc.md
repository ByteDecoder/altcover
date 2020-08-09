# RunSettings class

Used by the .net core implementation to inject an AltCover data collector, by creating a temporary run-settings file that includes AltCover as well as any user-defined settings.

Not intended for general use, but see the `AltCover.targets` file for how it is used around the test stage.

```csharp
public class RunSettings : Task
```

## Public Members

| name | description |
| --- | --- |
| [RunSettings](RunSettings/RunSettings-apidoc)() | The default constructor |
| [Extended](RunSettings/Extended-apidoc) { get; set; } | The settings file generated, an output parameter |
| [TestSetting](RunSettings/TestSetting-apidoc) { get; set; } | The current settings file to be extended |
| override [Execute](RunSettings/Execute-apidoc)() | Perform the operation |

## See Also

* namespace [AltCover](../AltCover.Engine-apidoc)

<!-- DO NOT EDIT: generated by xmldocmd for AltCover.Engine.dll -->