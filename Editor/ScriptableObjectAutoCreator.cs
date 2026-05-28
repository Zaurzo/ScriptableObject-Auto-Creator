using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace ZaurzoUtil
{
    [InitializeOnLoad]
    public class ScriptableObjectAutoCreator : UnityEditor.AssetModificationProcessor
    {
        private static List<AssetInfo> cachedAssetInfo = new List<AssetInfo>();

        private readonly struct AssetInfo
        {
            public readonly Type type;
            
            public readonly string path;
            public readonly bool movable;

            public AssetInfo(Type type, string path, bool movable)
            {
                this.type = type;
                this.path = path;
                this.movable = movable;
            }
        }

        static ScriptableObjectAutoCreator()
        {
            cachedAssetInfo.Clear();

            var existingAssets = new HashSet<Type>();
            {
                string[] assets = AssetDatabase.FindAssets(
                    "t:scriptableobject", 
                    new string[] { "Assets" }
                );

                foreach (string guid in assets)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    
                    Type type = AssetDatabase.GetMainAssetTypeAtPath(path);

                    if (type != null && !existingAssets.Contains(type))
                    {
                        existingAssets.Add(type);
                    }
                }
            }
            
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (Type type in assembly.GetTypes())
                {
                    if (!type.IsSubclassOf(typeof(ScriptableObject))) continue;

                    var attr = type.GetCustomAttribute<EnsureAssetExistsAttribute>();
                    if (attr == null) continue;

                    string assetFolder = attr.initialFolder;
                    string assetName = attr.initialName ?? type.Name;

                    if (assetFolder != null)
                    {
                        assetFolder = $"Assets/{assetFolder}";
                    }
                    else
                    {
                        assetFolder = "Assets";
                    }

                    var assetInfo = new AssetInfo(
                        type, 
                        $"{assetFolder}/{assetName}.asset",
                        attr.movable
                    );

                    cachedAssetInfo.Add(assetInfo);

                    if (type.GetCustomAttribute<CreateAssetMenuAttribute>() != null && !existingAssets.Contains(type))
                    {
                        Debug.LogWarning($"ScriptableObject ({type.Name}) with EnsureAssetExists attribute also has CreateAssetMenu attribute. EnsureAssetExists attribute will be disabled in this case.");
                    }
                }
            }

            if (cachedAssetInfo.Count < 1) return;

            bool refresh = false;

            foreach (AssetInfo asset in cachedAssetInfo)
            {
                if (existingAssets.Contains(asset.type)) continue;

                try
                {
                    string directoryPath = Path.GetDirectoryName(asset.path);

                    if (!Directory.Exists(directoryPath))
                    {
                        Directory.CreateDirectory(directoryPath);
                    }

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

        private static bool TryGetAssetInfo(string assetPath, out AssetInfo? assetInfo)
        {
            Type type = AssetDatabase.GetMainAssetTypeAtPath(assetPath);

            foreach (AssetInfo info in cachedAssetInfo)
            {
                if (type == info.type)
                {
                    assetInfo = info;

                    return true;
                }
            }

            assetInfo = null;

            return false;
        }

        private static AssetDeleteResult OnWillDeleteAsset(string assetPath, RemoveAssetOptions _)
        {
            if (TryGetAssetInfo(assetPath, out var _))
            {
                EditorUtility.DisplayDialog(
                    "Deletion Failed",
                    "This scriptable object may not be deleted as it is protected by the EnsureAssetExists attribute.",
                    "Ok"
                );

                return AssetDeleteResult.FailedDelete;
            }

            return AssetDeleteResult.DidNotDelete;
        }

        private static AssetMoveResult OnWillMoveAsset(string sourcePath, string _)
        {
            if (TryGetAssetInfo(sourcePath, out var assetInfo) && !assetInfo.Value.movable)
            {
                EditorUtility.DisplayDialog(
                    "Move Failed",
                    "This scriptable object may not be moved as it is protected by the EnsureAssetExists attribute.",
                    "Ok"
                );

                return AssetMoveResult.FailedMove;
            }

            return AssetMoveResult.DidNotMove;
        }
    }
}