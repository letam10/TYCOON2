using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Tycoon.Tests
{
    public sealed class MapAndSlotPersistenceTests
    {
        string directory, original;
        Keyboard keyboard;

        [SetUp] public void SetUp()
        {
            original = JsonUtility.ToJson(GameSettingsStore.Current);
            directory = Path.Combine(Path.GetTempPath(), "TycoonSlots_" + Guid.NewGuid().ToString("N"));
            keyboard = InputSystem.AddDevice<Keyboard>();
            GameSettingsStore.Initialize(true, directory);
        }

        [TearDown] public void TearDown()
        {
            InputSystem.RemoveDevice(keyboard);
            JsonUtility.FromJsonOverwrite(original, GameSettingsStore.Current);
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }

        [Test] public void LegacySaveStaysInSlotOneAndAdditionalSlotsCannotAliasIt()
        {
            string legacy = GameSaveSlots.PathFor(directory, 1, false);
            Assert.That(Path.GetFileName(legacy), Is.EqualTo("save-v2.json"));
            Directory.CreateDirectory(directory); File.WriteAllText(legacy, "old progress");
            for (int slot = 2; slot <= GameSaveSlots.Count; slot++)
                Assert.That(GameSaveSlots.PathFor(directory, slot, false), Is.Not.EqualTo(legacy));
            GameSaveSlots.Remember(directory, 3);
            Assert.That(GameSaveSlots.ReadActive(directory), Is.EqualTo(3));
            Assert.That(File.ReadAllText(legacy), Is.EqualTo("old progress"));
            Assert.That(Path.GetFileName(GameSaveSlots.PathFor(directory, 1, true)), Is.EqualTo("qa-save-v2.json"));
        }

        [TestCase(0)] [TestCase(4)] public void InvalidSlotNeverProducesAFilePath(int slot)
        { Assert.Throws<ArgumentOutOfRangeException>(() => GameSaveSlots.PathFor(directory, slot, false)); }

        [Test] public void RebindSurvivesStoreReloadAndResetSurvivesAnotherReload()
        {
            Assert.That(GameKeyBindings.Set("map", "<Keyboard>/p", out _), Is.True);
            GameSettingsStore.SaveAndApply();
            GameSettingsStore.Initialize(true, directory);
            Assert.That(GameKeyBindings.Path("map"), Is.EqualTo("<Keyboard>/p"));
            GameSettingsStore.Current.keyBindings.Clear(); GameSettingsStore.SaveAndApply();
            GameSettingsStore.Initialize(true, directory);
            Assert.That(GameKeyBindings.Path("map"), Is.EqualTo("<Keyboard>/m"));
        }

        [TestCase("<Keyboard>/w")] [TestCase("<Keyboard>/upArrow")]
        [TestCase("<Keyboard>/escape")] [TestCase("<Keyboard>/rightShift")]
        public void DuplicateAndFallbackKeysCannotSilentlyChangeAnotherAction(string path)
        {
            string before = JsonUtility.ToJson(GameSettingsStore.Current);
            Assert.That(GameKeyBindings.Set("map", path, out string reason), Is.False);
            Assert.That(reason, Is.Not.Empty);
            Assert.That(JsonUtility.ToJson(GameSettingsStore.Current), Is.EqualTo(before));
        }

        [Test] public void RebindingAnEnabledActionPreservesTheGamepadBinding()
        {
            using var action = new InputAction("Map", InputActionType.Button, "<Keyboard>/m");
            action.AddBinding("<Gamepad>/buttonNorth"); action.Enable();
            Assert.That(GameKeyBindings.Set("map", "<Keyboard>/p", out _), Is.True);
            GameKeyBindings.Apply(action, 0, "map");
            Assert.That(action.enabled, Is.True);
            Assert.That(action.bindings[0].effectivePath, Is.EqualTo("<Keyboard>/p"));
            Assert.That(action.bindings[1].effectivePath, Is.EqualTo("<Gamepad>/buttonNorth"));
        }
    }
}
