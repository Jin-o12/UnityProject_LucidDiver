using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

/// <summary>
/// Resources/JSON/TutorialGuide.json 파일을 읽어 튜토리얼 안내 데이터를 제공합니다.
/// ExcelToJson 결과물을 그대로 읽는 용도입니다.
/// </summary>
public sealed class LocalJsonTutorialGuideRepository
{
    private const string ResourcePath = "JSON/TutorialGuide";

    public List<TutorialGuideData> LoadAll()
    {
        TextAsset jsonAsset = Resources.Load<TextAsset>(ResourcePath);
        if (jsonAsset == null)
        {
            Debug.LogWarning("[TutorialGuideRepository] TutorialGuide.json 파일을 찾을 수 없습니다. ScriptableObject 카탈로그를 대신 사용할 수 있습니다.");
            return new List<TutorialGuideData>();
        }

        List<TutorialGuideData> guides = JsonConvert.DeserializeObject<List<TutorialGuideData>>(jsonAsset.text);
        return guides ?? new List<TutorialGuideData>();
    }
}
