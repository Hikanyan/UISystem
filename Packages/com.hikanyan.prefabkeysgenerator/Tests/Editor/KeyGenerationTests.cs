using System;
using System.Collections.Generic;
using HikanyanLibrary.Tool;
using NUnit.Framework;

public sealed class KeyGenerationTests
{
    [Test] public void PreservesAddressAndEscapesKeywords()
    {
        var source = AddressableAssetsUtil.BuildSource(new Dictionary<string, string> { ["class"] = "UI/Shop" }, "Game.UI");
        StringAssert.Contains("@class = \"UI/Shop\"", source);
    }
    [Test] public void EscapesStringLiterals()
    {
        var source = AddressableAssetsUtil.BuildSource(new Dictionary<string, string> { ["Popup"] = "a\"b\\c\n" }, "");
        StringAssert.Contains("a\\\"b\\\\c\\n", source);
    }
    [Test] public void InvalidNamesFailBeforeWriting() => Assert.Throws<ArgumentException>(() =>
        AddressableAssetsUtil.BuildSource(new Dictionary<string, string> { ["not-valid"] = "address" }, "Game.UI"));
    [Test] public void DeterministicOutput()
    {
        var first = new Dictionary<string, string> { ["B"] = "b", ["A"] = "a" };
        var second = new Dictionary<string, string> { ["A"] = "a", ["B"] = "b" };
        Assert.AreEqual(AddressableAssetsUtil.BuildSource(first, "Game"), AddressableAssetsUtil.BuildSource(second, "Game"));
    }
}
