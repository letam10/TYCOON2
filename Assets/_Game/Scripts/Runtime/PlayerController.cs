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
        public float CurrentSpeed { get; private set; }
        public bool IsSprinting { get; private set; }
        public int SuccessfulInteractions { get; private set; }
        public InputAction Move;
        public InputAction Use;
        public InputAction Sprint;
        public InputAction Withdraw;
        public InputAction Cycle;
        public InputAction Save;
        readonly PlayerInteractionSession interaction = new();
        Vector3 movement;
        bool canControl = true;
        public IPlayerInteractionArea ActiveInteraction => interaction.Current;
        public string InteractionReason { get; private set; } = "";
        public bool CanControl
        {
            get => canControl;
            set { canControl = value; if (!value) { ResetMotion(); StopInteraction(); } }
        }
        public void Initialize()
        {
            if (Move != null) return;
            Controller = GetComponent<CharacterController>();
            if (!Controller) Controller = gameObject.AddComponent<CharacterController>();
            Controller.height = 1.8f; Controller.radius = .26f; Controller.center = new Vector3(0, .91f, 0);
            Controller.stepOffset = .28f; Controller.skinWidth = .035f; Controller.slopeLimit = 45;
            Move = new InputAction("Move", InputActionType.Value);
            Move.AddCompositeBinding("2DVector").With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s").With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
            Move.AddCompositeBinding("2DVector").With("Up", "<Keyboard>/upArrow").With("Down", "<Keyboard>/downArrow").With("Left", "<Keyboard>/leftArrow").With("Right", "<Keyboard>/rightArrow");
            Move.AddBinding("<Gamepad>/leftStick");
            Use = new InputAction("Use", InputActionType.Button); Use.AddBinding("<Keyboard>/e"); Use.AddBinding("<Gamepad>/buttonSouth");
            Sprint = new InputAction("Sprint", InputActionType.Button); Sprint.AddBinding("<Keyboard>/leftShift"); Sprint.AddBinding("<Gamepad>/leftStickPress");
            Withdraw = new InputAction("Withdraw", InputActionType.Button); Withdraw.AddBinding("<Keyboard>/r"); Withdraw.AddBinding("<Gamepad>/buttonWest");
            Cycle = new InputAction("Cycle", InputActionType.Button); Cycle.AddBinding("<Keyboard>/q"); Cycle.AddBinding("<Gamepad>/rightShoulder");
            Save = new InputAction("Save", InputActionType.Button); Save.AddBinding("<Keyboard>/f5"); Save.AddBinding("<Gamepad>/start");
            Move.Enable(); Use.Enable(); Sprint.Enable(); Withdraw.Enable(); Cycle.Enable(); Save.Enable();
            View = GetComponentInChildren<ActorView>();
            View?.Initialize();
        }
        void Update()
        {
            if (!CanControl || Move == null || GameSession.Instance == null || !GameSession.Instance.CanSimulate) { ResetMotion(); StopInteraction(); return; }
            var game = GameSession.Instance;
            if (Cycle.WasPressedThisFrame()) { game.SelectedItem = (game.SelectedItem + 1) % Definitions.Items.Length; StopInteraction(); }
            if (Save.WasPressedThisFrame()) game.SaveGame();
            Vector2 input = Move.ReadValue<Vector2>();
            var direction = CameraRelativeDirection(input, Camera.main ? Camera.main.transform : null);
            float speed = Sprint.IsPressed() && Carry.Total == 0 ? 6.2f : Carry.Total > 0 ? 3.8f : 4.8f;
            // Tăng tốc ngắn, dừng nhanh để vẫn thao tác chính xác khi đứng sát trạm.
            bool braking = direction.sqrMagnitude < .0025f || Vector3.Dot(movement, direction) < 0;
            movement = Vector3.MoveTowards(movement, direction * speed, (braking ? 46 : 32) * Time.deltaTime);
            Vector3 before = transform.position;
            var goal=before+movement*Time.deltaTime;goal.x=Mathf.Clamp(goal.x,-48,58);goal.z=Mathf.Clamp(goal.z,-18,58);
            Controller.Move(goal-before+Vector3.down*3*Time.deltaTime);
            Vector3 travelled = transform.position - before; travelled.y = 0;
            DistanceWalked += travelled.magnitude;
            CurrentSpeed = travelled.magnitude / Mathf.Max(Time.deltaTime, .001f);
            IsSprinting = Carry.Total == 0 && Sprint.IsPressed() && CurrentSpeed > 5.5f;
            if (direction.sqrMagnitude > .01f)
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(direction), 540 * Time.deltaTime);
            View?.SetMotion(CurrentSpeed, Carry.Total > 0);
            InteractAtCurrentPosition(Time.deltaTime);
        }
        public static Vector3 CameraRelativeDirection(Vector2 input, Transform camera)
        {
            Vector3 forward = camera ? Vector3.ProjectOnPlane(camera.forward, Vector3.up) : Vector3.forward;
            if (forward.sqrMagnitude < .0001f) forward = Vector3.forward;
            forward.Normalize();
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            return Vector3.ClampMagnitude(forward * input.y + right * input.x, 1);
        }
        public bool InteractAtCurrentPosition(float delta, bool withdraw = false)
        {
            if (!CanControl || delta <= 0 || !float.IsFinite(delta) || !GameSession.Instance || !GameSession.Instance.CanSimulate) { StopInteraction(); return false; }
            // Đã dừng trong vùng mới thao tác; đi ngang kho không tự nhặt nhầm loại hàng.
            if(CurrentSpeed>.25f||Move!=null&&Move.ReadValue<Vector2>().sqrMagnitude>.0025f){StopInteraction();return false;}
            var game = GameSession.Instance;
            int carriedBefore = Carry.Total;
            var before = interaction.Current;
            string previousReason = InteractionReason;
            var result = interaction.Tick(game.PlayerInteractionAreas(),
                new InteractionContext(this, Definitions.Items[game.SelectedItem].id), transform.position, delta);
            if (!ReferenceEquals(before, interaction.Current) || result.Worked || interaction.Current == null) InteractionReason = "";
            if (!string.IsNullOrEmpty(result.Reason)) InteractionReason = result.Reason;
            if (!string.IsNullOrEmpty(result.Reason) && (!ReferenceEquals(before, interaction.Current) || result.Reason != previousReason))
                game.Feedback?.PlayBlocked();
            if (result.Worked)
            {
                SuccessfulInteractions++;
                if (Carry.Total != carriedBefore)
                {
                    View?.Interact(Carry.Total > carriedBefore);
                    game.Feedback?.ItemTransfer(transform.position + Vector3.up * .85f, Carry.Total > carriedBefore);
                }
                else
                {
                    Station target = interaction.Current is StationZone zone ? zone.Target : interaction.Current is ProximityTarget proximity ? proximity.Target : null;
                    if (interaction.Current?.Kind == InteractionKind.Collect)
                        game.Feedback?.Collect(transform.position + Vector3.up * .9f);
                    else if (target && interaction.Current.Kind is InteractionKind.Operate or InteractionKind.Serve or InteractionKind.Repair)
                    {
                        View?.Work(ActorView.WorkState(target, interaction.Current.Kind));
                        game.Feedback?.PlayWork();
                    }
                }
            }
            return result.Worked;
        }
        public void StopInteraction()
        {
            interaction.Stop(); InteractionReason = "";
        }
        void ResetMotion() { movement = Vector3.zero; CurrentSpeed = 0; IsSprinting = false; View?.SetMotion(0, Carry.Total > 0); }
        void OnEnable() { Move?.Enable(); Use?.Enable(); Sprint?.Enable(); Withdraw?.Enable(); Cycle?.Enable(); Save?.Enable(); }
        void OnDisable() { ResetMotion(); StopInteraction(); Move?.Disable(); Use?.Disable(); Sprint?.Disable(); Withdraw?.Disable(); Cycle?.Disable(); Save?.Disable(); }
        void OnDestroy()
        {
            StopInteraction();
            Move?.Dispose(); Use?.Dispose(); Sprint?.Dispose(); Withdraw?.Dispose(); Cycle?.Dispose(); Save?.Dispose();
        }
    }
}

