using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace Tycoon.Editor
{
    [InitializeOnLoad]
    public static class SaveRecoveryPlayVerification
    {
        const string ActiveKey = "Tycoon.SaveRecovery.Active";
        const string ReportKey = "Tycoon.SaveRecovery.Report";
        static PlayAudit audit;
        static Gamepad gamepad;
        static Vector3 startingPosition;
        static double deadline;
        static double moveUntil;
        static int phase;
        static string ReportPath => Path.Combine(GameSession.Argument("--qa-output", ""), "editor-play-report.json");

        static SaveRecoveryPlayVerification()
        {
            if (SessionState.GetBool(ActiveKey, false)) Attach();
        }

        public static void Run()
        {
            audit = new PlayAudit();
            SessionState.SetString(ReportKey, JsonUtility.ToJson(audit));
            SessionState.SetBool(ActiveKey, true);
            Attach();
            EditorSceneManager.OpenScene("Assets/_Game/Scenes/Tycoon.unity");
            EditorApplication.EnterPlaymode();
        }

        static void Attach()
        {
            audit = JsonUtility.FromJson<PlayAudit>(SessionState.GetString(ReportKey, "")) ?? new PlayAudit();
            deadline = EditorApplication.timeSinceStartup + 90;
            phase = 0;
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
            EditorApplication.playModeStateChanged -= PlayChanged;
            EditorApplication.playModeStateChanged += PlayChanged;
        }

        static void PlayChanged(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.EnteredPlayMode)
            {
                phase = 0;
                deadline = EditorApplication.timeSinceStartup + 90;
            }
            if (change != PlayModeStateChange.EnteredEditMode) return;
            if (audit.completedSessions < 2)
                EditorApplication.delayCall += EditorApplication.EnterPlaymode;
            else
                Finish(true, "");
        }

        static void Tick()
        {
            try
            {
                if (EditorApplication.timeSinceStartup > deadline)
                    throw new Exception("Editor Play did not initialize within 90 seconds.");
                if (!EditorApplication.isPlaying || phase == 2) return;
                var game = GameSession.Instance;
                if (!game || !game.Player) return;
                if (game.SaveBlocked) throw new Exception("Save blocked: " + game.Toast);
                if (game.RuntimeErrors.Count > 0) throw new Exception(string.Join(";", game.RuntimeErrors));
                if (!game.NavigationReady || game.IsRestoring) return;
                if (phase == 0)
                {
                    Require(game.CanSimulate && game.Player.CanControl, "player can control after Enter Play");
                    Require(game.Transactions != null, "transaction core restored");
                    Require(game.Stations.All(station => station), "station references are alive");
                    Require(game.Stations.Where(station => station is not StationZone)
                        .Select(station => station.Id).Distinct().Count() ==
                        game.Stations.Count(station => station is not StationZone), "station IDs stay unique");
                    audit.graphicsDevice = SystemInfo.graphicsDeviceName;
                    Require(audit.graphicsDevice.Contains("4060"), "Editor uses RTX 4060");
                    startingPosition = game.Player.transform.position;
                    gamepad = InputSystem.AddDevice<Gamepad>("QA_EditorRecoveryGamepad");
                    InputSystem.QueueStateEvent(gamepad, new GamepadState { leftStick = Vector2.down });
                    moveUntil = EditorApplication.timeSinceStartup + 1.2;
                    phase = 1;
                    return;
                }
                if (EditorApplication.timeSinceStartup < moveUntil) return;
                InputSystem.QueueStateEvent(gamepad, new GamepadState());
                InputSystem.RemoveDevice(gamepad);
                gamepad = null;
                Require(Vector3.Distance(startingPosition, game.Player.transform.position) > 1,
                    "real Input System movement works");
                game.SaveGame();
                Require(File.Exists(game.SavePath) && !game.SaveBlocked, "save after Play succeeds");
                audit.completedSessions++;
                SessionState.SetString(ReportKey, JsonUtility.ToJson(audit));
                phase = 2;
                EditorApplication.ExitPlaymode();
            }
            catch (Exception error)
            {
                Finish(false, error.ToString());
            }
        }

        static void Require(bool condition, string label)
        {
            if (!condition) throw new Exception(label);
            audit.checks.Add("Play " + (audit.completedSessions + 1) + ": " + label);
        }

        static void Finish(bool passed, string failure)
        {
            SessionState.SetBool(ActiveKey, false);
            EditorApplication.update -= Tick;
            EditorApplication.playModeStateChanged -= PlayChanged;
            if (gamepad != null && gamepad.added) InputSystem.RemoveDevice(gamepad);
            audit.passed = passed;
            audit.failure = failure;
            Directory.CreateDirectory(Path.GetDirectoryName(ReportPath));
            File.WriteAllText(ReportPath, JsonUtility.ToJson(audit, true));
            Debug.Log("EDITOR_SAVE_RECOVERY " + (passed ? "PASS" : "FAIL " + failure));
            EditorApplication.Exit(passed ? 0 : 2);
        }

        [Serializable]
        sealed class PlayAudit
        {
            public bool passed;
            public string failure;
            public string graphicsDevice;
            public int completedSessions;
            public List<string> checks = new();
        }
    }
}
