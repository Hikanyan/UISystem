using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using LitMotion;

namespace HikanyanLibrary.UITools
{
    public enum ButtonType { Normal, Gradation }

    /// <summary>
    /// 共通ボタン基盤（EventSystemのPointer系で受ける）
    /// - 色同期（InspectorのColor <-> Image.color）
    /// - PointerDown / PointerUp / PointerClick による拡張フック
    /// - クリック音は PointerClick で鳴らす（必要なら派生で変更可）
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Button))]
    public class ButtonCommon : MonoBehaviour,
        IPointerClickHandler,
        IPointerDownHandler,
        IPointerUpHandler
    {
        public enum ColorSyncMode
        {
            FieldsToImages,
            ImagesToFields
        }

        [Header("Images")]
        [SerializeField] protected Image _shadowImage;
        [SerializeField] protected Image _outLineImage;
        [SerializeField] protected Image _frontImage;

        [Header("Colors")]
        [SerializeField] private Color _shadowColor = Color.black;
        [SerializeField] private Color _outLineColor = Color.gray;
        [SerializeField] private Color _frontColor = Color.white;

        [Tooltip("色同期の方向。通常は FieldsToImages 推奨。")]
        [SerializeField] private ColorSyncMode _colorSyncMode = ColorSyncMode.FieldsToImages;

        [Header("Sound")]
        [SerializeField] private bool _isPlaySound = true;
        [SerializeField] private bool _isBackSound = false;
        [SerializeField] private bool _isStartSound = false;

        [Tooltip("未設定ならAwakeで自動生成します（UI用の簡易AudioSource）。")]
        [SerializeField] private AudioSource _audioSource;

        [Tooltip("通常クリック音")]
        [SerializeField] private AudioClip _clickClip;

        [Tooltip("戻る系クリック音（IsBackSound = true のとき優先）")]
        [SerializeField] private AudioClip _backClip;

        [Tooltip("スタート系クリック音（IsStartSound = true のとき優先）")]
        [SerializeField] private AudioClip _startClip;

        [Range(0f, 1f)]
        [SerializeField] private float _soundVolume = 1.0f;

        [Header("Animation (Base only - override in derived)")]
        [SerializeField] private bool _isAnimation = false;
        [SerializeField] private Ease _animationEase = Ease.OutBack;

        [Header("Events")]
        [SerializeField] private UnityEvent _onInitialized;
        [SerializeField] private UnityEvent _onPointerDown;
        [SerializeField] private UnityEvent _onPointerUp;
        [SerializeField] private UnityEvent _onPointerClick;

        public bool IsPlaySound => _isPlaySound;
        public bool IsBackSound => _isBackSound;
        public bool IsStartSound => _isStartSound;

        public bool IsAnimation => _isAnimation;
        public Ease AnimationEase => _animationEase;

        public Color ShadowColor => _shadowColor;
        public Color OutLineColor => _outLineColor;
        public Color FrontColor => _frontColor;

        protected Button _button;
        private bool _initialized;

        /// <summary>
        /// 生成直後に外部から明示的に初期化したい場合に呼ぶ。
        /// Awakeでも初期化されるため二重呼び出し安全。
        /// </summary>
        public void Initialized()
        {
            EnsureInitialized();
        }

        protected virtual void Awake()
        {
            EnsureInitialized();
        }

        private void EnsureInitialized()
        {
            if (_initialized) return;
            _initialized = true;

            _button = GetComponent<Button>();
            _button.onClick.AddListener(HandleClick);

            // AudioSourceが無ければ追加（UI用の簡易設定）
            if (_isPlaySound && _audioSource == null)
            {
                _audioSource = gameObject.GetComponent<AudioSource>();
                if (_audioSource == null) _audioSource = gameObject.AddComponent<AudioSource>();

                _audioSource.playOnAwake = false;
                _audioSource.loop = false;
                _audioSource.spatialBlend = 0f; // 2D
            }

            // 初回色同期
            SyncColors(_colorSyncMode);

            _onInitialized?.Invoke();
            OnInitialized();
        }

        /// <summary>派生で初期化後処理を追加したい場合にoverride。</summary>
        protected virtual void OnInitialized() { }

        // ----------------------------
        // Pointer Event Handlers
        // ----------------------------

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!IsInteractable()) return;

            _onPointerDown?.Invoke();
            OnPressed(eventData);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!IsInteractable()) return;

            _onPointerUp?.Invoke();
            OnReleased(eventData);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            // Button.onClick is the shared confirmation path for pointer and Submit.
        }

        private void HandleClick()
        {
            if (!IsInteractable()) return;

            // クリック音はClickで鳴らす（Downで鳴らしたいなら派生で変更）
            if (_isPlaySound)
            {
                PlayClickSound();
            }

            _onPointerClick?.Invoke();
            OnClicked(null);
        }

        protected virtual void OnDestroy()
        {
            if (_button != null) _button.onClick.RemoveListener(HandleClick);
        }

        /// <summary>派生で押下開始アニメ等を実装するためのフック。</summary>
        protected virtual void OnPressed(PointerEventData eventData) { }

        /// <summary>派生で押下解除アニメ等を実装するためのフック。</summary>
        protected virtual void OnReleased(PointerEventData eventData) { }

        /// <summary>派生でクリック確定後の演出等を実装するためのフック。</summary>
        protected virtual void OnClicked(PointerEventData eventData) { }

        private bool IsInteractable()
        {
            // Buttonが無効化されている場合などは無視
            return _button != null && _button.isActiveAndEnabled && _button.IsInteractable() && isActiveAndEnabled;
        }

        protected void PlayClickSound()
        {
            if (_audioSource == null) return;

            AudioClip clip = null;

            // 優先順位：Start > Back > Normal
            if (_isStartSound && _startClip != null) clip = _startClip;
            else if (_isBackSound && _backClip != null) clip = _backClip;
            else if (_clickClip != null) clip = _clickClip;

            if (clip == null) return;

            _audioSource.PlayOneShot(clip, _soundVolume);
        }

        // ----------------------------
        // Color Sync
        // ----------------------------

        public void SyncColors(ColorSyncMode mode)
        {
            if (mode == ColorSyncMode.ImagesToFields)
            {
                if (_shadowImage != null) _shadowColor = _shadowImage.color;
                if (_outLineImage != null) _outLineColor = _outLineImage.color;
                if (_frontImage != null) _frontColor = _frontImage.color;
            }

            if (_shadowImage != null) _shadowImage.color = _shadowColor;
            if (_outLineImage != null) _outLineImage.color = _outLineColor;
            if (_frontImage != null) _frontImage.color = _frontColor;
        }

        public void SetColors(Color shadow, Color outline, Color front, bool applyImmediately = true)
        {
            _shadowColor = shadow;
            _outLineColor = outline;
            _frontColor = front;

            if (applyImmediately)
            {
                SyncColors(ColorSyncMode.FieldsToImages);
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_frontImage == null) return;
            SyncColors(_colorSyncMode);
        }
#endif
    }
}
