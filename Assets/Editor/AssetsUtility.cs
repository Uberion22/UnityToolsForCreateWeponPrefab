using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Windows;

public static class AssetsUtility
{
    private static readonly string PrefabSuffix = ".prefab";

    public static void CreateDirectoryFoldersByPathIfNotExists(string directoryPath)
    {
        var foldersToCreate = directoryPath.Split('/');
        var allCurrentDirectory = foldersToCreate.FirstOrDefault();
        for (int i = 0; i < foldersToCreate.Length - 1; i++)
        {
            var currentPath = allCurrentDirectory + "/" + foldersToCreate[i + 1];
            if (!Directory.Exists(currentPath))
            {
                AssetDatabase.CreateFolder(allCurrentDirectory, foldersToCreate[i + 1]);
            }

            allCurrentDirectory = currentPath;
        }
    }

    public static GameObject CreatePrefabAtDirectory(GameObject gameObject, string directoryPath)
    {
        CreateDirectoryFoldersByPathIfNotExists(directoryPath);
        string localPath = directoryPath + "/" + gameObject.name + PrefabSuffix;
        localPath = AssetDatabase.GenerateUniqueAssetPath(localPath);
        var savedPrefab = PrefabUtility.SaveAsPrefabAssetAndConnect(gameObject, localPath, InteractionMode.UserAction, out var prefabSuccess);
        
        if (prefabSuccess)
        {
            Debug.Log("Prefab was saved successfully");
            Object.DestroyImmediate(gameObject);
        }
        else
        {
            Debug.LogError("Prefab failed to save");
        }

        return savedPrefab;
    }

    public static T CreateAssetAtPath<T>(T asset, string assetName, string assetFolderPath) where T : Object
    {
        CreateDirectoryFoldersByPathIfNotExists(assetFolderPath);
        var assetPath = assetFolderPath + "/" + assetName;
        AssetDatabase.CreateAsset(asset, assetPath);
        var result = AssetDatabase.LoadAssetAtPath<T>(assetPath);

        return result;
    }

}
