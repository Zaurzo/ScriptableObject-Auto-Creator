using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public class ScriptableObjectAutoCreator : UnityEditor.AssetModificationProcessor
{
    private static List<SObjectAsset> sObjectAssets = new List<SObjectAsset>();

    struct SObjectAsset
    {
        public Type type;
        
        public string folder;
        public string name;
        public string path;
    }

    static ScriptableObjectAutoCreator()
    {
        sObjectAssets.Clear();
        
        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            foreach (Type type in assembly.GetTypes())
            {
                if (!type.IsSubclassOf(typeof(ScriptableObject))) continue;
                if (type.GetCustomAttribute<CreateAssetMenuAttribute>() != null) continue;
                
                var attr = type.GetCustomAttribute<EnsureAssetExistsAttribute>();
                if (attr == null) continue;

                var asset = new SObjectAsset()
                {
                    type = type,
                    folder = $"Assets/{attr.AssetFolder}",
                    name = attr.AssetName ?? type.Name
                };

                asset.path = $"{asset.folder}/{asset.name}.asset";

                sObjectAssets.Add(asset);
            }
        }

        if (sObjectAssets.Count < 1) return;

        bool refresh = false;

        foreach (SObjectAsset asset in sObjectAssets)
        {
            if (AssetExists(asset.name, asset.folder)) continue;

            try
            {
                ScriptableObject obj = ScriptableObject.CreateInstance(asset.type);
                AssetDatabase.CreateAsset(obj, asset.path);

                refresh = true;
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
            }
        }

        if (refresh)
        {
            AssetDatabase.Refresh();
        }
    }

    // Prevent ScriptableObjects that have the EnsureAssetExists attribute from being deleted
    private static AssetDeleteResult OnWillDeleteAsset(string assetPath, RemoveAssetOptions options)
    {
        foreach (SObjectAsset asset in sObjectAssets)
        {
            if (asset.path != assetPath) continue;

            EditorUtility.DisplayDialog(
                "Deletion Blocked",
                "This scriptable object may not be deleted as it is protected by the EnsureAssetExists attribute.",
                "Ok"
            );

            return AssetDeleteResult.FailedDelete;
        }

        return AssetDeleteResult.DidNotDelete;
    }

    // "Pollyfill" for AssetDatabase.AssetExists (added in Unity 6)
    private static bool AssetExists(string name, string folder)
    {
        var assets = AssetDatabase.FindAssets(
            name, 
            new string[] { folder }
        );

        return assets.Length > 0;
    }
}
