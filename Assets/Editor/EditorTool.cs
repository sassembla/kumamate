using System.Collections.Generic;
using UnityEditor;
using System.IO;
using UnityEngine;
using Kumamate;

[InitializeOnLoad]
public class EditorTool
{
    [MenuItem("Window/Kumamate/Update UnityPackage")]
    public static void UnityPackage()
    {
        var assetPaths = new List<string>();

        var frameworkPath = "Assets/Kumamate";
        CollectPathRecursive(frameworkPath, assetPaths, new string[] { KumaConstants.STORAGE_PATH, KumaConstants.FONTS_PATH });

        foreach (var path in assetPaths)
        {
            Debug.Log("exporting path:" + path);
        }
        AssetDatabase.ExportPackage(assetPaths.ToArray(), "Kumamate.unitypackage", ExportPackageOptions.IncludeDependencies);
    }

    private static bool ShouldIgnore(string filePath, string[] ignoreFolderPaths)
    {
        foreach (var ignoreFolderPath in ignoreFolderPaths)
        {
            if (filePath.StartsWith(ignoreFolderPath))
            {
                return true;
            }
        }

        return false;
    }

    private static void CollectPathRecursive(string path, List<string> collectedPaths, string[] ignoreFolderPaths)
    {
        var filePaths = Directory.GetFiles(path);
        foreach (var filePath in filePaths)
        {
            if (ShouldIgnore(filePath, ignoreFolderPaths))
            {
                continue;
            }

            collectedPaths.Add(filePath);
        }

        var modulePaths = Directory.GetDirectories(path);
        foreach (var folderPath in modulePaths)
        {
            CollectPathRecursive(folderPath, collectedPaths, ignoreFolderPaths);
        }
    }
}