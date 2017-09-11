#r @"FakeLib.dll" // include Fake lib
open System
open System.IO

open Fake
open Fake.AssemblyInfoFile
open Fake.Testing
open Fake.OpenCoverHelper
open Fake.ReportGeneratorHelper

// The clean target cleans the build and deploy folders
Target "Clean" (fun _ ->
    CleanDirs ["./_Binaries/"; "./_Intermediate/"; "./_Generated/"; "./_Reports"]
)

Target "SetVersion" (fun _ ->
    let now = DateTimeOffset.UtcNow  
    let epoch = new DateTimeOffset(2000, 1, 1, 0, 0, 0, new TimeSpan(int64 0))  
    let diff = now.Subtract(epoch)  
    let fraction = diff.Subtract(TimeSpan.FromDays(float diff.Days))  
    let revision= ((int fraction.TotalSeconds) / 3)  
    let version = sprintf "0.0.%d.%d" diff.Days revision

    CreateFSharpAssemblyInfo "./_Generated/AssemblyVersion.fs"
        [Attribute.Version "0.0.0.0"
         Attribute.FileVersion version]

    let template ="#light
namespace Altcover
open System.Runtime.CompilerServices
[<assembly: InternalsVisibleTo(\"AltCover.Tests, PublicKey=0024000004800000940000000602000000240000525341310004000001000100916443a2ee1d294e8cfa7666fb3f512d998d7ceac4909e35edb2ac1e104de68890a93716d1d1931f7228aac0523cacf50fd82cdb4ccf4ff4bf0ded95e3a383f4f371e3b82c45502ce74d7d572583495208c1905e0f1e8a3cce66c4c75e4ca32e9a8f8dee64e059c0dc0266e8d2cb6d7ebd464b47e062f80b63d390e389217fb7\")>]
()
"
    let file = String.Format(System.Globalization.CultureInfo.InvariantCulture, template) //, token)
    let path = @"_Generated\VisibleToTest.fs"
    // Update the file only if it would change
    let old = if File.Exists(path) then File.ReadAllText(path) else String.Empty
    if not (old.Equals(file)) then File.WriteAllText(path, file)
)         

Target "BuildRelease" (fun _ ->
   !! "*.sln"
     |> MSBuildRelease "" ""
     |> Log "AppBuild-Output: "
)

Target "BuildDebug" (fun _ ->
   !! "*.sln"
     |> MSBuildDebug "" ""
     |> Log "AppBuild-Output: "
)

Target "TestCover" (fun _ ->
    ensureDirectory "./_Reports"
    OpenCover (fun p -> { p with ExePath = findToolInSubPath "OpenCover.Console.exe" "."
                                 WorkingDir = "."
                                 TestRunnerExePath = findToolInSubPath "nunit3-console.exe" "."
                                 Filter = "+[AltCove*]*"
                                 MergeByHash = true
                                 Register = RegisterType.RegisterUser
                                 Output = "_Reports/OpenCoverReport.xml" }) 
        "_Binaries/AltCover.Tests/Debug+AnyCPU/AltCover.Tests.dll --result=./_Reports/NUnit3Report.xml"
    ReportGenerator (fun p -> { p with ExePath = findToolInSubPath "Reportgenerator.exe" "."
                                       TargetDir = "_Reports"})
        ["./_Reports/OpenCoverReport.xml"]
)             

Target "Test" (fun _ ->
    ensureDirectory "./_Reports"
    !! (@"_Binaries\*Tests\Debug+AnyCPU\*.Tests.dll")
    |> NUnit3 (fun p -> { p with ToolPath = findToolInSubPath "nunit3-console.exe" "."
                                 WorkingDir = "."
                                 ResultSpecs = ["./_Reports/NUnit3Report.xml"] })
)             

// This defaults to Microsoft Visual Studio 10.0\Team Tools\Static Analysis Tools\FxCop\FxCopCmd.exe
Target "FxCop" (fun _ ->
    let fxCop = combinePaths (environVar "VS150COMNTOOLS") "../../Team Tools/Static Analysis Tools/FxCop/FxCopCmd.exe"
    !! (@"_Binaries\*Tests\Debug+AnyCPU/Altcove*.*") 
    |> Seq.filter (fun n -> not (n.EndsWith(".Tests.dll")))
    |> Seq.filter (fun n -> not (n.EndsWith(".pdb")))
    |> FxCop (fun p -> { p with ToolPath = fxCop
                                WorkingDir = "."
                                Verbose = false
                                ReportFileName = "_Reports/FxCopReport.xml"})
)


"Clean"
==> "SetVersion"
==> "BuildRelease"

"SetVersion"
=?> ("BuildDebug", (not(File.Exists("./_Generated/AssemblyVersion.fs"))))

"BuildDebug"
==> "Test"

"BuildDebug"
==> "TestCover"

"BuildDebug"
==> "FxCop"


RunTargetOrDefault "TestCover"
