using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HikanyanLibrary.UITools
{
    [RequireComponent(typeof(CanvasGroup))]
    public class TextScroller : MonoBehaviour
    {
        [SerializeField, Min(1)] private float scrollSpeed = 100f;
        [SerializeField] private float scrollFinishLineAddValue = 50f;
        [SerializeField, Min(0)] private float waitTimeBeforeScroll = 2f;
        [SerializeField, Min(0)] private float waitTimeAfterScroll = 2f;
        [SerializeField, Min(0)] private float fadeDuration = 0.4f;
        [SerializeField, Min(0)] private float waitTimeFade = 0.2f;
        private CancellationTokenSource _visibleLifetime;
        private TMP_Text _text;
        private RectTransform _rect;
        private CanvasGroup _group;
        private Vector2 _origin;
        private bool _ready;

        private void Awake()
        {
            _text = GetComponent<TMP_Text>();
            _rect = GetComponent<RectTransform>();
            _group = GetComponent<CanvasGroup>();
            if (_rect != null) _origin = _rect.anchoredPosition;
            _ready = _text != null && _rect != null;
        }
        private void OnEnable()
        {
            TMPro_EventManager.TEXT_CHANGED_EVENT.Add(OnTextChanged);
            Refresh();
        }
        private void OnDisable()
        {
            TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(OnTextChanged);
            Stop();
        }
        private void OnRectTransformDimensionsChange() { if (_ready && isActiveAndEnabled) Refresh(); }
        private void OnTextChanged(UnityEngine.Object obj) { if (obj == _text) Refresh(); }
        private void Stop()
        {
            _visibleLifetime?.Cancel();
            _visibleLifetime?.Dispose();
            _visibleLifetime = null;
            if (_rect != null) _rect.anchoredPosition = _origin;
            if (_group != null) _group.alpha = 1;
        }
        public void Refresh()
        {
            if (!_ready || !isActiveAndEnabled) return;
            Stop();
            _visibleLifetime = new CancellationTokenSource();
            ScrollAsync(_visibleLifetime.Token).Forget();
        }
        private async UniTask ScrollAsync(CancellationToken token)
        {
            await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate, token);
            var parent = _rect.parent as RectTransform;
            if (parent == null) return;
            var width = Mathf.Max(_rect.rect.width, _text.preferredWidth);
            if (width + _origin.x <= parent.rect.width) return;
            var finish = parent.rect.width - width - scrollFinishLineAddValue;
            while (!token.IsCancellationRequested)
            {
                await Pause(waitTimeBeforeScroll, token);
                var duration = Mathf.Abs(_origin.x - finish) / Mathf.Max(1, scrollSpeed);
                await Animate(duration, t => _rect.anchoredPosition = new Vector2(Mathf.Lerp(_origin.x, finish, t), _origin.y), token);
                await Pause(waitTimeAfterScroll, token);
                await Animate(fadeDuration, t => _group.alpha = 1 - t, token);
                _rect.anchoredPosition = _origin;
                await Pause(waitTimeFade, token);
                await Animate(fadeDuration, t => _group.alpha = t, token);
            }
        }
        private static UniTask Pause(float seconds, CancellationToken token) => UniTask.Delay(TimeSpan.FromSeconds(Mathf.Max(0, seconds)), ignoreTimeScale: true, cancellationToken: token);
        private static async UniTask Animate(float duration, Action<float> apply, CancellationToken token)
        {
            for (float time = 0; time < duration; time += Time.unscaledDeltaTime)
            {
                token.ThrowIfCancellationRequested();
                apply(time / duration);
                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }
            token.ThrowIfCancellationRequested();
            apply(1);
        }
    }
}
