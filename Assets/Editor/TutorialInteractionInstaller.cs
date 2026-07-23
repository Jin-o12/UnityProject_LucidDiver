using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// TutorialScene에 남겨 둔 스폰 풀 위치를 기준으로 튜토리얼 트리거와 이벤트 브리지를 배치합니다.
/// </summary>
public static class TutorialInteractionInstaller
{
    private const string TutorialScenePath = "Assets/01_Scenes/TutorialScene.unity";
    private const string TriggerPrefabPath = "Assets/03_PreFabs/Tutorial/TutorialTrigger.prefab";
    private const string CatalogPath = "Assets/Resources/Tutorial/TutorialMessageCatalog.asset";

    [MenuItem("Tools/Lucid Diver/Tutorial/Install Tutorial Interactions")]
    public static void InstallTutorialInteractions()
    {
        Scene scene = SceneManager.GetSceneByPath(TutorialScenePath);
        bool openedForInstall = !scene.IsValid() || !scene.isLoaded;
        if (openedForInstall)
            scene = EditorSceneManager.OpenScene(TutorialScenePath, OpenSceneMode.Additive);

        Transform playerSpawn = FindTransform(scene, "PlayerSpawnPoint");
        Transform boxPool = FindTransform(scene, "BoxSpawnPointPool");
        Transform enemyPool = FindTransform(scene, "EnemySpawnPointPool");
        Transform patrolPool = FindTransform(scene, "EnemyPatrolRoutePool");
        Transform tutorialSystem = FindTransform(scene, "TutorialSystem");

        Transform boxPoint = FindFirstChildPoint(boxPool, "BoxSpawnPoint");
        Transform enemyPoint = FindFirstChildPoint(enemyPool, "Spawn_");
        Transform patrolRoute = FindFirstChildPoint(patrolPool, "Route_");

        if (playerSpawn == null || boxPoint == null || enemyPoint == null || patrolRoute == null || tutorialSystem == null)
        {
            Debug.LogError("[TutorialInstaller] 필요한 플레이어/상자/에너미/순찰 루트/TutorialSystem 오브젝트를 찾지 못했습니다.");
            if (openedForInstall)
                EditorSceneManager.CloseScene(scene, true);
            return;
        }

        if (tutorialSystem.GetComponent<TutorialEventBridge>() == null)
            tutorialSystem.gameObject.AddComponent<TutorialEventBridge>();

        Transform triggerGroup = FindTransform(scene, "TutorialTriggers");
        if (triggerGroup == null)
        {
            GameObject group = new GameObject("TutorialTriggers");
            SceneManager.MoveGameObjectToScene(group, scene);
            triggerGroup = group.transform;
        }

        Vector3 playerToBox = FlatDirection(playerSpawn.position, boxPoint.position, playerSpawn.forward);
        Vector3 boxToEnemy = FlatDirection(boxPoint.position, enemyPoint.position, playerToBox);

        ConfigureTrigger(scene, triggerGroup, "Trigger_Move", "TUTORIAL_MOVE_001",
            playerSpawn.position + playerToBox * 4f, playerToBox);
        ConfigureTrigger(scene, triggerGroup, "Trigger_Interact", "TUTORIAL_INTERACT_001",
            boxPoint.position - playerToBox * 4f, playerToBox);
        ConfigureTrigger(scene, triggerGroup, "Trigger_Attack", "TUTORIAL_ATTACK_001",
            enemyPoint.position - boxToEnemy * 5f, boxToEnemy);
        ConfigureCompletionTrigger(scene, triggerGroup, "Trigger_TutorialComplete",
            patrolRoute.position + boxToEnemy * 6f, boxToEnemy);

        ConfigureItemTutorialMessage();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();

        Debug.Log(
            $"[TutorialInstaller] TutorialScene 연결 완료 - Box={GetPath(boxPoint)}, " +
            $"Enemy={GetPath(enemyPoint)}, Patrol={GetPath(patrolRoute)}");

        if (openedForInstall)
            EditorSceneManager.CloseScene(scene, true);
    }

