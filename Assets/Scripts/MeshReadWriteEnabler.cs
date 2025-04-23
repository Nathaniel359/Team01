#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

// Helper to enable Read/Write on assets
public class MeshReadWriteEnabler : EditorWindow
{
    [MenuItem("Tools/Enable Read/Write on All Models")]
    static void EnableReadWriteOnAllMeshes()
    {
        string[] modelGUIDs = AssetDatabase.FindAssets("t:Model");

        int updatedCount = 0;

        foreach (string guid in modelGUIDs)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;

            if (importer != null && !importer.isReadable)
            {
                importer.isReadable = true;
                EditorUtility.SetDirty(importer);
                importer.SaveAndReimport();
                updatedCount++;
            }
        }

        Debug.Log($"Finished enabling Read/Write on {updatedCount} model(s).");
    }
}
#endif
