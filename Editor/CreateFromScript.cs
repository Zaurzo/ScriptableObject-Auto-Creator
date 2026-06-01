using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.ProjectWindowCallback;
using UnityEngine;

namespace SOAutoCreator
{
    public static class CreateFromScript
    {
        private static readonly Type ScriptableObjectType = typeof(ScriptableObject);

        class CreateAndRenameAssetAction : EndNameEditAction
        {
            public Type assetType;

            public override void Action(int instanceId, string assetPath, string resourceFile)
            {
                if (!TryCreateInstance(assetType, assetPath, out ScriptableObject instance)) return;

                AssetDatabase.Refresh();
                EditorUtility.FocusProjectWindow();

                Selection.activeObject = instance;
            }
        }

        private static string GetFreeAssetPath(MonoScript script, string name)
        {
            string scriptPath = AssetDatabase.GetAssetPath(script);
            string folder = Path.GetDirectoryName(scriptPath);
            string assetPath = $"{folder}/{name}";

            // Check if an asset with that name already exists in the folder
            if (AssetDatabase.GetMainAssetTypeAtPath($"{assetPath}.asset") != null)
            {
                int attempt = 1;

                // Find free asset name
                while (AssetDatabase.GetMainAssetTypeAtPath($"{assetPath} {attempt}.asset") != null)
                {
                    attempt++;
                }

                assetPath = $"{assetPath} {attempt}";
            }

            return assetPath + ".asset";
        }

        private static bool TryCreateInstance(Type type, string assetPath, out ScriptableObject instance)
        {
            try
            {
                instance = ScriptableObject.CreateInstance(type);

                AssetDatabase.CreateAsset(instance, assetPath);

                return true;
            }
            catch (Exception e)
            {
                instance = null;

                Debug.LogError($"Could not create scriptable object: {e.Message}");
                
                return false;
            }
        }

        /// <summary>
        /// Creates a scriptable object for each script in the hash set.
        /// </summary>
        private static void CreateBatch(HashSet<MonoScript> scripts)
        {
            HashSet<ScriptableObject> createdAssets = new HashSet<ScriptableObject>();

            foreach (var script in scripts)
            {
                Type assetType = script.GetClass();

                string assetPath = GetFreeAssetPath(script, assetType.Name);

                if (TryCreateInstance(assetType, assetPath, out ScriptableObject instance))
                {
                    createdAssets.Add(instance);
                }
            }

            if (createdAssets.Count > 0)
            {
                AssetDatabase.Refresh();
                Selection.objects = createdAssets.ToArray();
            }
        }

        /// <summary>
        /// Returns a hash set of all ScriptableObject scripts that are selected in the project window.
        /// </summary>
        private static HashSet<MonoScript> GetSelectedScripts()
        {
            var selectedScripts = new HashSet<MonoScript>();

            foreach (var obj in Selection.objects)
            {
                var script = obj as MonoScript;

                if (script != null && ScriptableObjectType.IsAssignableFrom(script.GetClass()))
                {
                    selectedScripts.Add(script);
                }
            }

            return selectedScripts;
        }

        [MenuItem("Assets/Scriptable Object/Create From Selected Scripts")]
        private static void CreateFromSelectedScripts()
        {
            HashSet<MonoScript> selectedScripts = GetSelectedScripts();

            if (selectedScripts.Count > 1)
            {
                CreateBatch(selectedScripts);
                return;
            }

            var mainScript = selectedScripts.FirstOrDefault();
            if (mainScript == null) return;

            Type assetType = mainScript.GetClass();

            var action = ScriptableObject.CreateInstance<CreateAndRenameAssetAction>();
            action.assetType = assetType;

            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(
                0,
                action,
                GetFreeAssetPath(mainScript, assetType.Name),
                null,
                null
            );
        }
    }
}