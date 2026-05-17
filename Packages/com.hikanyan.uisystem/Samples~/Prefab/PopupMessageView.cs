using TMPro;
using UnityEngine;

namespace HikanyanLibrary.UISystem.Example
{
    public sealed class PopupMessageView : MonoBehaviour
    {
        // TextMeshProUGUI 等の参照を持つ想定
        [SerializeField] private TextMeshProUGUI  _titleText;
        [SerializeField] private TextMeshProUGUI  _bodyText;
        public string TitleText { get; set; }
        public string BodyText { get; set; }

        void Start()
        {
            _titleText.text = TitleText;
            _bodyText.text = BodyText;
        }
    }
}