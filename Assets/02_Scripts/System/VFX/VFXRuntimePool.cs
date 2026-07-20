using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 하나의 VFX ID에 해당하는 인스턴스만 관리하는 런타임 풀입니다.
/// </summary>
public sealed class VFXRuntimePool
{
    private readonly VFXCatalogEntry entry;
    private readonly GameObject prefab;
    private readonly Transform root;
    private readonly Queue<PooledVFX> available = new Queue<PooledVFX>();
    private readonly HashSet<PooledVFX> instances = new HashSet<PooledVFX>();
    private readonly HashSet<PooledVFX> availableSet = new HashSet<PooledVFX>();
    private readonly HashSet<PooledVFX> pendingHierarchyRestore = new HashSet<PooledVFX>();

    public VFXRuntimePool(VFXCatalogEntry entry, GameObject prefab, Transform root)
    {
        this.entry = entry;
        this.prefab = prefab;
        this.root = root;

        for (int i = 0; i < entry.InitialPoolSize; i++)
            EnqueueAvailable(CreateInstance());
    }

    /// <summary>
    /// 비활성 인스턴스를 우선 사용하고, 최대 크기 전까지만 새 인스턴스를 생성합니다.
    /// </summary>
    public PooledVFX Rent(VFXContext context)
    {
        instances.RemoveWhere(candidate => candidate == null);
        availableSet.RemoveWhere(candidate => candidate == null);

        PooledVFX instance = null;
        while (available.Count > 0 && instance == null)
        {
            PooledVFX candidate = available.Dequeue();
            availableSet.Remove(candidate);
            pendingHierarchyRestore.Remove(candidate);
            if (candidate != null)
                instance = candidate;
        }

        if (instance == null && instances.Count < entry.MaxPoolSize)
            instance = CreateInstance();

        if (instance == null)
        {
            Debug.LogWarning($"[VFXPool] 최대 풀 크기에 도달해 재생을 건너뜁니다: {entry.VfxId}");
            return null;
        }

        Transform instanceTransform = instance.transform;
        if (entry.AttachType == VFXAttachType.Parent && context.Parent != null)
        {
            instanceTransform.SetParent(context.Parent, false);
            instanceTransform.localPosition = context.Position + entry.PositionOffset;
            instanceTransform.localRotation = context.Rotation * entry.RotationOffset;
        }
        else
        {
            instanceTransform.SetParent(null, false);
            instanceTransform.SetPositionAndRotation(
                context.Position + context.Rotation * entry.PositionOffset,
                context.Rotation * entry.RotationOffset);
        }

        instanceTransform.localScale = prefab.transform.localScale * entry.ScaleMultiplier;
        instance.gameObject.SetActive(true);
        instance.Play(this, entry);
        return instance;
    }

    /// <summary>
    /// 일반 반환은 오브젝트를 비활성화하고, OnDisable 경로는 중복 비활성화 없이 풀 상태만 복원합니다.
    /// </summary>
    internal void Release(PooledVFX instance, bool deactivate)
    {
        if (instance == null || !instances.Contains(instance))
            return;

        if (deactivate)
        {
            if (instance.gameObject.activeSelf)
                instance.gameObject.SetActive(false);

            // 명시적 반환에서만 부모를 풀 루트로 복원합니다.
            // 부모 비활성화로 OnDisable이 호출된 경우에는 계층을 변경하지 않아 활성화 충돌을 방지합니다.
            instance.transform.SetParent(root, false);
            EnqueueAvailable(instance);
        }
        else
        {
            // 활성화 전환이 끝나기 전에는 다시 대여하지 않고 지연 복원 대기열에만 보관합니다.
            pendingHierarchyRestore.Add(instance);
        }
    }

    internal void RemoveDestroyed(PooledVFX instance)
    {
        instances.Remove(instance);
        availableSet.Remove(instance);
        pendingHierarchyRestore.Remove(instance);
    }

    public void Dispose()
    {
        foreach (PooledVFX instance in instances)
        {
            if (instance == null)
                continue;

            instance.DetachFromPool();
            Object.Destroy(instance.gameObject);
        }

        instances.Clear();
        available.Clear();
        availableSet.Clear();
        pendingHierarchyRestore.Clear();
    }

    private PooledVFX CreateInstance()
    {
        GameObject created = Object.Instantiate(prefab, root);
        created.name = $"{prefab.name} [Pooled]";

        PooledVFX pooledVFX = created.GetComponent<PooledVFX>();
        if (pooledVFX == null)
            pooledVFX = created.AddComponent<PooledVFX>();

        created.SetActive(false);
        instances.Add(pooledVFX);
        return pooledVFX;
    }

    /// <summary>
    /// 같은 인스턴스가 대기열에 두 번 등록되지 않도록 반환 경로를 한곳에서 관리합니다.
    /// </summary>
    private void EnqueueAvailable(PooledVFX instance)
    {
        if (instance == null || !availableSet.Add(instance))
            return;

        available.Enqueue(instance);
    }

    /// <summary>
    /// 부모 비활성화가 끝난 뒤 반환된 VFX를 안전하게 끄고 풀 루트로 복원합니다.
    /// </summary>
    internal void ProcessPendingReturns()
    {
        if (pendingHierarchyRestore.Count == 0)
            return;

        // SetActive(false)가 다른 OnDisable을 동기 호출해도 현재 열거 대상이 바뀌지 않도록 먼저 복사합니다.
        PooledVFX[] pendingInstances = new PooledVFX[pendingHierarchyRestore.Count];
        pendingHierarchyRestore.CopyTo(pendingInstances);
        pendingHierarchyRestore.Clear();

        foreach (PooledVFX instance in pendingInstances)
        {
            if (instance == null || !instances.Contains(instance))
                continue;

            if (instance.gameObject.activeSelf)
                instance.gameObject.SetActive(false);

            instance.transform.SetParent(root, false);
            EnqueueAvailable(instance);
        }
    }
}
