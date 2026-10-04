using UnityEngine;
using UnityEngine.InputSystem;

namespace Tycoon
{
    public sealed class PlayerController : MonoBehaviour
    {
        [System.NonSerialized] public Inventory Carry = new(6, true);
        public CharacterController Controller;
        public ActorView View;
        public float DistanceWalked { get; private set; }
        public int SuccessfulInteractions { get; private set; }
        public InputAction Move;
        public InputAction Use;
        public InputAction Sprint;
        public InputAction Withdraw;
        public InputAction Cycle;
        public InputAction Save;
        float nextUse;
        Station activeStation;
        public bool CanControl = true;
        public void Initialize()
        {
            Controller = gameObject.AddComponent<CharacterController>();
            Controller.height = 1.8f; Controller.radius = .26f; Controller.center = new Vector3(0, .91f, 0);
            Controller.stepOffset = .28f; Controller.skinWidth = .035f; Controller.slopeLimit = 45;
            Move = new InputAction("Move", InputActionType.Value);
            Move.AddCompositeBinding("2DVector").With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s").With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
            Move.AddBinding("<Gamepad>/leftStick");
            Use = new InputAction("Use", InputActionType.Button); Use.AddBinding("<Keyboard>/e"); Use.AddBinding("<Gamepad>/buttonSouth");
            Sprint = new InputAction("Sprint", InputActionType.Button); Sprint.AddBinding("<Keyboard>/leftShift"); Sprint.AddBinding("<Gamepad>/leftStickPress");
            Withdraw = new InputAction("Withdraw", InputActionType.Button); Withdraw.AddBinding("<Keyboard>/r"); Withdraw.AddBinding("<Gamepad>/buttonWest");
            Cycle = new InputAction("Cycle", InputActionType.Button); Cycle.AddBinding("<Keyboard>/q"); Cycle.AddBinding("<Gamepad>/rightShoulder");
            Save = new InputAction("Save", InputActionType.Button); Save.AddBinding("<Keyboard>/f5"); Save.AddBinding("<Gamepad>/start");
            Move.Enable(); Use.Enable(); Sprint.Enable(); Withdraw.Enable(); Cycle.Enable(); Save.Enable();
            View = GetComponentInChildren<ActorView>();
            View.Initialize();
        }
        void Update()
        {
            if (!CanControl || Move == null || GameSession.Instance == null) return;
            var game = GameSession.Instance;
            if (Cycle.WasPressedThisFrame()) game.SelectedItem = (game.SelectedItem + 1) % Definitions.Items.Length;
            if (Save.WasPressedThisFrame()) game.SaveGame();
            Vector2 input = Move.ReadValue<Vector2>();
            Vector3 forward = Camera.main.transform.forward; forward.y = 0; forward.Normalize();
            Vector3 right = Camera.main.transform.right; right.y = 0; right.Normalize();
            var direction = Vector3.ClampMagnitude(forward * input.y + right * input.x, 1);
            float speed = Sprint.IsPressed() && Carry.Total == 0 ? 6.2f : Carry.Total > 0 ? 3.8f : 4.8f;
            Vector3 before = transform.position;
            Controller.Move((direction * speed + Vector3.down * 3) * Time.deltaTime);
            Vector3 travelled = transform.position - before; travelled.y = 0;
            DistanceWalked += travelled.magnitude;
            if (direction.sqrMagnitude > .01f)
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(direction), 540 * Time.deltaTime);
            View.SetMotion(travelled.magnitude / Mathf.Max(Time.deltaTime, .001f), Carry.Total > 0);
            if (direction.sqrMagnitude < .01f || Use.IsPressed() || Withdraw.IsPressed())
                InteractAtCurrentPosition(Time.deltaTime, Withdraw.IsPressed());
            else StopInteraction();
        }
        public bool InteractAtCurrentPosition(float delta, bool withdraw = false)
        {
            if (!CanControl || delta <= 0) { StopInteraction(); return false; }
            var game = GameSession.Instance;
            var station = game.NearestStation(transform.position);
            if (activeStation != station) { StopInteraction(); activeStation = station; }
            if (!station) return false;
            int carriedBefore = Carry.Total;
            bool worked;
            if (station is PurchasePad pad)
                worked = !withdraw && pad.HoldToBuy(delta);
            else if (station is StationZone zone && zone.Mode != "cash")
                worked = zone.Work(Carry, delta, GetEntityId());
            else if (station is ProductionStation || station is MachineStation || station is TableStation)
                worked = station.Work(Carry, delta, GetEntityId());
            else
            {
                if (Time.time < nextUse) return false;
                worked = station.Interact(this, withdraw);
                nextUse = Time.time + .15f;
            }
            if (worked)
            {
                SuccessfulInteractions++;
                if (Carry.Total != carriedBefore)
                {
                    View?.Interact(Carry.Total > carriedBefore);
                    game.Feedback?.PlayPickup();
                }
            }
            return worked;
        }
        public void StopInteraction()
        {
            if (activeStation is PurchasePad pad) pad.StopContributing();
            var target = activeStation is StationZone zone ? zone.Target : activeStation;
            if (target) target.ReleaseOperator(GetEntityId());
            activeStation = null;
        }
        void OnDestroy()
        {
            Move?.Dispose(); Use?.Dispose(); Sprint?.Dispose(); Withdraw?.Dispose(); Cycle?.Dispose(); Save?.Dispose();
        }
    }
}

