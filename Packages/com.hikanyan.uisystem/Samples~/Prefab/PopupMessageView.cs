using TMPro;
using UnityEngine;

namespace HikanyanLibrary.UISystem.Example
{
    public sealed class PopupMessageView : MonoBehaviour
    {
        // TextMeshProUGUI 等の参照を持つ想定
        [SerializeField] private TextMeshProUGUI  _titleText;
        [SerializeField] private TextMeshProUGUI  _bodyText;
        public string TitleText { get => _titleText != null ? _titleText.text : ""; set { if (_titleText != null) _titleText.text = value; } }
        public string BodyText { get => _bodyText != null ? _bodyText.text : ""; set { if (_bodyText != null) _bodyText.text = value; } }
    }
}
