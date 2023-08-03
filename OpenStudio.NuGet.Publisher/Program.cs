using System.IO.Compression;
using System.Text.RegularExpressions;

Console.WriteLine("Hello, World!");

var zipPath = @"C:\Users\mingo\Downloads\Compressed\CSharp_Windows_x64x86.zip";

var workDir = Path.Combine(Path.GetDirectoryName(zipPath), Path.GetFileNameWithoutExtension(zipPath));

var repoDir = Path.GetFullPath(".");
repoDir = repoDir.Substring(0, repoDir.LastIndexOf("bin"));

Directory.Delete(workDir, true);
ZipFile.ExtractToDirectory(zipPath, workDir);

var nugetPath = Directory.GetFiles(workDir, "*.nupkg").FirstOrDefault();

if (string.IsNullOrEmpty(nugetPath))
    throw new ArgumentException("Failed to find a NuGet package");

ZipFile.ExtractToDirectory(nugetPath, workDir);


// update nuget package for Windows

// update OpenStudio.nuspec
var nuspec =  Directory.GetFiles(workDir,"*.nuspec").FirstOrDefault();
var packageID = "NREL.OpenStudio.win";
var version = "3.6.1";
var year = "2023";

FixNuspec(nuspec, packageID, version, year);
Console.WriteLine("Done fixing nuspec!");


// remove files for targetFramework 4.5 build\net45
var net45Dir = Path.Combine(workDir, @"build\net45");
Directory.Delete(net45Dir, true);
Console.WriteLine("Done removing net45 dir!");


// update .targets
var targets = Directory.GetFiles(workDir, "*.targets", SearchOption.AllDirectories).FirstOrDefault();
var targetTemp = Path.Combine(repoDir, @"Template\NREL.OpenStudio.win.targets");
var newTargets = Path.Combine(Path.GetDirectoryName(targets), $"{packageID}.targets");

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



void FixNuspec(string nuspecPath, string packId, string version, string year)
{
    var text = File.ReadAllText(nuspecPath);
    // replace ID <id>OpenStudio</id> with <id>NREL.OpenStudio.win</id>
    text = Regex.Replace(text, @"<id>\w*</id>", $"<id>{packId}</id>", RegexOptions.IgnoreCase);
    text = Regex.Replace(text, @"<version>\d+.\d+.\d+</version>", $"<version>{version}</version>", RegexOptions.IgnoreCase);
    // remove <group targetFramework=".NETFramework4.5" />
    text = Regex.Replace(text, @"<group(\s|\S)*4.5(\S|\s)*>", string.Empty, RegexOptions.IgnoreCase);
    // update copyright year
    text = Regex.Replace(text, @"2018(\s|\S)*\d{4}", $"2018-{year}", RegexOptions.IgnoreCase);
    File.WriteAllText(nuspecPath, text);
}


