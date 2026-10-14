using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Tycoon
{
    public sealed partial class QaDriver
    {
        readonly List<SprintSample> sprintSamples = new();

        IEnumerator PlayerSprintAcceptance()
        {
            yield return CompletionWait(() => game.NavigationReady && game.Player.CanControl, 30,
                "Sprint: world and player ready");
            InputSystem.RemoveDevice(gamepad);
            yield return new WaitForSeconds(.2f);
            yield return Capture("01a-shift-controls.png");
            gamepad = InputSystem.AddDevice<Gamepad>("QA_Gamepad");
            Check(!game.Player.HasCarry, "Sprint: empty hands at start");
            // Hướng W ở spawn đi thẳng vào quầy Farm; đo về phía đường trống để không đo tốc độ va tường.
            yield return SprintSampleMotion("walk", 4.8f, false, Key.S);
            yield return SprintSampleMotion("left-shift", 10f, true, Key.S, Key.LeftShift);
            yield return SprintSampleMotion("right-shift", 10f, true, Key.S, Key.RightShift);
            yield return SprintSampleMotion("diagonal", 10f, true, Key.S, Key.A, Key.LeftShift);
            Check(Mathf.Abs(sprintSamples[1].speed / sprintSamples[0].speed - 10f / 4.8f) < .05f,
                "Sprint: measured speed ratio matches existing movement speeds");

            Keys(Key.LeftShift);
            yield return new WaitForSeconds(.2f);
            Check(game.Player.CurrentSpeed < .1f && !game.Player.IsSprinting,
                "Sprint: Shift alone cannot move or run");
            Keys(Key.S, Key.LeftShift);
            yield return new WaitForSeconds(.3f);
            Keys(Key.S);
            yield return new WaitForSeconds(.2f);
            Check(!game.Player.IsSprinting && Mathf.Abs(game.Player.CurrentSpeed - 4.8f) < .15f,
                "Sprint: release Shift returns to walking while movement stays held");

            Keys(Key.S, Key.LeftShift, Key.Escape);
            yield return new WaitForSecondsRealtime(.25f);
            Check(!game.Player.CanControl && !game.Player.IsSprinting && game.Player.CurrentSpeed == 0,
                "Sprint: pause clears motion even with Shift held");
            Vector3 paused = game.Player.transform.position;
            Keys();
            yield return new WaitForSecondsRealtime(.25f);
            Check(Vector3.Distance(paused, game.Player.transform.position) < .01f,
                "Sprint: paused player remains still");
            Keys(Key.Escape);
            yield return new WaitForSecondsRealtime(.2f);
            Keys();
            yield return new WaitForSeconds(.2f);
            Check(game.Player.CanControl && !game.Player.IsSprinting && game.Player.CurrentSpeed < .1f,
                "Sprint: resume does not retain released input");
            yield return Travel(CityDistricts.PlayerSpawn);
        }

        IEnumerator SprintSampleMotion(string name, float expectedSpeed, bool sprinting, params Key[] keys)
        {
            Vector3 origin = game.Player.transform.position;
            Keys(keys);
            // v/a cộng hai frame cho input và quan sát, rồi mới đo quãng đường thật.
            yield return new WaitForSeconds(expectedSpeed / 32f + 2 * Time.deltaTime);
            float start = Time.time;
            Vector3 before = game.Player.transform.position;
            yield return new WaitForSeconds(.4f);
            Vector3 delta = game.Player.transform.position - before;
            delta.y = 0;
            float speed = delta.magnitude / (Time.time - start);
            sprintSamples.Add(new SprintSample { name = name, speed = speed,
                currentSpeed = game.Player.CurrentSpeed, sprinting = game.Player.IsSprinting,
                animation = game.Player.View.State });
            File.WriteAllText(Path.Combine(game.QaDirectory, "sprint-measurements.json"),
                JsonUtility.ToJson(new SprintMeasurements { measuredUtc = DateTime.UtcNow.ToString("o"),
                    samples = sprintSamples.ToArray() }, true));
            Check(Mathf.Abs(speed - expectedSpeed) < .15f,
                "Sprint: " + name + " actual speed " + speed.ToString("F3"));
            Check(game.Player.IsSprinting == sprinting, "Sprint: " + name + " sprint state");
            Check(game.Player.View.State == (game.Player.HasCarry ? "CarryWalk" : sprinting ? "Run" : "Walk"),
                "Sprint: " + name + " animation");
            Keys();
            yield return new WaitForSeconds(expectedSpeed / 46f + 2 * Time.deltaTime);
            Check(game.Player.CurrentSpeed < .1f && !game.Player.IsSprinting,
                "Sprint: " + name + " stops on release");
            yield return Travel(origin);
        }

        [Serializable] sealed class SprintMeasurements
        {
            public string measuredUtc;
            public SprintSample[] samples;
        }
        [Serializable] sealed class SprintSample
        {
            public string name, animation;
            public float speed, currentSpeed;
            public bool sprinting;
        }
    }
}
