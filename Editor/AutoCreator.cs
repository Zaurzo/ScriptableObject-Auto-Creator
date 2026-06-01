using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace SOAutoCreator
{
    [InitializeOnLoad]
    static class AutoCreator
    {
        private static readonly Type ScriptableObjectType = typeof(ScriptableObject);

        static AutoCreator()
        {
            CreateMissingAssets();
        }

        [MenuItem("Assets/Scriptable Object/Auto Creator/Create Missing Assets")]
        private static void CreateMissingAssets(MenuCommand command = null)
        {
            HashSet<Type> existingAssetTypes = GetScriptableObjectTypes();

            bool refresh = false;

            int count = 0;

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
                    count++;
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

            if (command == null) return;

            string text;

            if (count == 1)
            {
                text = "Created 1 scriptable object with a type that has the AutoCreateAsset attribute.";
            }
            else
            {
                text = $"Created {count} scriptable objects with types that have the AutoCreateAsset attribute.";
            }

            EditorWindow.focusedWindow.ShowNotification(
                new GUIContent(text),
                2.0f
            );
        }

        /// <summary>
        /// Returns a hash set of all the unique types of scriptable objects in the asset database.
        /// </summary>
        private static HashSet<Type> GetScriptableObjectTypes()
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