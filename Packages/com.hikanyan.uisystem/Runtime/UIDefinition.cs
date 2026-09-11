using UnityEngine;

namespace HikanyanLibrary.UISystem
{
    public enum UILayer { HUD, Page, Modal, Toast }
    public enum UIReuse { Destroy, KeepAlive }
    public enum UIDuplicate { Multiple, FocusExisting }
    [CreateAssetMenu(menuName = "UI/Screen Definition", fileName = "Screen")]
    public sealed class UIDefinition : ScriptableObject
    {
        public string Id;
        [Tooltip("Direct reference takes precedence. Leave empty to use AddressableKey.")]
        public UINodeBase Prefab;
        public string AddressableKey;
        public UILayer Layer = UILayer.Page;
        public UIReuse Reuse = UIReuse.Destroy;
        public UIDuplicate Duplicate = UIDuplicate.FocusExisting;
        public bool CloseOnBack = true;
    }
}
