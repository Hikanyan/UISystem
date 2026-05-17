using TMPro;
using UnityEngine;

namespace HikanyanLibrary.UISystem
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SceneUIRegistrar))]
    [RequireComponent(typeof(NewUIPresenter))]
    public class NewUIView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI text;

        public void SetMessage(string message)
        {
            if (text != null)
            {
                text.text = message;
            }
        }
    }
}
