#r @"packages/FAKE.4.19.0/tools/FakeLib.dll"

open Fake
open Fake.Testing
open System

let releaseFolder = "Release"
let nunitToolsFolder = "Packages/NUnit.Runners.2.6.2/tools"
let nuGetOutputFolder = "NuGetPackages"
let solutionsToBuild = !! "Src/*.sln"
let processorArchitecture = environVar "PROCESSOR_ARCHITECTURE"

let build target configuration =
    let keyFile =
        match getBuildParam "signkey" with
        | "" -> []
        | x  -> [ "AssemblyOriginatorKeyFile", FullName x ]

    let properties = [ "Configuration", configuration ] @ keyFile

    solutionsToBuild
    |> MSBuild "" target properties
    |> ignore

let clean   = build "Clean"
let rebuild = build "Rebuild"

Target "CleanAll"           (fun _ -> ())
Target "CleanVerify"        (fun _ -> clean "Verify")
Target "CleanRelease"       (fun _ -> clean "Release")
Target "CleanReleaseFolder" (fun _ -> CleanDir releaseFolder)

Target "Verify" (fun _ -> rebuild "Verify")

Target "BuildOnly" (fun _ -> rebuild "Release")
Target "TestOnly" (fun _ ->
    let configuration = getBuildParamOrDefault "Configuration" "Release"
    let parallelizeTests = getBuildParamOrDefault "ParallelizeTests" "False" |> Convert.ToBoolean
    let maxParallelThreads = getBuildParamOrDefault "MaxParallelThreads" "0" |> Convert.ToInt32
    let parallelMode = if parallelizeTests then ParallelMode.All else ParallelMode.NoParallelization
    let maxThreads = if maxParallelThreads = 0 then CollectionConcurrencyMode.Default else CollectionConcurrencyMode.MaxThreads(maxParallelThreads)

    let testAssemblies = !! (sprintf "Src/*Test/bin/%s/*Test.dll" configuration)
                         -- (sprintf "Src/AutoFixture.NUnit*.*Test/bin/%s/*Test.dll" configuration)

    let nunit3TestAssemblies = !! (sprintf "Src/AutoFixture.NUnit3.UnitTest/bin/%s/Ploeh.AutoFixture.NUnit3.UnitTest.dll" configuration)

    nunit3TestAssemblies
    |> NUnit3 (fun p -> { p with StopOnError = false
                                 ResultSpecs = ["NUnit3TestResult.xml;format=nunit2"] })
)

Target "BuildAndTestOnly" (fun _ -> ())
Target "Build" (fun _ -> ())
Target "Test"  (fun _ -> ())

Target "CopyToReleaseFolder" (fun _ ->
    let buildOutput = [
      "Src/AutoFixture.NUnit3/bin/Release/Ploeh.AutoFixture.NUnit3.dll";
      "Src/AutoFixture.NUnit3/bin/Release/Ploeh.AutoFixture.NUnit3.pdb";
      "Src/AutoFixture.NUnit3/bin/Release/Ploeh.AutoFixture.NUnit3.XML";
      nunitToolsFolder @@ "lib/nunit.core.interfaces.dll"
    ]
    let nuGetPackageScripts = !! "NuGet/*.ps1" ++ "NuGet/*.txt" ++ "NuGet/*.pp" |> List.ofSeq
    let releaseFiles = buildOutput @ nuGetPackageScripts

    releaseFiles
    |> CopyFiles releaseFolder
)

Target "CleanNuGetPackages" (fun _ ->
    CleanDir nuGetOutputFolder
)

Target "NuGetPack" (fun _ ->
    let version = "Release/Ploeh.AutoFixture.NUnit3.dll"
                  |> GetAssemblyVersion
                  |> (fun v -> sprintf "%i.%i.%i.%i" v.Major v.Minor v.Build v.Revision)

    let nuSpecFiles = !! "NuGet/*.nuspec"

    nuSpecFiles
    |> Seq.iter (fun f -> NuGet (fun p -> { p with Version = version
                                                   WorkingDir = releaseFolder
                                                   OutputPath = nuGetOutputFolder
                                                   SymbolPackage = NugetSymbolPackage.Nuspec }) f)
)

Target "CompleteBuild" (fun _ -> ())

"CleanVerify"  ==> "CleanAll"
"CleanRelease" ==> "CleanAll"

"CleanReleaseFolder" ==> "Verify"
"CleanAll"           ==> "Verify"

"Verify"    ==> "Build"
"BuildOnly" ==> "Build"

"Build"    ==> "Test"
"TestOnly" ==> "Test"

"BuildOnly" ==> "TestOnly"

"BuildOnly" ==> "BuildAndTestOnly"
"TestOnly"  ==> "BuildAndTestOnly"

"Test" ==> "CopyToReleaseFolder"

"CleanNuGetPackages"  ==> "NuGetPack"
"CopyToReleaseFolder" ==> "NuGetPack"

"NuGetPack" ==> "CompleteBuild"

RunTargetOrDefault "CompleteBuild"
