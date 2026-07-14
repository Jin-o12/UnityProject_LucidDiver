using System.Linq;
using Unity.AI.Navigation;
using Unity.AI.Navigation.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

/// <summary>
/// TutorialScene의 도로 계층만 NavMesh 베이크 대상으로 분리합니다.
/// 씬을 다시 정리하더라도 같은 메뉴를 실행하면 동일한 설정으로 재베이크할 수 있습니다.
/// </summary>
public static class TutorialNavMeshBuilder
{
    private const string TutorialScenePath = "Assets/01_Scenes/TutorialScene.unity";

    private static Scene pendingScene;
    private static NavMeshSurface pendingSurface;
    private static bool closeSceneAfterBake;

    [MenuItem("Tools/Lucid Diver/Tutorial/Bake Tutorial Road NavMesh")]
    public static void BakeTutorialRoadNavMesh()
    {
        Scene scene = SceneManager.GetSceneByPath(TutorialScenePath);
        bool openedForBake = !scene.IsValid() || !scene.isLoaded;
        if (openedForBake)
            scene = EditorSceneManager.OpenScene(TutorialScenePath, OpenSceneMode.Additive);

        Transform roadRoot = FindTransform(scene, "Road");
        Transform navigationRoot = FindTransform(scene, "TutorialNavMesh");
        if (navigationRoot == null)
        {
            GameObject navigationObject = new GameObject("TutorialNavMesh");
            SceneManager.MoveGameObjectToScene(navigationObject, scene);
            navigationRoot = navigationObject.transform;
        }

        NavMeshSurface surface = navigationRoot.GetComponent<NavMeshSurface>();
        if (surface == null)
            surface = navigationRoot.gameObject.AddComponent<NavMeshSurface>();

        if (roadRoot == null)
        {
            if (openedForBake)
                EditorSceneManager.CloseScene(scene, true);

            Debug.LogError("[TutorialNavMesh] TutorialScene에서 Road 계층을 찾지 못했습니다.");
            return;
        }

        // Surface 아래에 Road를 두고 현재 계층만 수집해 다른 씬과 공용 레이어에 영향을 주지 않습니다.
        roadRoot.SetParent(surface.transform, true);

        // 복제한 도로 프리팹에 남아 있는 개별 Surface는 중복 NavMesh를 등록하므로 이 씬에서만 비활성화합니다.
        foreach (NavMeshSurface nestedSurface in roadRoot.GetComponentsInChildren<NavMeshSurface>(true))
        {
            nestedSurface.RemoveData();
            nestedSurface.enabled = false;
            EditorUtility.SetDirty(nestedSurface);
        }

        surface.collectObjects = CollectObjects.Children;
        surface.layerMask = ~0;
        surface.useGeometry = NavMeshCollectGeometry.RenderMeshes;
        surface.defaultArea = NavMesh.GetAreaFromName("Walkable");

        EditorUtility.SetDirty(surface);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        pendingScene = scene;
        pendingSurface = surface;
        closeSceneAfterBake = openedForBake;
        EditorApplication.update -= FinishBakeWhenAssetIsReady;
        EditorApplication.update += FinishBakeWhenAssetIsReady;

        // Inspector의 Bake 버튼과 같은 경로를 사용해 TutorialScene 전용 NavMeshData 에셋을 만듭니다.
        NavMeshAssetManager.instance.StartBakingSurfaces(new Object[] { surface });
        Debug.Log("[TutorialNavMesh] TutorialScene 도로 NavMesh 베이크를 시작했습니다.");
    }

    private static void FinishBakeWhenAssetIsReady()
    {
        if (pendingSurface == null || pendingSurface.navMeshData == null)
            return;

        string assetPath = AssetDatabase.GetAssetPath(pendingSurface.navMeshData);
        if (string.IsNullOrEmpty(assetPath))
            return;

        EditorApplication.update -= FinishBakeWhenAssetIsReady;
        EditorUtility.SetDirty(pendingSurface);
        EditorSceneManager.MarkSceneDirty(pendingScene);
        EditorSceneManager.SaveScene(pendingScene);
        AssetDatabase.SaveAssets();

        Debug.Log($"[TutorialNavMesh] TutorialScene 도로 NavMesh 베이크를 완료했습니다: {assetPath}");

        if (closeSceneAfterBake && pendingScene.IsValid() && pendingScene.isLoaded)
            EditorSceneManager.CloseScene(pendingScene, true);

        pendingSurface = null;
        pendingScene = default;
        closeSceneAfterBake = false;
    }

    private static Transform FindTransform(Scene scene, string objectName)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform match = root.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(candidate => candidate.name == objectName);
            if (match != null)
                return match;
        }

        return null;
    }

}
