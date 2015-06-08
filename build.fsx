// --------------------------------------------------------------------------------------
// FAKE build script 
// --------------------------------------------------------------------------------------

#r @"packages/FAKE/tools/FakeLib.dll"
open Fake 
open Fake.Git
open Fake.AssemblyInfoFile
open Fake.ReleaseNotesHelper
open System

// Git configuration (used for publishing documentation in gh-pages branch)
// The profile where the project is posted 
let gitHome = "https://github.com/dungpa"
// The name of the project on GitHub
let gitName = "fantomas"
let cloneUrl = "git@github.com:dungpa/fantomas.git"

// The name of the project 
// (used by attributes in AssemblyInfo, name of a NuGet package and directory in 'src')
let project = "Fantomas"

// Short summary of the project
// (used as description in AssemblyInfo and as a short summary for NuGet package)
let summary = "Source code formatting tool for F#"


// (<solutionFile>.sln is built during the building process)
let solutionFile  = "fantomas"
let testAssemblies = "src/**/bin/Release/*Tests*.dll"

Environment.CurrentDirectory <- __SOURCE_DIRECTORY__
let release = parseReleaseNotes (IO.File.ReadAllLines "RELEASE_NOTES.md")

// --------------------------------------------------------------------------------------
// Clean build results & restore NuGet packages

Target "Clean" (fun _ ->
    CleanDirs ["bin"]
)

Target "RestorePackages" (fun _ ->
    !! "./**/packages.config"
    |> Seq.iter (RestorePackage (fun p -> { p with ToolPath = "src/.nuget/NuGet.exe" }))
)

Target "AssemblyInfo" (fun _ ->
  let shared =
      [ Attribute.Product project
        Attribute.Description summary
        Attribute.Version release.AssemblyVersion
        Attribute.FileVersion release.AssemblyVersion ] 

  CreateFSharpAssemblyInfo "src/Fantomas/AssemblyInfo.fs"
      ( Attribute.Title "FantomasLib" :: shared)

  CreateFSharpAssemblyInfo "src/Fantomas.Cmd/AssemblyInfo.fs"
      (Attribute.Title "Fantomas" :: shared)

  CreateFSharpAssemblyInfo "src/Fantomas.UI/AssemblyInfo.fs"
      (Attribute.Title "Fantomas.UI" :: shared) 
)

// --------------------------------------------------------------------------------------
// Build library & test project

Target "Build" (fun _ ->
    // We would like to build only one solution
    !! (solutionFile + ".sln")
    |> MSBuildRelease "" "Rebuild"
    |> ignore
)

Target "UnitTests" (fun _ ->
    !! testAssemblies 
    |> NUnit (fun p ->        
          { p with
              DisableShadowCopy = true
              TimeOut = TimeSpan.FromMinutes 20.
              Framework = "4.5"
              Domain = NUnitDomainModel.MultipleDomainModel
              OutputFile = "TestResults.xml" })
)

// --------------------------------------------------------------------------------------
// Run all targets by default. Invoke 'build <Target>' to override

Target "All" DoNothing

"Clean"
  ==> "RestorePackages"
  ==> "AssemblyInfo"
  ==> "Build"
  ==> "UnitTests"
  ==> "All"


RunTargetOrDefault "All"