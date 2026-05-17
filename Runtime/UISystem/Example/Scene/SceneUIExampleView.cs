using TMPro;
using UnityEngine;

namespace HikanyanLibrary.UISystem.Example
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SceneUIRegistrar))]
    [RequireComponent(typeof(SceneUIExamplePresenter))]
    public sealed class SceneUIExampleView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI text;
        
        // 本来は TextMeshProUGUI 等の参照を持つ想定
        public void SetMessage(string message)
        {
            text.text = message;
            Debug.Log($"[SceneUIExampleView] {message}");
        }
    }
}