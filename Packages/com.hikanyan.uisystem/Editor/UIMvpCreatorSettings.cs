#nullable enable
using UnityEngine;

namespace HikanyanLibrary.UISystem.Editor
{
    public sealed class UIMvpCreatorSettings : ScriptableObject
    {
        [Header("Roots (Folder Paths)")]
        public string GeneratedRoot = "Assets/HikanyanLibrary/Scripts/UISystem/Generated";
        public string TemplateRoot   = "Assets/HikanyanLibrary/Scripts/UISystem/Editor/Template";
        public string PrefabRoot     = "Assets/HikanyanLibrary/Prefab/UISystem";

        [Header("General")]
        public string DefaultNamespace = "HikanyanLibrary.UISystem";

        [Header("Overwrite Defaults")]
        public bool OverwriteScriptsDefault = false;
        public bool OverwritePrefabDefault  = false;
    }
}