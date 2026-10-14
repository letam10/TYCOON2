using System.Linq;
using NUnit.Framework;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

namespace Tycoon.Tests
{
    public sealed class DinerSeatingTests : CityPedestrianPlayModeFixture
    {
        GameObject root;
        GameObject actor;
        Transform seat;
        Transform hips;
        ActorView view;
        DinerSeatingPose pose;
        NavMeshAgent agent;
        CharacterController body;
        NavMeshData data;
        GameSession previousSession;
        readonly Vector3 approach = new(2, 0, 1);

        protected override void CreateWorld()
        {
            root = new GameObject("DinerSeatingTests");
            previousSession = GameSession.Instance;
            seat = new GameObject("HipAnchor").transform;
            seat.SetParent(root.transform, false);
            seat.localPosition = new(1.2f, .57f, 0);
            seat.localRotation = Quaternion.Euler(0, -90, 0);
        }

        protected override void ClearWorld()
        {
            Object.DestroyImmediate(root);
            GameSession.Instance = previousSession;
            if (data) Object.DestroyImmediate(data);
        }

        void Actor(string key, bool withNavigation = false)
        {
            actor = new GameObject("Diner");
            actor.transform.SetParent(root.transform, false);
            actor.transform.position = approach;
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_Game/Art/Imported/Models/" + key + ".prefab");
            Assert.That(prefab, Is.Not.Null);
            var model = Object.Instantiate(prefab, actor.transform);
            view = model.AddComponent<ActorView>();
            view.Initialize();
            view.Animator.Rebind();
            view.Animator.Update(0);
            hips = model.GetComponentsInChildren<Transform>().Single(x => x.name == "Hips");
            if (withNavigation)
            {
                // Agent natif chỉ đăng ký trong PlayMode; fixture cơ sở chịu trách nhiệm chuyển chế độ.
                Assert.That(Application.isPlaying, Is.True);
                var floor = new GameObject("DiningTestFloor", typeof(BoxCollider));
                floor.transform.SetParent(root.transform, false);
                floor.transform.localPosition = Vector3.down * .1f;
                floor.GetComponent<BoxCollider>().size = new(20, .2f, 20);
                Physics.SyncTransforms();
                var surface = floor.AddComponent<NavMeshSurface>();
                surface.collectObjects = CollectObjects.Children;
                surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
                surface.BuildNavMesh();
                data = surface.navMeshData;
                Assert.That(data, Is.Not.Null);
                Assert.That(NavMesh.SamplePosition(approach, out var hit, 1, NavMesh.AllAreas), Is.True);
                actor.transform.position = hit.position;
                var diner = actor.AddComponent<DinerAgent>();
                diner.Initialize();
                agent = diner.Agent;
                body = actor.GetComponent<CharacterController>();
                Assert.That(agent.isOnNavMesh, Is.True, "Native diner agent registered on the baked test floor");
                Assert.That(Navigation.Warp(agent, approach), Is.True);
            }
            pose = actor.GetComponent<DinerSeatingPose>();
            if (!pose)
            {
                pose = actor.AddComponent<DinerSeatingPose>();
                pose.Initialize(agent, view);
            }
        }

        [TestCase("customer")]
        [TestCase("customer_beach")]
        public void ActualSeatedHipMatchesAnchorWithNoExtraHeightOrFootOffset(string key)
        {
            Actor(key);
            pose.Sit(seat, approach, true);
            Assert.That(Vector3.Distance(hips.position, seat.position), Is.LessThan(.001f));
            Assert.That(Quaternion.Angle(actor.transform.rotation, seat.rotation), Is.LessThan(.01f));
            Assert.That(view.State, Is.EqualTo("Sit"));
            Assert.That(view.Animator.GetCurrentAnimatorStateInfo(0).IsName("Sit"), Is.True);
        }

        [Test]
        public void RepeatedSeatingDoesNotRestartTransitionOrReturnToWalking()
        {
            Actor("customer");
            Vector3 before = actor.transform.position;
            pose.Sit(seat, approach);
            Assert.That(actor.transform.position, Is.EqualTo(before));
            for (int i = 0; i < 90; i++)
            {
                pose.Sit(seat, approach);
                view.Animator.Update(1f / 60);
                pose.Advance(1f / 60);
                Assert.That(view.State, Is.EqualTo("Sit"));
            }
            Assert.That(pose.IsMoving, Is.False);
            Assert.That(Vector3.Distance(hips.position, seat.position), Is.LessThan(.001f));
            Assert.That(view.Animator.GetCurrentAnimatorStateInfo(0).IsName("Sit"), Is.True);
            Assert.That(view.Animator.GetCurrentAnimatorStateInfo(0).normalizedTime, Is.GreaterThan(.5f));
        }

        [Test]
        public void HipTargetTracksTranslatedAndRotatedFurniture()
        {
            Actor("customer_beach");
            pose.Sit(seat, approach, true);
            seat.position += new Vector3(-32, 0, 21);
            seat.rotation = Quaternion.Euler(0, 38, 0);
            pose.Advance(.1f);
            Assert.That(Vector3.Distance(hips.position, seat.position), Is.LessThan(.001f));
        }

