using System;
using System.Drawing;
using System.IO;
using Microsoft.Win32;
using Unity.VisualScripting;
using UnityEngine;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

public class BuildProcessing : IPreprocessBuildWithReport, IPostprocessBuildWithReport
{
    public int callbackOrder => 0;


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

        if (!InstallerCreationSetup.IsSetup())
        {
            return;
        }

        var path = report.summary.outputPath;

        InstallerCreationSetup.ExportInstallerInfo(path);

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


    void RunInstallerCompiler()
    {
        if (TryGetInnoSetupCompilerPath(out string innoCompilerPath))
        {
            string installationPath = Path.Combine(Application.dataPath, "../Installers/");
            string installerPath = Path.Combine(installationPath, "installer.iss");

            if (!File.Exists(installerPath))
            {
                if (EditorUtility.DisplayDialog("Inno setup not found",
                        "Have you setup the Inno installer script, if not would you like to create one?",
                        "Yes",
                        "No"))
                {
                    InstallerCreationSetup.CopyTempletFolderToInstallerOutputFolder();
                }
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

    bool TryGetInnoSetupCompilerPath(out string path)
    {
        Microsoft.Win32.RegistryKey key =
            Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Classes\InnoSetupScriptFile\shell\open\command");
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

public class InstallerCreationSetup
{
    [MenuItem("Dragyn Games/TestExport")]
    static void testExport()
    {
        ExportInstallerInfo(Path.Combine(Application.dataPath, "../Installers"));
    }

    public static void ExportInstallerInfo(string path)
    {
        ExportTextFile(new ExportData()
            {path = path, fileName = "Version.txt", content = PlayerSettings.bundleVersion});
        ExportTextFile(new ExportData()
            {path = path, fileName = "CompanyName.txt", content = PlayerSettings.companyName});
        ExportTextFile(new ExportData()
            {path = path, fileName = "ProductName.txt", content = PlayerSettings.productName});
        ExportTextFile(new ExportData()
            {path = path, fileName = "ApplicationID.txt", content = PlayerSettings.applicationIdentifier});


        CopyTempletFolderToInstallerOutputFolder();
    }

    public static void CopyTempletFolderToInstallerOutputFolder()
    {
        var resourcesPath = "Packages/com.dragyngames.installercreationpackage/InstallerTemplate/Installers";
        string targetPath = Path.Combine(Application.dataPath, "../Installers");
        CopyDirectory(resourcesPath, targetPath, true);
    }

    static void ExportTextFile(ExportData exportData)
    {
        string buildpath = new FileInfo(exportData.path).Directory.FullName;
        string fullPath = Path.Combine(buildpath, exportData.fileName);
        if (!File.Exists(fullPath))
        {
            File.Create(fullPath).Close();
        }
        File.WriteAllText(fullPath, exportData.content);
    }

    void ExportVersionFile(string outputPath)
    {
        string buildpath = new FileInfo(outputPath).Directory.FullName;
        string versionPath = Path.Combine(buildpath, "Version.txt");
        File.WriteAllText(versionPath, PlayerSettings.bundleVersion);
    }

    public static bool IsSetup()
    {
        bool isSetup = true;
        var verFilePath = Path.Combine(InstallerCreationMenu.buildPath, "Version.txt");
        if (!File.Exists(verFilePath))
        {
            isSetup = false;
        }

        var compNameFilePath = Path.Combine(InstallerCreationMenu.buildPath, "CompanyName.txt");
        if (!File.Exists(compNameFilePath))
        {
            isSetup = false;
        }

        var prodNameFilePath = Path.Combine(InstallerCreationMenu.buildPath, "ProductName.txt");
        if (!File.Exists(prodNameFilePath))
        {
            isSetup = false;
        }

        var appIDFilePath = Path.Combine(InstallerCreationMenu.buildPath, "ApplicationID.txt");
        if (!File.Exists(appIDFilePath))
        {
            isSetup = false;
        }

        var iconFilePath = Path.Combine(InstallerCreationMenu.buildPath, "Resources/SetupIcon.ico");


        return isSetup;
    }

    void SetupNeededImages(string installationPath)
    {
        var iconStringPath = "Packages/com.dragyngames.installercreationpackage/InstallerTemplate/SetupIcon.png";
        //Load icon file from package and copy it to the installer folder
        var iconAsset = AssetDatabase.LoadAssetAtPath<Texture2D>(iconStringPath);
        var iconPath = Path.Combine(installationPath, "SetupIcon.ico");
        Texture2D uncompressedTexture = new Texture2D(iconAsset.width, iconAsset.height, TextureFormat.RGBA32, false);
        uncompressedTexture.SetPixels(iconAsset.GetPixels());
        uncompressedTexture.Apply();
        File.WriteAllBytes(iconPath, uncompressedTexture.EncodeToPNG());
    }

    static void CopyDirectory(string sourceDir, string destinationDir, bool copySubDirs)
    {
        DirectoryInfo dir = new DirectoryInfo(sourceDir);

        if (!dir.Exists)
        {
            throw new DirectoryNotFoundException("Source directory does not exist or could not be found: " + sourceDir);
        }

        DirectoryInfo[] dirs = dir.GetDirectories();
        Directory.CreateDirectory(destinationDir);

        foreach (FileInfo file in dir.GetFiles())
        {
            if (!file.Extension.Equals(".meta", StringComparison.OrdinalIgnoreCase))
            {
                string temppath = Path.Combine(destinationDir, file.Name);
                try
                {
                    file.CopyTo(temppath, false);
                }
                catch (Exception e)
                {
                    //EditorUtility.DisplayDialog() if exception is file already exists
                    Debug.LogError("Failed to copy files: " + e.Message);
                }
            }
        }

        if (copySubDirs)
        {
            foreach (DirectoryInfo subdir in dirs)
            {
                string temppath = Path.Combine(destinationDir, subdir.Name);
                CopyDirectory(subdir.FullName, temppath, copySubDirs);
            }
        }
    }
}

public class InstallerCreationMenu
{
    public static string buildPath => Path.Combine(Application.dataPath, "../Builds/");

    [MenuItem("Dragyn Games/QuickBuild/QuickBuild")]
    static void QuickBuildMenuItem()
    {
        if (!Directory.Exists(buildPath))
        {
            Directory.CreateDirectory(buildPath);
        }

        string exePath = Path.Combine(buildPath, Application.productName + ".exe");
        BuildPipeline.BuildPlayer(EditorBuildSettings.scenes, exePath, EditorUserBuildSettings.activeBuildTarget,
            BuildOptions.None);
    }

    [MenuItem("Dragyn Games/QuickBuild/SetupInstaller")]
    static void SetupInstaller()
    {
        InstallerCreationSetup.ExportInstallerInfo(buildPath);
    }
}

public struct ExportData
{
    public string path;
    public string fileName;
    public string content;
}