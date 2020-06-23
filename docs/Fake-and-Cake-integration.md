# F# Fake and Cake integration v7.x

APIs for use with build scripting tools are provided in the `AltCover.Cake.dll` and `AltCover.Fake.dll` assemblies, which are present in the `AltCover.Api` nuget package

* [Fake integration](#fake-integration)
* [Cake integration](#cake-integration)

# Fake integration 
Found in `AltCover.Fake.dll`  
Detailed API documentation is [presented here](AltCover.Fake/Fake-fsapidoc).

### Example
Driving `dotnet test` in a Fake script
```
open AltCover
...
  let ForceTrue = DotNet.CLIOptions.Force true  
  let p = { Primitive.PrepareOptions.Create() with CallContext = [| "[Fact]"; "0" |]
                                                    AssemblyFilter = [| "xunit" |] }
  let prep = OptionApi.PrepareOptions.Primitive p
  let c = Primitive.CollectOptions.Create()
  let collect = OptionApi.CollectOptions.Primitive c

  let setBaseOptions (o : DotNet.Options) =
    { o with WorkingDirectory = Path.getFullName "./_DotnetTest"
             Verbosity = Some DotNet.Verbosity.Minimal }

  DotNet.test
    (fun to' ->
    { to'.WithCommon(setBaseOptions).WithAltCoverOptions prep collect ForceTrue })
    "dotnettest.fsproj"
```

# Cake integration 

Found in `AltCover.Cake.dll`  
Detailed API documentation is [presented here](AltCover.Cake/AltCover.Cake-apidoc).