using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LoadUI : MonoBehaviour
{
    [Header("로딩 화면 UI")]
    [SerializeField] private UnityEngine.UI.Slider progressBar; // 로딩 진행률 표시 바
    [SerializeField] private TMPro.TMP_Text progressText; // 로딩 진행률 텍스트

    public void SetProgress(float progress)
    {
        progressBar.value = progress;
        progressText.text = $"{(int)(progress * 100)}%";
    }
}
