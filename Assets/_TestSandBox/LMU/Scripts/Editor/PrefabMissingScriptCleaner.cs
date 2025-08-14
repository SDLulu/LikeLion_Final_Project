using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 프로젝트 내 모든 프리팹을 검사하여 끊어진(실종된) 스크립트 컴포넌트를 제거하는 도구.
/// </summary>
public static class PrefabMissingScriptCleaner
{
    [MenuItem("에디터툴/편의기능/프리팹 Missing 스크립트 제거")]
    private static void RemoveMissingScriptsInAllPrefabs()
    {
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab");
        if (prefabGuids == null || prefabGuids.Length == 0)
        {
            EditorUtility.DisplayDialog("Missing Script Cleaner", "프로젝트에서 프리팹을 찾을 수 없습니다.", "확인");
            return;
        }

        int totalPrefabs = prefabGuids.Length;
        int processedPrefabs = 0;
        int totalRemovedComponents = 0;

        try
        {
            for (int i = 0; i < totalPrefabs; i++)
            {
                string guid = prefabGuids[i];
                string path = AssetDatabase.GUIDToAssetPath(guid);

                if (string.IsNullOrEmpty(path))
                {
                    continue;
                }

                float progress = (float)i / Math.Max(1, totalPrefabs);
                EditorUtility.DisplayProgressBar("Missing Script Cleaner", path, progress);

                GameObject root = PrefabUtility.LoadPrefabContents(path);
                if (root == null)
                {
                    continue;
                }

                bool modified = false;
                int removedInThisPrefab = 0;

                foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                {
                    GameObject go = t.gameObject;
                    int missingCount = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go);
                    if (missingCount > 0)
                    {
                        GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
                        removedInThisPrefab += missingCount;
                        modified = true;
                    }
                }

                if (modified)
                {
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                    processedPrefabs += 1;
                    totalRemovedComponents += removedInThisPrefab;
                }

                PrefabUtility.UnloadPrefabContents(root);
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        string message = $"검사한 프리팹: {totalPrefabs}\n수정된 프리팹: {processedPrefabs}\n제거된 Missing Script 수: {totalRemovedComponents}";
        EditorUtility.DisplayDialog("Missing Script Cleaner", message, "확인");
        Debug.Log(message);
    }
}


