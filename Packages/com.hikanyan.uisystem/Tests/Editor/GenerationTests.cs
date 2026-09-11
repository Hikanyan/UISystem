using System.IO;
using HikanyanLibrary.UISystem.Editor;
using NUnit.Framework;

namespace HikanyanLibrary.UISystem.Tests
{
    public sealed class GenerationTests
    {
        [TestCase("MyGame.UI", true)]
        [TestCase("Game.class", false)]
        [TestCase("Game..UI", false)]
        [TestCase("1Game.UI", false)]
        public void NamespaceIsValidated(string value, bool expected) => Assert.AreEqual(expected, CodeGenerationValidation.IsNamespace(value));
        [Test] public void DefaultTemplatesExistAndImportRuntime()
        {
            var settings = UnityEngine.ScriptableObject.CreateInstance<UIMvpCreatorSettings>();
            try
            {
                foreach (var mode in new[] { "Scene", "Prefab" })
                    foreach (var type in new[] { "Param", "View", "Presenter" })
                    {
                        var path = $"{settings.TemplateRoot}/{mode}/{type}.txt";
                        Assert.IsTrue(File.Exists(path), path);
                        StringAssert.Contains("using HikanyanLibrary.UISystem;", File.ReadAllText(path));
                    }
            }
            finally { UnityEngine.Object.DestroyImmediate(settings); }
        }
    }
}
