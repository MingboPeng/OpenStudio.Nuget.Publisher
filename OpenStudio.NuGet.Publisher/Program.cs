using System.IO.Compression;
using System.Net.Http.Json;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

var system = "Windows";
if (ThisOS.isMacOS())
    system = "MacOS";
else if (ThisOS.isLinux())
    system = "Linux";

Console.WriteLine($"Hello, OpenStudio Nuget Publisher on {system}!");
var commandArgs = args.Select(x => x.Trim()).Where(_ => !string.IsNullOrEmpty(_)).ToList();

var assembly = typeof(Program).Assembly;
if (!commandArgs.Any() || commandArgs.Count() != 2)
{
    Console.WriteLine("Invalid inputs. The default value will be used: NREL.OpenStudio.win, 3.6.1");
    commandArgs.Add("NREL.OpenStudio.win");
    commandArgs.Add("3.5.1");
}

var packageID = commandArgs.ElementAtOrDefault(0); // "NREL.OpenStudio.win";
var version = commandArgs.ElementAtOrDefault(1); // "3.6.1";

Console.WriteLine($"[INFO] Input packageID:\n {packageID}");
Console.WriteLine($"[INFO] Input version:\n {version}");


var repoDir = Path.GetFullPath(".");
repoDir = repoDir.Contains("bin") ? repoDir.Substring(0, repoDir.LastIndexOf("OpenStudio.NuGet.Publisher")): repoDir;
Console.WriteLine($"[INFO] Current dir:\n {repoDir}");

var nugetVersions = new List<Version>();
try
{
    // check version https://api.nuget.org/v3-flatcontainer/nrel.openstudio.win/index.json
    var api = @$"https://api.nuget.org/v3-flatcontainer/{packageID.ToLower()}/index.json";
    var httpC = new HttpClient();
    nugetVersions = httpC.GetFromJsonAsync<nugetVersions>(api).GetAwaiter().GetResult()?.versions.OrderBy(_ => _)?.ToList();

}
catch (Exception)
{
    //throw;
}


var inputVersion = Version.Parse(version);
while (nugetVersions.Contains(inputVersion))
{
    var rev = inputVersion.Revision;
    if (rev == -1)
        rev = 0;
    inputVersion = new Version(inputVersion.Major, inputVersion.Minor, inputVersion.Build, rev + 1);

}

version = inputVersion.ToString();
Console.WriteLine($"Done checking version {version}");

var workDir = Path.Combine(repoDir, "nuget");

// clean up
if (Directory.Exists(workDir))
    Directory.Delete(workDir, true);

var nugetPath = Directory.GetFiles(repoDir, "*.nupkg").FirstOrDefault();

if (string.IsNullOrEmpty(nugetPath))
    throw new ArgumentException($"Failed to find a NuGet package in {repoDir}");

ZipFile.ExtractToDirectory(nugetPath, workDir);
Console.WriteLine($"Extracting {nugetPath}");

// update nuget package for Windows

// update OpenStudio.nuspec
var nuspec =  Directory.GetFiles(workDir,"*.nuspec").FirstOrDefault();
var nuspecTemp = Path.Combine(repoDir, "Template", "OpenStudio.nuspec");
File.Copy(nuspecTemp, nuspec, true);
FixNuspec(nuspec, packageID, version);
Console.WriteLine("Done fixing nuspec!");


// remove files for targetFramework 4.5 build\net45
var net45Dir = Path.Combine(workDir, "build", "net45");
if (Directory.Exists(net45Dir))
{
    Directory.Delete(net45Dir, true);
    Console.WriteLine("Done removing net45 dir!");
}

// update .targets
var targets = Directory.GetFiles(workDir, "*.targets", SearchOption.AllDirectories).FirstOrDefault();
var targetTemp = Path.Combine(repoDir, "Template", $"{packageID}.targets");
var newTargets = Path.Combine(Path.GetDirectoryName(targets), $"OpenStudio.targets");


File.Delete(targets);
if (!File.Exists(targetTemp))
    throw new ArgumentException($"Failed to find {targetTemp}");
File.Copy(targetTemp, newTargets, true);
Console.WriteLine("Done updating targets!");


// remove old nupkg
File.Delete(nugetPath);

// zip back to nupkg.
var newNugetPath = Path.Combine(repoDir, $"{packageID}.{version}.nupkg");
ZipFile.CreateFromDirectory(workDir, newNugetPath);
Console.WriteLine($"Done creating {newNugetPath}");



void FixNuspec(string nuspecPath, string packId, string version)
{
    var text = File.ReadAllText(nuspecPath);
    // replace ID <id>OpenStudio</id> with <id>NREL.OpenStudio.win</id>
    text = Regex.Replace(text, @"<id>\w*</id>", $"<id>{packId}</id>", RegexOptions.IgnoreCase);
    text = Regex.Replace(text, @"<version>\S*</version>", $"<version>{version}</version>", RegexOptions.IgnoreCase);
    File.WriteAllText(nuspecPath, text);
}


static class ThisOS
{
    public static bool isWindows() => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
    public static bool isMacOS() => RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
    public static bool isLinux() => RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
}

class nugetVersions
{
    public List<Version> versions { get; set; }
}