    private static void ConfigureTrigger(
        Scene scene,
        Transform parent,
        string objectName,
        string tutorialId,
        Vector3 position,
        Vector3 forward)
    {
        Transform existing = FindTransform(scene, objectName);
        GameObject triggerObject;
        if (existing != null)
        {
            triggerObject = existing.gameObject;
            triggerObject.transform.SetParent(parent, true);
        }
        else
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(TriggerPrefabPath);
            triggerObject = prefab != null
                ? (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent)
                : new GameObject(objectName, typeof(BoxCollider), typeof(TutorialTrigger));
            triggerObject.name = objectName;
            if (prefab == null)
                SceneManager.MoveGameObjectToScene(triggerObject, scene);
        }

        triggerObject.transform.position = new Vector3(position.x, position.y + 1f, position.z);
        triggerObject.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);

        BoxCollider collider = triggerObject.GetComponent<BoxCollider>();
        collider.isTrigger = true;
        collider.size = new Vector3(4f, 2f, 1.5f);

        TutorialTrigger trigger = triggerObject.GetComponent<TutorialTrigger>();
        SerializedObject serializedTrigger = new SerializedObject(trigger);
        serializedTrigger.FindProperty("tutorialId").stringValue = tutorialId;
        serializedTrigger.FindProperty("triggerOnce").boolValue = true;
        serializedTrigger.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(triggerObject);
    }

    private static void ConfigureCompletionTrigger(
        Scene scene,
        Transform parent,
        string objectName,
        Vector3 position,
        Vector3 forward)
    {
        Transform existing = FindTransform(scene, objectName);
        GameObject triggerObject;
        if (existing != null)
        {
            triggerObject = existing.gameObject;
            triggerObject.transform.SetParent(parent, true);
        }
        else
        {
            triggerObject = new GameObject(objectName, typeof(BoxCollider), typeof(TutorialCompletionTrigger));
            triggerObject.transform.SetParent(parent, true);
            SceneManager.MoveGameObjectToScene(triggerObject, scene);
        }

        triggerObject.transform.position = new Vector3(position.x, position.y + 1f, position.z);
        triggerObject.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);

        BoxCollider collider = triggerObject.GetComponent<BoxCollider>();
        collider.isTrigger = true;
        collider.size = new Vector3(4f, 2f, 1.5f);

        TutorialCompletionTrigger completionTrigger = triggerObject.GetComponent<TutorialCompletionTrigger>();
        SerializedObject serializedTrigger = new SerializedObject(completionTrigger);
        serializedTrigger.FindProperty("triggerOnce").boolValue = true;
        serializedTrigger.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(triggerObject);
    }

    private static void ConfigureItemTutorialMessage()
    {
        TutorialMessageCatalog catalog = AssetDatabase.LoadAssetAtPath<TutorialMessageCatalog>(CatalogPath);
        if (catalog == null)
            return;

        SerializedObject serializedCatalog = new SerializedObject(catalog);
        SerializedProperty entries = serializedCatalog.FindProperty("entries");
        for (int index = 0; index < entries.arraySize; index++)
        {
            SerializedProperty entry = entries.GetArrayElementAtIndex(index);
            if (entry.FindPropertyRelative("tutorialId").stringValue != "TUTORIAL_ITEM_001")
                continue;

            entry.FindPropertyRelative("message").stringValue =
                "상자 슬롯의 아이템을 선택하여 인벤토리로 옮겨 보세요.";
            // Chest UI가 이미 UI 입력 맵을 사용하므로 이 안내는 입력 상태와 시간을 다시 바꾸지 않습니다.
            entry.FindPropertyRelative("pauseGame").boolValue = false;
            serializedCatalog.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);
            return;
        }
    }

    private static Vector3 FlatDirection(Vector3 from, Vector3 to, Vector3 fallback)
    {
        Vector3 direction = to - from;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.001f)
            return fallback.sqrMagnitude > 0.001f ? fallback.normalized : Vector3.forward;
        return direction.normalized;
    }

    private static Transform FindFirstChildPoint(Transform root, string namePart)
    {
        return root == null
            ? null
            : root.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(candidate => candidate != root && candidate.name.Contains(namePart));
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

    private static string GetPath(Transform transform)
    {
        string path = transform.name;
        while (transform.parent != null)
        {
            transform = transform.parent;
            path = transform.name + "/" + path;
        }
        return path;
    }
}
