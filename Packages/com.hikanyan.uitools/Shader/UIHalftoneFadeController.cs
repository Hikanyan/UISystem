using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI/HalftoneFade_PixelLocked 専用コントローラ
/// - ドット半径でフェードする UI 表現を C# から安全に制御する
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(Graphic))]
public sealed class UIHalftoneFadeController : MonoBehaviour
{

    [Header("Fade (Logical 0-1)")]
    [Range(0f, 1f)]
    [SerializeField] private float fade01 = 0f;

    [SerializeField] private bool invert = false;

    [Header("Halftone (Pixel Based)")]
    [SerializeField] private float dotSpacingPx = 18f;
    [SerializeField] private float maxRadiusPx = 9f;
    [SerializeField] private float edgeSoftnessPx = 1.0f;

    [Header("Screen Angle")]
    [Range(0f, 180f)]
    [SerializeField] private float angleDeg = 45f;

    private Graphic _graphic;
    private Material _runtimeMaterial;
    private Material _sourceMaterial;

    // Shader property IDs（高速・安全）
    private static readonly int DotSpacingPxID   = Shader.PropertyToID("_DotSpacingPx");
    private static readonly int MaxRadiusPxID    = Shader.PropertyToID("_MaxRadiusPx");
    private static readonly int EdgeSoftnessPxID = Shader.PropertyToID("_EdgeSoftnessPx");
    private static readonly int AngleDegID       = Shader.PropertyToID("_AngleDeg");
    private static readonly int FadeStartID      = Shader.PropertyToID("_FadeStart");
    private static readonly int FadeEndID        = Shader.PropertyToID("_FadeEnd");
    private static readonly int InvertID         = Shader.PropertyToID("_Invert");


    private void Awake()
    {
        InitializeMaterial();
        ApplyAll();
    }

    private void OnEnable()
    {
        InitializeMaterial();
        ApplyAll();
    }

#if UNITY_EDITOR
    private bool _materialUpdateScheduled;
    private void OnValidate()
    {
        if (!isActiveAndEnabled || _materialUpdateScheduled) return;
        _materialUpdateScheduled = true;
        UnityEditor.EditorApplication.delayCall += () =>
        {
            _materialUpdateScheduled = false;
            if (this == null || !isActiveAndEnabled) return;
            InitializeMaterial();
            ApplyAll();
        };
    }
#endif

    private void OnDestroy()
    {
        // ランタイム生成マテリアルの後始末
        ReleaseMaterial();
    }

    private void ReleaseMaterial()
    {
        if (_runtimeMaterial == null) return;
        if (_graphic != null && _graphic.material == _runtimeMaterial) _graphic.material = _sourceMaterial;
        if (Application.isPlaying) Destroy(_runtimeMaterial);
        else DestroyImmediate(_runtimeMaterial);
        _runtimeMaterial = null;
    }

    // ==============================
    // Public API
    // ==============================

    /// <summary>
    /// フェード量を 0–1 で設定（0 = ドット最大、1 = 消失）
    /// </summary>
    public void SetFade01(float value)
    {
        fade01 = Mathf.Clamp01(value);
        ApplyFade();
    }

    /// <summary>
    /// 一括設定（Tween 用）
    /// </summary>
    public void SetFade01(float value, bool inverted)
    {
        fade01 = Mathf.Clamp01(value);
        invert = inverted;
        ApplyFade();
    }

    // ==============================
    // Core
    // ==============================

    private void InitializeMaterial()
    {
        if (_graphic == null)
            _graphic = GetComponent<Graphic>();

        if (_graphic.material == null)
            return;

        // すでにインスタンス化済みなら再利用
        if (_runtimeMaterial != null && _graphic.material == _runtimeMaterial)
            return;

        // ★ sharedMaterial を直接触らない
        ReleaseMaterial();
        _sourceMaterial = _graphic.material;
        _runtimeMaterial = Instantiate(_sourceMaterial);
        _runtimeMaterial.hideFlags = HideFlags.HideAndDontSave;
        _runtimeMaterial.name = $"{_graphic.material.name} (Halftone Runtime)";
        _graphic.material = _runtimeMaterial;
    }

    private void ApplyAll()
    {
        if (_runtimeMaterial == null) return;

        _runtimeMaterial.SetFloat(DotSpacingPxID, dotSpacingPx);
        _runtimeMaterial.SetFloat(MaxRadiusPxID, maxRadiusPx);
        _runtimeMaterial.SetFloat(EdgeSoftnessPxID, edgeSoftnessPx);
        _runtimeMaterial.SetFloat(AngleDegID, angleDeg);

        ApplyFade();
    }

    private void ApplyFade()
    {
        if (_runtimeMaterial == null) return;

        // Shader は FadeStart / FadeEnd で範囲指定する設計
        // → C# 側では「0→fade01」を常にフェード範囲にする
        _runtimeMaterial.SetFloat(FadeStartID, 0f);
        _runtimeMaterial.SetFloat(FadeEndID, fade01);

        _runtimeMaterial.SetFloat(InvertID, invert ? 1f : 0f);
    }
}
