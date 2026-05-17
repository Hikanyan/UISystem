using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HikanyanLibrary.UISystem
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SceneUIRegistrar))]
    [RequireComponent(typeof(LoadingScenePresenter))]
    public class LoadingSceneView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI text;

        // 任意：進捗バー（無いなら未設定でOK）
        [SerializeField] private Slider progressSlider;

        // 任意：くるくる表示など（無いなら未設定でOK）
        [SerializeField] private GameObject spinnerRoot;

        public void SetMessage(string message)
        {
            if (text != null) text.text = message;
        }

        /// <summary>0..1</summary>
        public void SetProgress(float value01)
        {
            if (progressSlider != null) progressSlider.value = Mathf.Clamp01(value01);
        }

        public void SetSpinnerActive(bool active)
        {
            if (spinnerRoot != null) spinnerRoot.SetActive(active);
        }
    }
}