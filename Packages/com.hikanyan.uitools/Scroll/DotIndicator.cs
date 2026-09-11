using UnityEngine;

namespace HikanyanLibrary.UITools
{
    public class DotIndicator : MonoBehaviour
    {
        [Header("Dot Build")]
        [SerializeField] private Dot _dotPrefab;
        [SerializeField] private Transform _dotContainer;
        [SerializeField, Min(0)] private int _dotCount = 3;

        [SerializeField, HideInInspector] private Dot[] _dots;

#if UNITY_EDITOR
        private bool _rebuildScheduled;

        private void OnValidate()
        {
            // OnValidate内で破棄/生成すると制限に当たるので、遅延で実行する
            if (Application.isPlaying) return;
            if (_dotPrefab == null || _dotContainer == null) return;

            // Project上のPrefabアセット等（persistent）に対しては実行しない
            if (IsPersistentObject(gameObject) || IsPersistentObject(_dotContainer.gameObject)) return;

            // ステージ（Scene / PrefabStage）が違う親へ生成しようとすると不整合になるので回避
            if (!IsSameStage(gameObject, _dotContainer.gameObject)) return;

            ScheduleRebuildInEditor();
        }

        private void OnEnable()
        {
            // 追加/複製直後などで反映したい場合
            if (Application.isPlaying) return;
            if (_dotPrefab == null || _dotContainer == null) return;

            if (IsPersistentObject(gameObject) || IsPersistentObject(_dotContainer.gameObject)) return;
            if (!IsSameStage(gameObject, _dotContainer.gameObject)) return;

            ScheduleRebuildInEditor();
        }

        private void ScheduleRebuildInEditor()
        {
            if (_rebuildScheduled) return;
            _rebuildScheduled = true;

            UnityEditor.EditorApplication.delayCall += () =>
            {
                _rebuildScheduled = false;

                // delayCallの時点で破棄されている可能性がある
                if (this == null) return;
                if (_dotPrefab == null || _dotContainer == null) return;

                // persistent / ステージ不一致の再チェック
                if (IsPersistentObject(gameObject) || IsPersistentObject(_dotContainer.gameObject)) return;
                if (!IsSameStage(gameObject, _dotContainer.gameObject)) return;

                BuildDots(editorMode: true);
                UnityEditor.EditorUtility.SetDirty(this);
            };
        }

        private static bool IsPersistentObject(Object obj)
        {
            return UnityEditor.EditorUtility.IsPersistent(obj);
        }

        private static bool IsSameStage(GameObject a, GameObject b)
        {
            var stageA = UnityEditor.SceneManagement.StageUtility.GetStageHandle(a);
            var stageB = UnityEditor.SceneManagement.StageUtility.GetStageHandle(b);
            return stageA == stageB;
        }
#endif

        private void Awake()
        {
            BuildDots(editorMode: false);
        }

        private void BuildDots(bool editorMode)
        {
            if (_dotPrefab == null || _dotContainer == null)
            {
                _dots = null;
                return;
            }

#if UNITY_EDITOR
            if (editorMode)
            {
                // persistent（Project上のPrefabアセット等）を親にしている場合は危険なので止める
                if (UnityEditor.EditorUtility.IsPersistent(_dotContainer.gameObject))
                {
                    Debug.LogWarning(
                        $"{nameof(DotIndicator)}: _dotContainer is persistent (asset object). " +
                        "Please assign a scene/prefab-stage instance Transform under the same hierarchy/stage.",
                        this);
                    return;
                }

                // ステージ不一致も止める（SceneとPrefabStage混在など）
                if (UnityEditor.SceneManagement.StageUtility.GetStageHandle(gameObject) !=
                    UnityEditor.SceneManagement.StageUtility.GetStageHandle(_dotContainer.gameObject))
                {
                    Debug.LogWarning(
                        $"{nameof(DotIndicator)}: Stage mismatch between DotIndicator and _dotContainer. " +
                        "Assign a container in the same Scene/PrefabStage.",
                        this);
                    return;
                }
            }
#endif

            // 既存を掃除
            for (int i = (_dots?.Length ?? 0) - 1; i >= 0; i--)
            {
                if (_dots[i] == null) continue;
                var child = _dots[i].gameObject;

#if UNITY_EDITOR
                if (editorMode)
                {
                    // DestroyImmediateだと「asset破壊」判定に当たりやすいのでUndo経由で安全に消す
                    UnityEditor.Undo.DestroyObjectImmediate(child);
                }
                else
                {
                    Destroy(child);
                }
#else
                Destroy(child);
#endif
            }

            if (_dotCount <= 0)
            {
                _dots = null;
                return;
            }

            _dots = new Dot[_dotCount];

            for (int i = 0; i < _dotCount; i++)
            {
                Dot dot;

#if UNITY_EDITOR
                if (editorMode)
                {
                    // Prefab参照を維持したまま生成（可能ならこれがベスト）
                    var go = UnityEditor.PrefabUtility.InstantiatePrefab(_dotPrefab.gameObject, _dotContainer) as GameObject;
                    if (go != null)
                    {
                        dot = go.GetComponent<Dot>();
                    }
                    else
                    {
                        // フォールバック（通常は来ない）
                        dot = Instantiate(_dotPrefab, _dotContainer);
                    }
                }
                else
                {
                    dot = Instantiate(_dotPrefab, _dotContainer);
                }
#else
                dot = Instantiate(_dotPrefab, _dotContainer);
#endif

                dot.name = $"Dot_{i}";
                _dots[i] = dot;
            }

            SetAll(DotState.Gray);
        }

        public void SetAll(DotState state)
        {
            if (_dots == null) return;
            for (int i = 0; i < _dots.Length; i++)
            {
                if (_dots[i] != null) _dots[i].SetState(state);
            }
        }

        public void SetStates(DotState[] states)
        {
            if (_dots == null) return;

            for (int i = 0; i < _dots.Length; i++)
            {
                var s = (states != null && i < states.Length) ? states[i] : DotState.Gray;
                if (_dots[i] != null) _dots[i].SetState(s);
            }
        }
    }
}
