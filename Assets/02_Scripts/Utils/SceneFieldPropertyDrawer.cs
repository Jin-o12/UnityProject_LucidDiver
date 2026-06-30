using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 인스펙터에서는 SceneAsset을 드래그 앤 드롭으로 받고, 
/// 런타임에는 Scene의 이름(string)을 반환하는 직렬화 클래스입니다
/// </summary>
[System.Serializable]
public class SceneField
{
    [SerializeField]
    private Object sceneAsset; // 에디터에서 드래그해 넣을 씬 파일
    
    [SerializeField]
    private string sceneName = ""; // 빌드 시 실제 사용될 씬 이름

    public string SceneName => sceneName;

    // 이 클래스를 string처럼 바로 사용할 수 있게 해주는 마법의 연산자
    public static implicit operator string(SceneField sceneField)
    {
        return sceneField.SceneName;
    }
}

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(SceneField))]
public class SceneFieldPropertyDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, GUIContent.none, property);
        
        SerializedProperty sceneAsset = property.FindPropertyRelative("sceneAsset");
        SerializedProperty sceneName = property.FindPropertyRelative("sceneName");
        
        position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);
        
        if (sceneAsset != null)
        {
            // 인스펙터에 SceneAsset만 넣을 수 있는 슬롯을 만듭니다.
            sceneAsset.objectReferenceValue = EditorGUI.ObjectField(position, sceneAsset.objectReferenceValue, typeof(SceneAsset), false);
            
            // 파일이 등록되면 그 파일의 이름을 문자열로 추출해서 몰래 저장해둡니다.
            if (sceneAsset.objectReferenceValue != null)
            {
                sceneName.stringValue = (sceneAsset.objectReferenceValue as SceneAsset).name;
            }
            else
            {
                sceneName.stringValue = "";
            }
        }
        EditorGUI.EndProperty();
    }
}
#endif