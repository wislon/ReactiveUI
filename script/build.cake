#addin "Cake.FileHelpers"

//////////////////////////////////////////////////////////////////////
// ARGUMENTS
//////////////////////////////////////////////////////////////////////

var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");

//////////////////////////////////////////////////////////////////////
// PREPARATION
//////////////////////////////////////////////////////////////////////

// Get whether or not this is a local build.
var local = BuildSystem.IsLocalBuild;
var isRunningOnUnix = IsRunningOnUnix();
var isRunningOnWindows = IsRunningOnWindows();

//var isRunningOnBitrise = Bitrise.IsRunningOnBitrise;
//var isRunningOnAppVeyor = AppVeyor.IsRunningOnAppVeyor;
//var isPullRequest = AppVeyor.Environment.PullRequest.IsPullRequest ? Bitrise.Environment.PullRequst.IsPullRequest;
//var isMainReactiveUIRepo = StringComparer.OrdinalIgnoreCase.Equals("reactiveui/reactiveui", AppVeyor.Environment.Repository.Name) StringComparer.OrdinalIgnoreCase.Equals("reactiveui/reactiveui", Bitrise.Environment.Repository.Name);

// Parse release notes.
var releaseNotes = ParseReleaseNotes("../ReleaseNotes.md");

// Get version.
var gitSha = GitVersion.Sha;
var buildNumber = AppVeyor.Environment.Build.Number;
var version = releaseNotes.Version.ToString();
var semVersion = local ? version : (version + string.Concat("-sha-", gitSha) + string.Contact("-build-", buildNumber));

// Define directories.
var buildDir = Directory("./src/Cake/bin") + Directory(configuration);
var buildResultDir = Directory("./build") + Directory("v" + semVersion);
var testResultsDir = buildResultDir + Directory("test-results");
var nugetRoot = buildResultDir + Directory("nuget");
var binDir = buildResultDir + Directory("bin");

///////////////////////////////////////////////////////////////////////////////
// SETUP / TEARDOWN
///////////////////////////////////////////////////////////////////////////////
Setup(() =>
{
    Information("Building version {0} of ReactiveUI.", semVersion);
});

Teardown(() =>
{
    // Executed AFTER the last task.
});

//////////////////////////////////////////////////////////////////////
// TASKS
//////////////////////////////////////////////////////////////////////

Task("RunUnitTests")
    .IsDependentOn("Build")
    .Does(() =>
{
    XUnit2("../src/**/bin/" + configuration + "/*.Tests.dll", new XUnit2Settings {
        OutputDirectory = testResultsDir,
        XmlReportV1 = true,
        NoAppDomain = true
    });
});

Task("RunCodeCoverage")
    .IsDependentOn("RunUnitTests")
    .Does(() =>
{
//		OpenCover(tool => {
//		  tool.XUnit2("./**/App.Tests.dll",
//		    new XUnit2Settings {
//		      ShadowCopy = false
//		    });
//		  },
//		  new FilePath("./result.xml"),
//		  new OpenCoverSettings()
//		    .WithFilter("+[App]*")
//		    .WithFilter("-[App.Tests]*"));
});


Task ("Build")
		.IsDependentOn("BuildEvents")
		.IsDependentOn("BuildReactiveUI")

Task ("BuildEvents")
		.IsDependentOn("RestorePackages")
		.IsDependentOn("UpdateAssemblyInfo")
		.Does (() =>
{
		if(isRunningOnUnix)
		{
				// run mdtool
		}
		else
		{
				// run msbuild
		}
});


Task ("BuildReactiveUI")
		.IsDependentOn("RestorePackages")
		.IsDependentOn("UpdateAssemblyInfo")
		.IsDependentOn("BuildEvents")
		.Does (() =>
{
		if(isRunningOnUnix)
		{
		    // run mdtool
		}
		else
		{
			MSBuild("./src/ReactiveUI_VSALL.sln", new MSBuildSettings()
					.SetConfiguration(configuration)
					.WithProperty("Windows", "True")
					.WithProperty("TreatWarningsAsErrors", "True")
					.UseToolVersion(MSBuildToolVersion.NET45)
					.SetVerbosity(Verbosity.Minimal)
					.SetNodeReuse(false));
		}
});


Task ("UpdateAssemblyInfo")
		.Does (() =>
{
		var file = "./src/SolutionInfo.cs";
		CreateAssemblyInfo(file, new AssemblyInfoSettings {
				Product = "ReactiveUI",
				Version = version,
				FileVersion = version,
				InformationalVersion = semVersion,
				Copyright = "Copyright (c) ReactiveUI and contributors"
		});
});

Task ("RestorePackages").Does (() =>
{
		NuGetRestore ("../src/ReactiveUI_VSALL.sln");
    NuGetRestore ("../src/ReactiveUI_XSALL.sln");
});

Task("CopyFiles")
    .IsDependentOn("RunUnitTests")
    .Does(() =>
{
//		CopyFileToDirectory(buildDir + File("Cake.exe"), binDir);

//		// Copy testing assemblies.
//		var testingDir = Directory("./src/Cake.Testing/bin") + Directory(configuration);
//		CopyFileToDirectory(testingDir + File("Cake.Testing.dll"), binDir);

//		CopyFiles(new FilePath[] { "LICENSE", "README.md", "ReleaseNotes.md" }, binDir);
});

Task ("CreatePackages")
		.IsDependentOn("CopyFiles")
		.Does (() =>
{

});


Task("CreateArchive")
    .IsDependentOn("CopyFiles")
    .Does(() =>
{
    var packageFile = File("ReactiveUI-bin-v" + semVersion + ".zip");
    var packagePath = buildResultDir + packageFile;

    var files = GetFiles(binDir.Path.FullPath + "/*")
      - GetFiles(binDir.Path.FullPath + "/*.Testing.*");

    Zip(binDir, packagePath, files);
});

//////////////////////////////////////////////////////////////////////
// TASK TARGETS
//////////////////////////////////////////////////////////////////////

Task ("Default")
		.IsDependentOn("Package");

Task("Package")
  .IsDependentOn("CreateArchive")
  .IsDependentOn("CreatePackages");

Task("Publish")
		.IsDependentOn("PublishArchive")
		.IsDependentOn("PublishPackagesToMyGet");

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////

RunTarget(target);
