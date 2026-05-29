using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Zaurzo.ScriptableObjects
{
    [InitializeOnLoad]
    static class AutoCreator
    {
        private readonly static Type ScriptableObjectType = typeof(ScriptableObject);

        static AutoCreator()
        {
            Create();
        }

        [MenuItem("Assets/ScriptableObject/Run Auto Creator")]
        private static void Create()
        {
            HashSet<Type> existingAssetTypes = GetAllUniqueScriptableObjects();

            bool refresh = false;

            foreach (Type type in TypeCache.GetTypesWithAttribute<AutoCreateAssetAttribute>())
            {
                if (existingAssetTypes.Contains(type)) continue;
                if (!ScriptableObjectType.IsAssignableFrom(type)) continue;

                var attr = type.GetCustomAttribute<AutoCreateAssetAttribute>();

                if (type.GetCustomAttribute<CreateAssetMenuAttribute>() != null)
                {
                    Debug.LogWarning(
                        $"Scriptable object ({type.Name}) with AutoCreateAsset attribute also has CreateAssetMenu attribute. " +
                        "Auto-creation will be disabled in this case."
                    );

                    continue;
                }

                string assetName = attr.initialName ?? type.Name;
                string assetFolder;

                if (attr.initialFolder != null)
                {
                    assetFolder = $"Assets/{attr.initialFolder}";
                }
                else
                {
                    assetFolder = $"Assets";
                }

                string assetPath = $"{assetFolder}/{assetName}.asset";

                Type currentAssetType = AssetDatabase.GetMainAssetTypeAtPath(assetPath);

                // Safeguard
                if (currentAssetType != null && currentAssetType != type)
                {
                    Debug.LogError(
                        $"Could not auto-create scriptable object ({assetName}): " +
                        $"A different type of asset is already located at '{assetPath}'"
                    );

                    continue;
                }

                try
                {
                    if (!Directory.Exists(assetFolder))
                    {
                        Directory.CreateDirectory(assetFolder);
                    }

                    ScriptableObject obj = ScriptableObject.CreateInstance(type);
                    AssetDatabase.CreateAsset(obj, assetPath);

                    refresh = true;
                }
                catch (Exception e)
                {
                    Debug.LogError($"Could not auto-create scriptable object ({assetName}): {e.Message}");
                }
            }

            if (refresh)
            {
                AssetDatabase.Refresh();
            }
        }

        private static HashSet<Type> GetAllUniqueScriptableObjects()
        {
            var set = new HashSet<Type>();

            string[] assetGUIDs = AssetDatabase.FindAssets(
                "t:scriptableobject",
                new string[] { "Assets" }
            );

            if (assetGUIDs.Length < 1)
            {
                return set;
            }

            foreach (string guid in assetGUIDs)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);

                Type type = AssetDatabase.GetMainAssetTypeAtPath(path);

                if (!set.Contains(type))
                {
                    set.Add(type);
                }
            }

            return set;
        }
    }
}