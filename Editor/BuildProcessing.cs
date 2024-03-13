using System;
using System.IO;
using Microsoft.Win32;
using UnityEngine;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

public class BuildProcessing : IPreprocessBuildWithReport, IPostprocessBuildWithReport
{
    public int callbackOrder => 0;

    [MenuItem("My Menu/QuickBuild")]
    static void QuickBuildMenuItem()
    {
        string buildPath = Path.Combine(Application.dataPath, "../Builds/");
        if (!Directory.Exists(buildPath))
        {
            Directory.CreateDirectory(buildPath);
        }

        string exePath = Path.Combine(buildPath, Application.productName + ".exe");
        BuildPipeline.BuildPlayer(EditorBuildSettings.scenes, exePath, EditorUserBuildSettings.activeBuildTarget,
            BuildOptions.None);
    }

    public void OnPreprocessBuild(BuildReport report)
    {
        IncrementBuildVersion();
    }

    public void OnPostprocessBuild(BuildReport report)
    {
        if (report.summary.result == BuildResult.Failed)
        {
            return;
        }

        var path = report.summary.outputPath;
        ExportVersionFile(path);
        ExportCompanyNameFile(path);
        ExportProductNameFile(path);

        if (report.summary.platformGroup == BuildTargetGroup.Standalone)
        {
            if (EditorUtility.DisplayDialog(
                    "Create Installer",
                    "Would you like to create an installer?",
                    "Yes",
                    "No"))
            {
                RunInstallerCompiler();
            }

            {
            }
            RunInstallerCompiler();
        }
    }

    void IncrementBuildVersion()
    {
        Version version;
        if (Version.TryParse(PlayerSettings.bundleVersion, out version))
        {
            version = new Version(version.Major, version.Minor, version.Build + 1);
            PlayerSettings.bundleVersion = version.ToString();
        }
        else
        {
            version = new Version(0, 0, 1);
        }
    }

    void ExportVersionFile(string outputPath)
    {
        string buildpath = new FileInfo(outputPath).Directory.FullName;
        string versionPath = Path.Combine(buildpath, "Version.txt");
        File.WriteAllText(versionPath, PlayerSettings.bundleVersion);
    }
    void ExportCompanyNameFile(string outputPath)
    {
        string buildpath = new FileInfo(outputPath).Directory.FullName;
        string versionPath = Path.Combine(buildpath, "CompanyName.txt");
        File.WriteAllText(versionPath, PlayerSettings.companyName);
    }
    void ExportProductNameFile(string outputPath)
    {
        string buildpath = new FileInfo(outputPath).Directory.FullName;
        string versionPath = Path.Combine(buildpath, "ProductName.txt");
        File.WriteAllText(versionPath, PlayerSettings.productName);
    }
    

    void RunInstallerCompiler()
    {
        if (TryGetInnoSetupCompilerPath(out string innoCompilerPath))
        {
            string installationPath = Path.Combine(Application.dataPath, "../Installers/");
            string installerPath = Path.Combine(installationPath, "installer.iss");

            if (!File.Exists(installerPath))
            {
                CreateInstallerFolder(installationPath);
            }
            
            if (File.Exists(innoCompilerPath) && File.Exists(installerPath))
            {
                System.Diagnostics.Process process = new System.Diagnostics.Process();
                process.StartInfo.FileName = innoCompilerPath;
                process.StartInfo.Arguments = @"/cc " + installerPath;
                process.Start();
            }
            
        }
    }

    private void CreateInstallerFolder(string installationPath)
    {
        Directory.CreateDirectory(installationPath);
        string installerPath = Path.Combine(installationPath, "installer.iss");
        File.WriteAllText(installerPath, "content");
    }

    bool TryGetInnoSetupCompilerPath(out string path)
    {
        RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Classes\InnoSetupScriptFile\shell\open\command");
        if (key != null)
        {
            string value = key.GetValue("") as string;
            foreach (var entry in value.Split('\"'))
            {
                if (entry.EndsWith(".exe"))
                {
                    path = entry;
                    return true;
                }
            }
        }

        Debug.LogError("No Inno Setup Compiler found");
        path = null;
        return false;
    }
}