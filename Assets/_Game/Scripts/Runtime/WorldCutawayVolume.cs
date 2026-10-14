using System.Collections.Generic;
using UnityEngine;

namespace Tycoon
{
    /// <summary>
    /// Hides an upper shell while a configured player is inside a volume.
    /// </summary>
    public sealed class WorldCutawayVolume : MonoBehaviour
    {
        [Header("Upper shell")]
        public List<Renderer> upperShellRenderers = new List<Renderer>();
        public List<GameObject> upperShellObjects = new List<GameObject>();

        [Header("Player filter")]
        public string playerTag = "Player";
        public bool usePlayerLayer;
        public LayerMask playerLayerMask = ~0;

        [Header("Trigger and hysteresis")]
        public bool useTriggerCallbacks = true;
        [Min(0f)]
        public float enterHysteresisSeconds;
        [Min(0f)]
        public float exitHysteresisSeconds;

        readonly Dictionary<Renderer, bool> initialRendererStates =
            new Dictionary<Renderer, bool>();
        readonly Dictionary<GameObject, bool> initialObjectStates =
            new Dictionary<GameObject, bool>();
        readonly HashSet<Collider> playerColliders = new HashSet<Collider>();

        bool desiredPlayerInside;
        bool transitionPending;
        bool pendingRevealState;
        float transitionDeadline;

        /// <summary>
        /// True when the upper shell is hidden and the interior is revealed.
        /// </summary>
        public bool IsRevealed { get; private set; }

        void Awake()
        {
            ConfigureOptionalTrigger();
            CacheConfiguredStates();
            ApplyRevealState(false);
        }

        void OnEnable()
        {
            ConfigureOptionalTrigger();
        }

        void OnValidate()
        {
            enterHysteresisSeconds = Mathf.Max(0f, enterHysteresisSeconds);
            exitHysteresisSeconds = Mathf.Max(0f, exitHysteresisSeconds);
            ConfigureOptionalTrigger();
        }

        void Update()
        {
            if (!transitionPending || Time.unscaledTime < transitionDeadline)
                return;

            transitionPending = false;
            ApplyRevealState(pendingRevealState);
        }

        /// <summary>
        /// Requests a player-inside state. A non-zero hysteresis delays the change.
        /// </summary>
        public void SetPlayerInside(bool inside)
        {
            desiredPlayerInside = inside;

            if (inside == IsRevealed && !transitionPending)
                return;

            float delay = inside ? enterHysteresisSeconds : exitHysteresisSeconds;
            if (delay <= 0f)
            {
                transitionPending = false;
                ApplyRevealState(inside);
                return;
            }

            pendingRevealState = inside;
            transitionDeadline = Time.unscaledTime + delay;
            transitionPending = true;
        }

        /// <summary>
        /// Adds a shell renderer and preserves its current enabled state on restore.
        /// </summary>
        public void RegisterRenderer(Renderer renderer)
        {
            if (renderer == null)
                return;

            if (!upperShellRenderers.Contains(renderer))
                upperShellRenderers.Add(renderer);

            if (!initialRendererStates.ContainsKey(renderer))
                initialRendererStates.Add(renderer, renderer.enabled);

            if (IsRevealed)
                renderer.enabled = false;
        }

        /// <summary>
        /// Removes a shell renderer and restores its state before unregistering it.
        /// </summary>
        public void UnregisterRenderer(Renderer renderer)
        {
            if (renderer == null)
                return;

            if (initialRendererStates.TryGetValue(renderer, out bool initialState))
                renderer.enabled = initialState;

            upperShellRenderers.Remove(renderer);
            initialRendererStates.Remove(renderer);
        }

        void OnTriggerEnter(Collider other)
        {
            if (!useTriggerCallbacks || !IsPlayerCollider(other))
                return;

            if (playerColliders.Add(other))
                SetPlayerInside(true);
        }

        void OnTriggerExit(Collider other)
        {
            if (!useTriggerCallbacks || other == null)
                return;

            if (!playerColliders.Remove(other))
                return;

            if (playerColliders.Count == 0)
                SetPlayerInside(false);
        }

        bool IsPlayerCollider(Collider other)
        {
            if (other == null)
                return false;

            Transform candidate = other.transform;
            while (candidate != null)
            {
                bool tagMatches = string.IsNullOrEmpty(playerTag)
                    || candidate.gameObject.tag == playerTag;
                bool layerMatches = !usePlayerLayer
                    || (playerLayerMask.value & (1 << candidate.gameObject.layer)) != 0;
                if (tagMatches && layerMatches)
                    return true;

                candidate = candidate.parent;
            }

            return false;
        }

        void ConfigureOptionalTrigger()
        {
            if (!useTriggerCallbacks)
                return;

            BoxCollider trigger = GetComponent<BoxCollider>();
            if (trigger != null)
                trigger.isTrigger = true;
        }

        void CacheConfiguredStates()
        {
            foreach (Renderer renderer in upperShellRenderers)
            {
                if (renderer != null && !initialRendererStates.ContainsKey(renderer))
                    initialRendererStates.Add(renderer, renderer.enabled);
            }

            foreach (GameObject shellObject in upperShellObjects)
            {
                if (shellObject != null && !initialObjectStates.ContainsKey(shellObject))
                    initialObjectStates.Add(shellObject, shellObject.activeSelf);
            }
        }

        void ApplyRevealState(bool revealed)
        {
            CacheConfiguredStates();
            IsRevealed = revealed;

            foreach (KeyValuePair<Renderer, bool> entry in initialRendererStates)
            {
                if (entry.Key != null)
                    entry.Key.enabled = revealed ? false : entry.Value;
            }

            foreach (KeyValuePair<GameObject, bool> entry in initialObjectStates)
            {
                if (entry.Key != null)
                    entry.Key.SetActive(revealed ? false : entry.Value);
            }
        }
    }
}
