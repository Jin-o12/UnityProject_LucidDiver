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

    public VFXRuntimePool(VFXCatalogEntry entry, GameObject prefab, Transform root)
    {
        this.entry = entry;
        this.prefab = prefab;
        this.root = root;

        for (int i = 0; i < entry.InitialPoolSize; i++)
            available.Enqueue(CreateInstance());
    }

    /// <summary>
    /// 비활성 인스턴스를 우선 사용하고, 최대 크기 전까지만 새 인스턴스를 생성합니다.
    /// </summary>
    public PooledVFX Rent(VFXContext context)
    {
        instances.RemoveWhere(candidate => candidate == null);

        PooledVFX instance = null;
        while (available.Count > 0 && instance == null)
            instance = available.Dequeue();

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

    public void Release(PooledVFX instance)
    {
        if (instance == null || !instances.Contains(instance))
            return;

        instance.gameObject.SetActive(false);
        instance.transform.SetParent(root, false);
        available.Enqueue(instance);
    }

    internal void RemoveDestroyed(PooledVFX instance)
    {
        instances.Remove(instance);
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
}
