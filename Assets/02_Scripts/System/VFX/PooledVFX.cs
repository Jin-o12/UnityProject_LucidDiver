using System.Collections;
using UnityEngine;

/// <summary>
/// 풀에서 대여된 VFX의 재생 상태와 자동 반환 시점을 관리합니다.
/// </summary>
public sealed class PooledVFX : MonoBehaviour
{
    private VFXRuntimePool owner;
    private ParticleSystem[] particles;
    private TrailRenderer[] trails;
    private Animator[] animators;
    private Coroutine returnRoutine;
    private bool isRented;

    private void Awake()
    {
        CacheComponents();
    }

    internal void Play(VFXRuntimePool pool, VFXCatalogEntry entry)
    {
        owner = pool;
        isRented = true;
        CacheComponents();

        if (returnRoutine != null)
            StopCoroutine(returnRoutine);

        foreach (TrailRenderer trail in trails)
            trail.Clear();

        foreach (Animator animator in animators)
        {
            animator.Rebind();
            animator.Update(0f);
        }

        foreach (ParticleSystem particle in particles)
        {
            particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            particle.Play(true);
        }

        switch (entry.ReturnType)
        {
            case VFXReturnType.ParticleStopped:
                returnRoutine = StartCoroutine(ReturnWhenParticlesStop(entry.Duration, entry.UseUnscaledTime));
                break;
            case VFXReturnType.Duration:
                returnRoutine = StartCoroutine(ReturnAfterDelay(entry.Duration, entry.UseUnscaledTime));
                break;
        }
    }

    /// <summary>
    /// 지속형 또는 Animation Event 기반 VFX를 명시적으로 풀에 반환합니다.
    /// </summary>
    public void Release()
    {
        if (!isRented)
            return;

        isRented = false;
        if (returnRoutine != null)
        {
            StopCoroutine(returnRoutine);
            returnRoutine = null;
        }

        owner?.Release(this);
    }

    internal void DetachFromPool()
    {
        owner = null;
        isRented = false;
    }

    private void OnDisable()
    {
        // 부모 캐릭터가 비활성화돼 VFX도 함께 꺼진 경우 풀에서 유실되지 않도록 즉시 반환합니다.
        if (isRented)
            Release();
    }

    private void OnDestroy()
    {
        // 부착 대상과 함께 파괴된 인스턴스를 풀의 전체 개수에서 제거합니다.
        owner?.RemoveDestroyed(this);
    }

    private IEnumerator ReturnWhenParticlesStop(float fallbackDuration, bool useUnscaledTime)
    {
        // 파티클이 없는 애니메이션 프리팹은 설정된 시간으로 안전하게 반환합니다.
        if (particles.Length == 0)
        {
            yield return Wait(fallbackDuration, useUnscaledTime);
            Release();
            yield break;
        }

        yield return null;
        while (HasAliveParticle())
            yield return null;

        Release();
    }

    private IEnumerator ReturnAfterDelay(float duration, bool useUnscaledTime)
    {
        yield return Wait(duration, useUnscaledTime);
        Release();
    }

    private static object Wait(float duration, bool useUnscaledTime)
    {
        return useUnscaledTime
            ? new WaitForSecondsRealtime(duration)
            : new WaitForSeconds(duration);
    }

    private bool HasAliveParticle()
    {
        foreach (ParticleSystem particle in particles)
        {
            if (particle != null && particle.IsAlive(true))
                return true;
        }

        return false;
    }

    private void CacheComponents()
    {
        particles ??= GetComponentsInChildren<ParticleSystem>(true);
        trails ??= GetComponentsInChildren<TrailRenderer>(true);
        animators ??= GetComponentsInChildren<Animator>(true);
    }
}
