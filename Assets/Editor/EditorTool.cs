using System.Collections.Generic;
using UnityEditor;
using System.IO;

[InitializeOnLoad]
public class EditorTool
{
    [MenuItem("Window/Kumamate/Update UnityPackage")]
    public static void UnityPackage()
    {
        var assetPaths = new List<string>();

        var frameworkPath = "Assets/Kumamate";
        CollectPathRecursive(frameworkPath, assetPaths);

        AssetDatabase.ExportPackage(assetPaths.ToArray(), "Kumamate.unitypackage", ExportPackageOptions.IncludeDependencies);
    }

    private static void CollectPathRecursive(string path, List<string> collectedPaths)
    {
        var filePaths = Directory.GetFiles(path);
        foreach (var filePath in filePaths)
        {
            collectedPaths.Add(filePath);
        }

        var modulePaths = Directory.GetDirectories(path);
        foreach (var folderPath in modulePaths)
        {
            CollectPathRecursive(folderPath, collectedPaths);
        }
    }
}