using UnityEngine;

namespace HikanyanLibrary.UITools
{
    public enum DotState
    {
        Gray = 0,
        Clear = 1,
        Perfect = 2,
    }

    public class Dot : MonoBehaviour
    {
        [SerializeField] private DotState _initialState = DotState.Gray;
        [SerializeField] private GradientImage _dotImage;
        [SerializeField] private Gradient _dotGrayColor;
        [SerializeField] private Gradient _dotClearColor;
        [SerializeField] private Gradient _dotPerfectColor;

        void OnValidate()
        {
            SetState(_initialState);
        }
        
        public void SetState(DotState state)
        {
            if (_dotImage == null) return;

            // Gradientの終端色を採用（必要なら Evaluate(0.5f) 等に変更可）

            switch (state)
            {
                case DotState.Gray:
                    _dotImage.SetGradient(_dotGrayColor);
                    break;
                case DotState.Clear:
                    _dotImage.SetGradient(_dotClearColor);
                    break;
                case DotState.Perfect:
                    _dotImage.SetGradient(_dotPerfectColor);
                    break;
            }
        }
    }
}
