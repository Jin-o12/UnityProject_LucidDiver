using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class OcclusionWall : MonoBehaviour
{
    private sealed class RendererRuntimeData
    {
        public Renderer renderer;
        public Material[] opaqueMaterials;
        public Color[] fadeBaseColors;
        public MaterialPropertyBlock[] propertyBlocks;
    }

    [Header("LOD 및 벽 렌더러")]
    [SerializeField] private LODGroup lodGroup;

    [Tooltip("비어 있으면 LOD Group에 등록된 모든 Renderer를 자동으로 수집합니다.")]
    [SerializeField] private Renderer[] wallRenderers;

    [Header("투명 처리용 재질")]
    [Tooltip("각 LOD Renderer의 Material 슬롯 순서와 동일하게 등록합니다.")]
    [SerializeField] private Material[] fadeMaterials;

    [Header("투명 처리 설정")]
    [SerializeField, Range(0.05f, 1f)] private float fadedAlpha = 0.25f;
    [SerializeField, Min(0.1f)] private float fadeSpeed = 3f;

    [Header("디버그")]
    [SerializeField] private bool showDebugLog = false;

    private readonly List<RendererRuntimeData> rendererDataList = new();

    private bool isOccluded;
    private bool isUsingFadeMaterials;
    private float currentAlpha = 1f;

    private static readonly int BaseColorId =
        Shader.PropertyToID("_BaseColor");

    private static readonly int ColorId =
        Shader.PropertyToID("_Color");

    private void Awake()
    {
        CollectWallRenderers();

        if (!BuildRendererData())
        {
            enabled = false;
            return;
        }

        // {게임 시작 시 모든 Renderer가 원래 Opaque Material을 사용하도록 한다}
        RestoreOpaqueMaterialsImmediate();
    }

    private void CollectWallRenderers()
    {
        if (lodGroup == null)
        {
            // {같은 Root에 있는 LOD Group을 자동으로 찾는다}
            lodGroup = GetComponent<LODGroup>();
        }

        if (wallRenderers != null && wallRenderers.Length > 0)
        {
            return;
        }

        HashSet<Renderer> collectedRenderers = new();

        if (lodGroup != null)
        {
            // {LOD 0, LOD 1, LOD 2에 등록된 모든 Renderer를 수집한다}
            LOD[] lods = lodGroup.GetLODs();

            for (int lodIndex = 0; lodIndex < lods.Length; lodIndex++)
            {
                Renderer[] renderers = lods[lodIndex].renderers;

                for (int rendererIndex = 0;
                     rendererIndex < renderers.Length;
                     rendererIndex++)
                {
                    Renderer renderer = renderers[rendererIndex];

                    if (renderer != null)
                    {
                        collectedRenderers.Add(renderer);
                    }
                }
            }
        }

        if (collectedRenderers.Count == 0)
        {
            // {LOD Group이 없다면 자식에 있는 모든 Renderer를 대신 수집한다}
            Renderer[] childRenderers =
                GetComponentsInChildren<Renderer>(true);

            for (int i = 0; i < childRenderers.Length; i++)
            {
                if (childRenderers[i] != null)
                {
                    collectedRenderers.Add(childRenderers[i]);
                }
            }
        }

        wallRenderers = new Renderer[collectedRenderers.Count];
        collectedRenderers.CopyTo(wallRenderers);
    }

    private bool BuildRendererData()
    {
        rendererDataList.Clear();

        if (wallRenderers == null || wallRenderers.Length == 0)
        {
            Debug.LogError(
                $"{name}: 투명 처리할 벽 Renderer를 찾을 수 없습니다.");

            return false;
        }

        if (fadeMaterials == null || fadeMaterials.Length == 0)
        {
            Debug.LogError(
                $"{name}: Fade Materials가 등록되지 않았습니다.");

            return false;
        }

        for (int rendererIndex = 0;
             rendererIndex < wallRenderers.Length;
             rendererIndex++)
        {
            Renderer renderer = wallRenderers[rendererIndex];

            if (renderer == null)
            {
                continue;
            }

            Material[] originalMaterials = renderer.sharedMaterials;

            if (originalMaterials.Length != fadeMaterials.Length)
            {
                Debug.LogError(
                    $"{name}: {renderer.name}의 Material 슬롯 수와 " +
                    $"Fade Material 수가 다릅니다. " +
                    $"원본={originalMaterials.Length}, " +
                    $"Fade={fadeMaterials.Length}");

                return false;
            }

            RendererRuntimeData runtimeData =
                new RendererRuntimeData
                {
                    renderer = renderer,
                    opaqueMaterials = originalMaterials,
                    fadeBaseColors =
                        new Color[fadeMaterials.Length],
                    propertyBlocks =
                        new MaterialPropertyBlock[fadeMaterials.Length]
                };

            for (int materialIndex = 0;
                 materialIndex < fadeMaterials.Length;
                 materialIndex++)
            {
                Material fadeMaterial = fadeMaterials[materialIndex];

                if (fadeMaterial == null)
                {
                    Debug.LogError(
                        $"{name}: Fade Material의 " +
                        $"{materialIndex}번 슬롯이 비어 있습니다.");

                    return false;
                }

                // {각 투명 Material의 원래 색상을 저장한다}
                runtimeData.fadeBaseColors[materialIndex] =
                    GetMaterialColor(fadeMaterial);

                // {Renderer별 Alpha 값을 적용할 PropertyBlock을 생성한다}
                runtimeData.propertyBlocks[materialIndex] =
                    new MaterialPropertyBlock();
            }

            rendererDataList.Add(runtimeData);
        }

        return rendererDataList.Count > 0;
    }

    private void Update()
    {
        if (!isUsingFadeMaterials && !isOccluded)
        {
            return;
        }

        float targetAlpha =
            isOccluded ? fadedAlpha : 1f;

        // {현재 Alpha를 목표 값 방향으로 부드럽게 이동한다}
        currentAlpha = Mathf.MoveTowards(
            currentAlpha,
            targetAlpha,
            fadeSpeed * Time.deltaTime);

        ApplyAlpha(currentAlpha);

        if (!isOccluded &&
            Mathf.Approximately(currentAlpha, 1f))
        {
            RestoreOpaqueMaterialsImmediate();
        }
    }

    public void SetOccluded(bool value)
    {
        isOccluded = value;

        if (!isOccluded || isUsingFadeMaterials)
        {
            return;
        }

        // {LOD 0, LOD 1, LOD 2의 모든 Renderer를 투명 Material로 교체한다}
        for (int i = 0; i < rendererDataList.Count; i++)
        {
            rendererDataList[i].renderer.sharedMaterials =
                fadeMaterials;
        }

        isUsingFadeMaterials = true;
        currentAlpha = 1f;

        ApplyAlpha(currentAlpha);

        if (showDebugLog)
        {
            Debug.Log(
                $"OcclusionWall: 투명화 시작 - {name}");
        }
    }

    private void ApplyAlpha(float alpha)
    {
        for (int rendererIndex = 0;
             rendererIndex < rendererDataList.Count;
             rendererIndex++)
        {
            RendererRuntimeData runtimeData =
                rendererDataList[rendererIndex];

            for (int materialIndex = 0;
                 materialIndex < fadeMaterials.Length;
                 materialIndex++)
            {
                Material fadeMaterial =
                    fadeMaterials[materialIndex];

                Color color =
                    runtimeData.fadeBaseColors[materialIndex];

                color.a = alpha;

                MaterialPropertyBlock propertyBlock =
                    runtimeData.propertyBlocks[materialIndex];

                propertyBlock.Clear();

                if (fadeMaterial.HasProperty(BaseColorId))
                {
                    // {Shader Graph의 _BaseColor Alpha를 변경한다}
                    propertyBlock.SetColor(BaseColorId, color);
                }
                else if (fadeMaterial.HasProperty(ColorId))
                {
                    // {일반 Shader의 _Color Alpha를 변경한다}
                    propertyBlock.SetColor(ColorId, color);
                }

                runtimeData.renderer.SetPropertyBlock(
                    propertyBlock,
                    materialIndex);
            }
        }
    }

    private void RestoreOpaqueMaterialsImmediate()
    {
        for (int rendererIndex = 0;
             rendererIndex < rendererDataList.Count;
             rendererIndex++)
        {
            RendererRuntimeData runtimeData =
                rendererDataList[rendererIndex];

            // {각 LOD Renderer의 원래 Opaque Material을 복원한다}
            runtimeData.renderer.sharedMaterials =
                runtimeData.opaqueMaterials;

            for (int materialIndex = 0;
                 materialIndex <
                 runtimeData.propertyBlocks.Length;
                 materialIndex++)
            {
                // {투명 Material에 사용했던 PropertyBlock을 제거한다}
                runtimeData.renderer.SetPropertyBlock(
                    null,
                    materialIndex);
            }
        }

        isUsingFadeMaterials = false;
        currentAlpha = 1f;

        if (showDebugLog)
        {
            Debug.Log(
                $"OcclusionWall: 원래 재질 복원 - {name}");
        }
    }

    private Color GetMaterialColor(Material material)
    {
        if (material.HasProperty(BaseColorId))
        {
            return material.GetColor(BaseColorId);
        }

        if (material.HasProperty(ColorId))
        {
            return material.GetColor(ColorId);
        }

        // {색상 Property가 없는 Material은 흰색을 기본값으로 사용한다}
        return Color.white;
    }

    private void OnDisable()
    {
        // {씬 전환이나 비활성화 시 투명 상태가 남지 않도록 복원한다}
        isOccluded = false;

        if (rendererDataList.Count > 0)
        {
            RestoreOpaqueMaterialsImmediate();
        }
    }
}