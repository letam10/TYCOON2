using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;

namespace Tycoon
{
    public sealed partial class CityPlaySession
    {
        IEnumerator Walk(Vector3 destination)
        {
            var path = new NavMeshPath();
            if (!NavMesh.SamplePosition(destination, out var hit, 2, NavMesh.AllAreas) ||
                !NavMesh.CalculatePath(game.Player.transform.position, hit.position, NavMesh.AllAreas, path) ||
                path.status != NavMeshPathStatus.PathComplete)
                throw new InvalidOperationException("Không đi được tới " + destination);
            float deadline = Time.realtimeSinceStartup + 95;
            foreach (var corner in path.corners.Skip(1))
            {
                Vector3 target = corner;
                while (Vector3.ProjectOnPlane(target - game.Player.transform.position, Vector3.up).magnitude > .22f)
                {
                    if (Time.realtimeSinceStartup > deadline)
                    {
                        CityCollisionEvidence.Save(game, destination, target, path.corners);
                        throw new InvalidOperationException("Di chuyển bị kẹt tới " + destination);
                    }
                    Vector3 direction = Vector3.ProjectOnPlane(target - game.Player.transform.position, Vector3.up);
                    float remaining = direction.magnitude;
                    direction.Normalize();
                    Vector3 forward = Vector3.ProjectOnPlane(Camera.main.transform.forward, Vector3.up).normalized;
                    Vector3 right = Vector3.Cross(Vector3.up, forward);
                    var stick = new Vector2(Vector3.Dot(direction, right), Vector3.Dot(direction, forward));
                    float speed = game.Player.HasCarry ? 3.8f : 6.2f;
                    stick *= Mathf.Clamp(remaining / (speed * .12f), .15f, 1);
                    var state = new GamepadState { leftStick = stick };
                    if (remaining > 2 && !game.Player.HasCarry) state = state.WithButton(GamepadButton.LeftStick);
                    InputSystem.QueueStateEvent(pad, state);
                    yield return null;
                }
            }
            StopInput();
            yield return Hold(.3f);
        }

        void StopInput() => InputSystem.QueueStateEvent(pad, new GamepadState());

        IEnumerator Wait(Func<bool> condition, float timeout, string label)
        {
            StopInput();
            float end = Time.realtimeSinceStartup + timeout;
            while (!condition() && Time.realtimeSinceStartup < end) yield return null;
            Check(condition(), label);
            if (!condition()) throw new InvalidOperationException(label + ": " + game.Player.InteractionReason);
        }

        IEnumerator Hold(float seconds)
        {
            StopInput();
            float end = Time.realtimeSinceStartup + seconds;
            while (Time.realtimeSinceStartup < end) yield return null;
        }

        IEnumerator Click(string label)
        {
            StopInput();
            var button = FindObjectsByType<Button>().FirstOrDefault(x =>
                x.isActiveAndEnabled && x.interactable && x.GetComponentsInChildren<Text>()
                    .Any(text => text.text == label && text.GetComponentInParent<Button>() == x));
            if (!button) throw new InvalidOperationException("Không tìm thấy nút " + label);
            ExecuteEvents.Execute(button.gameObject, new PointerEventData(EventSystem.current),
                ExecuteEvents.pointerClickHandler);
            yield return null;
        }

        IEnumerator StoreGoods(StorageStation storage)
        {
            yield return Walk(storage.transform.position + Vector3.back * 2);
            yield return Wait(() => game.Player.Carry.Total == 0, 12, "Cất hàng vào kho thật");
        }

        IEnumerator Capture(string name)
        {
            StopInput();
            yield return Hold(.25f);
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(game.QaDirectory, name + ".png"));
            captures.Add(name + ".png");
            yield return null;
        }
    }
}
