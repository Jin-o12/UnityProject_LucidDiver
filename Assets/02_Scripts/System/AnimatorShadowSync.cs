using System.Collections;
using System.Collections.Generic;
using AnyPortrait;
using UnityEngine;

[DefaultExecutionOrder(1000)]
public class AnimatorShadowSync : MonoBehaviour
{
    [Header("Animator References")]
    [SerializeField] private Animator sourceAnimator;
    [SerializeField] private Animator shadowAnimator;

    [Header("Sync Options")]
    [SerializeField] private bool syncLayerWeights = true;

    [Header("Shadow Visibility")]
    [SerializeField] private apPortrait shadowPortrait;
    [SerializeField, Range(0f, 1f)] private float targetShadowAlpha = 0f;
    [SerializeField] private float shadowFadeTime = 0.5f;

    private AnimatorControllerParameter[] parameters;
    private float currentShadowAlpha = 1f;

    private void Awake()
    {
        if (sourceAnimator == null || shadowAnimator == null)
        {
            enabled = false;
            Debug.LogError("AnimatorShadowSync: 원본 또는 그림자 Animator가 연결되지 않았습니다.");
            return;
        }

        // {원본 Animator의 파라미터 목록을 캐싱한다}
        parameters = sourceAnimator.parameters;

        // {그림자 Animator가 화면 컬링과 관계없이 계속 갱신되도록 설정한다}
        shadowAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

        // {그림자 복제본의 루트 모션 적용을 방지한다}
        shadowAnimator.applyRootMotion = false;

        // {원본과 그림자 Animator의 업데이트 방식을 동일하게 맞춘다}
        shadowAnimator.updateMode = sourceAnimator.updateMode;

        // {원본과 그림자가 동일한 Animator Controller를 사용하도록 맞춘다}
        if (shadowAnimator.runtimeAnimatorController != sourceAnimator.runtimeAnimatorController)
        {
            shadowAnimator.runtimeAnimatorController =
                sourceAnimator.runtimeAnimatorController;
        }

        // {변경된 Animator 설정과 초기 포즈를 즉시 반영한다}
        shadowAnimator.Rebind();
        shadowAnimator.Update(0f);

        ResolveShadowPortrait();
        currentShadowAlpha = targetShadowAlpha;
        ApplyShadowAlpha(currentShadowAlpha);
    }

    private void Update()
    {
        if (sourceAnimator == null || shadowAnimator == null)
            return;

        // {원본 Animator의 재생 속도를 그림자 Animator에 동기화한다}
        shadowAnimator.speed = sourceAnimator.speed;

        for (int i = 0; i < parameters.Length; i++)
        {
            AnimatorControllerParameter parameter = parameters[i];

            switch (parameter.type)
            {
                case AnimatorControllerParameterType.Float:
                    // {이동 속도와 방향 등에 사용하는 Float 파라미터를 동기화한다}
                    shadowAnimator.SetFloat(
                        parameter.nameHash,
                        sourceAnimator.GetFloat(parameter.nameHash));
                    break;

                case AnimatorControllerParameterType.Int:
                    // {방향 번호와 상태 번호 등에 사용하는 Int 파라미터를 동기화한다}
                    shadowAnimator.SetInteger(
                        parameter.nameHash,
                        sourceAnimator.GetInteger(parameter.nameHash));
                    break;

                case AnimatorControllerParameterType.Bool:
                    // {이동 여부와 공격 여부 등에 사용하는 Bool 파라미터를 동기화한다}
                    shadowAnimator.SetBool(
                        parameter.nameHash,
                        sourceAnimator.GetBool(parameter.nameHash));
                    break;

                case AnimatorControllerParameterType.Trigger:
                    // {Trigger는 원본 Animator에서 소비될 수 있어 자동 복사에서 제외한다}
                    break;
            }
        }

        if (syncLayerWeights)
        {
            int layerCount = Mathf.Min(
                sourceAnimator.layerCount,
                shadowAnimator.layerCount);

            for (int i = 0; i < layerCount; i++)
            {
                // {상체 공격 레이어 등 Animator Layer의 가중치를 동기화한다}
                shadowAnimator.SetLayerWeight(
                    i,
                    sourceAnimator.GetLayerWeight(i));
            }
        }

        UpdateShadowVisibility();
    }

    public void SetTrigger(string triggerName)
    {
        if (sourceAnimator == null || shadowAnimator == null)
            return;

        // {Trigger 파라미터를 원본과 그림자 Animator에 동시에 전달한다}
        sourceAnimator.SetTrigger(triggerName);
        shadowAnimator.SetTrigger(triggerName);
    }

    public void ResetTrigger(string triggerName)
    {
        if (sourceAnimator == null || shadowAnimator == null)
            return;

        // {Trigger 파라미터를 원본과 그림자 Animator에서 동시에 초기화한다}
        sourceAnimator.ResetTrigger(triggerName);
        shadowAnimator.ResetTrigger(triggerName);
    }

    private void ResolveShadowPortrait()
    {
        if (shadowPortrait != null || shadowAnimator == null)
            return;

        // 그림자 Animator가 포함된 AnyPortrait를 자동으로 찾아 인스펙터 연결 누락을 보완한다
        shadowPortrait = shadowAnimator.GetComponentInParent<apPortrait>();
        if (shadowPortrait == null)
        {
            shadowPortrait = shadowAnimator.GetComponentInChildren<apPortrait>(true);
        }
    }

    private void UpdateShadowVisibility()
    {
        ResolveShadowPortrait();
        if (shadowPortrait == null)
            return;

        // EnemyVisible과 동일하게 포트레이트를 비활성화하지 않고 알파값만 점진적으로 변경한다
        float fadeTime = Mathf.Max(0.01f, shadowFadeTime);
        currentShadowAlpha = Mathf.MoveTowards(
            currentShadowAlpha,
            targetShadowAlpha,
            Time.deltaTime / fadeTime);

        ApplyShadowAlpha(currentShadowAlpha);
    }

    private void ApplyShadowAlpha(float alpha)
    {
        if (shadowPortrait == null)
            return;

        // AnyPortrait/Animator 업데이트는 유지하고 렌더 투명도만 조절한다
        shadowPortrait.SetMeshAlphaAll(alpha);
    }
}