        [Test]
        public void LeavingRestoresAgentAndCapsuleAtApproachAndCallsBackOnce()
        {
            Actor("customer", true);
            var previousCulling = view.Animator.cullingMode;
            pose.Sit(seat, approach, true);
            Assert.That(agent.enabled, Is.False);
            Assert.That(body.enabled, Is.False);
            Assert.That(view.Animator.cullingMode, Is.EqualTo(AnimatorCullingMode.AlwaysAnimate));
            int callbacks = 0;
            pose.Leave(() => callbacks++);
            pose.Advance(DinerSeatingPose.TransitionSeconds * .5f);
            Assert.That(agent.enabled, Is.False);
            Assert.That(body.enabled, Is.False);
            pose.Leave(() => callbacks += 10);
            pose.Advance(DinerSeatingPose.TransitionSeconds);
            pose.Advance(1);
            Assert.That(callbacks, Is.EqualTo(1));
            Assert.That(pose.Seat, Is.Null);
            Assert.That(agent.enabled && agent.isOnNavMesh && body.enabled, Is.True);
            Assert.That(Vector3.Distance(actor.transform.position, approach), Is.LessThan(.15f));
            Assert.That(view.Animator.cullingMode, Is.EqualTo(previousCulling));
            Assert.That(view.State, Is.EqualTo("Idle"));
        }

        [Test]
        public void PoolReuseCancelsLeavingCallbackAndRestoresWalkingBody()
        {
            Actor("customer", true);
            pose.Sit(seat, approach, true);
            int callbacks = 0;
            pose.Leave(() => callbacks++);
            actor.SetActive(false);
            actor.transform.position = Vector3.zero;
            actor.SetActive(true);
            Assert.That(Navigation.Warp(agent, Vector3.zero), Is.True);
            pose.Advance(1);
            Assert.That(callbacks, Is.Zero);
            Assert.That(pose.Seat, Is.Null);
            Assert.That(agent.enabled && body.enabled, Is.True);
            view.SetMotion(3, false);
            Assert.That(view.State, Is.EqualTo("Walk"));
            pose.Sit(seat, approach, true);
            Assert.That(Vector3.Distance(hips.position, seat.position), Is.LessThan(.001f));
        }

        [Test]
        public void RootAlignmentAccountsForModelScaleAndOffset()
        {
            Actor("customer");
            actor.transform.localScale = Vector3.one * 1.25f;
            view.transform.localPosition = new(.1f, .2f, -.1f);
            pose.Sit(seat, approach, true);
            Assert.That(Vector3.Distance(hips.position, seat.position), Is.LessThan(.001f));
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        public void RestoreAndPoolRoundTripPreservePhaseReceiptAndReachableSavePosition(int phase)
        {
            var session = new GameObject("Session");
            session.SetActive(false);
            session.transform.SetParent(root.transform);
            var game = session.AddComponent<GameSession>();
            GameSession.Instance = game;
            Actor("customer", true);
            var diner = actor.GetComponent<DinerAgent>();
            var tableObject = new GameObject("table_0");
            tableObject.transform.SetParent(root.transform);
            var table = tableObject.AddComponent<TableStation>();
            table.Id = "table_0";
            table.Seat = approach;
            table.Inventory = new Inventory(1);
            var furniture = tableObject.AddComponent<DiningFurniture>();
            furniture.Seats = new[] { seat };
            var saved = new DinerSave
            {
                table = table.Id,
                receipt = 77,
                item = "meal",
                remaining = phase == 2 ? 4 : 90,
                phase = phase,
                price = 600,
                x = approach.x,
                z = approach.z
            };
            for (int cycle = 0; cycle < 2; cycle++)
            {
                diner.Begin(table, saved);
                Assert.That(diner.Phase, Is.EqualTo(phase));
                Assert.That(diner.Receipt, Is.EqualTo(77));
                Assert.That(diner.Remaining, Is.EqualTo(saved.remaining).Within(.01f));
                Assert.That(pose.IsSeated, Is.EqualTo(phase is 1 or 2));
                Assert.That(agent.enabled && body.enabled, Is.EqualTo(phase is not (1 or 2)));
                if (phase is 1 or 2)
                    Assert.That(Vector3.Distance(hips.position, seat.position), Is.LessThan(.001f));
                saved = diner.Snapshot();
                Assert.That(saved.x, Is.EqualTo(approach.x).Within(.15f));
                Assert.That(saved.z, Is.EqualTo(approach.z).Within(.15f));
                actor.SetActive(false);
                actor.transform.position = approach;
                actor.SetActive(true);
                Assert.That(Navigation.Warp(agent, approach), Is.True);
            }
            Assert.That(game.Economy.Transactions, Is.Zero);
            Assert.That(game.NextReceipt, Is.EqualTo(1));
        }
    }
}
