using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Tycoon.Tests
{
    public sealed class Stage4RBoardingCutawayTests
    {
        GameObject root;

        [TearDown]
        public void TearDown()
        {
            if (root)
                Object.DestroyImmediate(root);
        }

        [Test]
        public void Stage4RClosedDoorTryBoardLeavesPlayerStateUntouched()
        {
            root = new GameObject("Stage4RClosedDoorRoot");
            PlayerController player = CreatePlayer("Stage4RPlayer");
            player.transform.position = new Vector3(4f, 0f, -2f);
            Vector3 originalPosition = player.transform.position;
            Transform originalParent = player.transform.parent;
            TestCarrier carrier = CreateCarrier("Stage4RClosedCarrier");
            carrier.DoorsOpenValue = false;

            bool boarded = WorldPlayerRide.TryBoard(
                player,
                carrier,
                carrier.transform,
                5f,
                "ride",
                out string failure);

            Assert.That(boarded, Is.False);
            Assert.That(failure, Does.Contain("cửa"));
            Assert.That(player.transform.parent, Is.EqualTo(originalParent));
            Assert.That(player.transform.position, Is.EqualTo(originalPosition));
            Assert.That(player.Controller.enabled, Is.True);
            Assert.That(player.CanControl, Is.True);
            Assert.That(player.GetComponent<WorldPlayerRide>(), Is.Null);
            Assert.That(carrier.Boardings, Is.Zero);
        }

        [Test]
        public void Stage4ROpenCarrierUsesInsideDoorThenRecordsSafeAlighting()
        {
            root = new GameObject("Stage4RBoardingRoot");
            PlayerController player = CreatePlayer("Stage4RPlayer");
            TestCarrier carrier = CreateCarrier("Stage4ROpenCarrier");
            carrier.DoorsOpenValue = true;
            carrier.DoorInsideValue = new Vector3(.7f, 1.35f, -.25f);
            carrier.DoorOutsideLocal = new Vector3(0f, .05f, 3f);

            bool boarded = WorldPlayerRide.TryBoard(
                player,
                carrier,
                carrier.transform,
                5f,
                "ride",
                out string failure);

            Assert.That(boarded, Is.True, failure);
            Assert.That(player.transform.parent, Is.EqualTo(carrier.transform));
            Assert.That(player.transform.localPosition, Is.EqualTo(carrier.DoorInsideLocal));
            Assert.That(player.Controller.enabled, Is.False);
            Assert.That(player.CanControl, Is.False);
            Assert.That(carrier.Boardings, Is.EqualTo(1));

            carrier.transform.position = new Vector3(10f, 0f, 6f);
            WorldPlayerRide ride = player.GetComponent<WorldPlayerRide>();
            MethodInfo endRide = typeof(WorldPlayerRide).GetMethod(
                "EndRide",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(endRide, Is.Not.Null);
            endRide.Invoke(ride, null);

            Assert.That(player.transform.parent, Is.EqualTo(root.transform));
            Assert.That(player.Controller.enabled, Is.True);
            Assert.That(player.CanControl, Is.True);
            Assert.That(carrier.Alightings, Is.EqualTo(1));
            Assert.That(Vector3.Distance(player.transform.position, carrier.DoorOutside), Is.LessThan(.001f));
        }

        [Test]
        public void Stage4RInteractionAreaUsesApproachRadiusAndVerticalBounds()
        {
            root = new GameObject("Stage4RInteractionRoot");
            root.transform.position = new Vector3(8f, 2f, -5f);
            WorldInteractionArea area = WorldInteractionArea.Add(
                root.transform,
                "stage4r_approach",
                "Board",
                "Board",
                WorldInteractionAction.BoardTrain,
                new Vector3(2f, 0f, -1f),
                2f);

            Assert.That(area.ApproachPoint, Is.EqualTo(area.Center));
            Assert.That(area.Contains(area.Center), Is.True);
            Assert.That(area.Contains(area.Center + Vector3.right * 1.99f), Is.True);
            Assert.That(area.Contains(area.Center + Vector3.right * 2.01f), Is.False);
            Assert.That(area.Contains(area.Center + Vector3.up * 3.19f), Is.True);
            Assert.That(area.Contains(area.Center + Vector3.up * 3.21f), Is.False);
        }

        [Test]
        public void Stage4RInteractionAreaClosedCarrierGateDoesNotEnter()
        {
            root = new GameObject("Stage4RGatedInteractionRoot");
            WorldInteractionArea area = WorldInteractionArea.Add(
                root.transform,
                "stage4r_gate",
                "Board",
                "Board",
                WorldInteractionAction.BoardTrain,
                Vector3.zero,
                2f);
            TestCarrier carrier = CreateCarrier("Stage4RGatedCarrier");
            carrier.DoorsOpenValue = false;
            area.CarrierSource = carrier;
            PlayerController player = CreatePlayer("Stage4RGatedPlayer");
            player.transform.position = area.Center;

            bool entered = area.Enter(player, out string failure);

            Assert.That(entered, Is.False);
            Assert.That(failure, Does.Contain("cửa"));
            Assert.That(area.IsInside, Is.False);

            carrier.DoorsOpenValue = true;
            Assert.That(area.Enter(player, out failure), Is.True, failure);
            Assert.That(area.IsInside, Is.True);
        }

        [Test]
        public void Stage4RCarrierApproachAnchorStaysInsideInteractionRadius()
        {
            root = new GameObject("Stage4RCarrierApproachRoot");
            TestCarrier carrier = CreateCarrier("Stage4RApproachCarrier");
            Vector3 door = carrier.DoorOutside;
            WorldInteractionArea area = WorldInteractionArea.Add(
                root.transform,
                "stage4r_carrier_approach",
                "Board",
                "Board",
                WorldInteractionAction.BoardTrain,
                door,
                2.8f);
            area.CarrierSource = carrier;
            Vector3 anchor = WorldInteractionArea.ComputeCarrierApproach(
                carrier,
                area.Radius);
            area.SetApproachPoint(anchor);

            Assert.That(area.ApproachPoint, Is.Not.EqualTo(area.Center));
            Assert.That(Vector3.Distance(area.ApproachPoint, area.Center),
                Is.LessThan(area.Radius));
            Assert.That(area.Contains(area.ApproachPoint), Is.True);
        }

        [Test]
        public void Stage4RCutawayToggleIsIdempotentAndRestoresShellRoofStates()
        {
            root = new GameObject("Stage4RCutawayRoot");
            WorldCutawayVolume volume = root.AddComponent<WorldCutawayVolume>();
            volume.enterHysteresisSeconds = 0f;
            volume.exitHysteresisSeconds = 0f;

            GameObject shell = new GameObject("UpperShell");
            shell.transform.SetParent(root.transform, false);
            MeshRenderer shellRenderer = shell.AddComponent<MeshRenderer>();

            GameObject roof = new GameObject("RoofShell");
            roof.transform.SetParent(root.transform, false);
            MeshRenderer roofRenderer = roof.AddComponent<MeshRenderer>();
            roofRenderer.enabled = false;
            roof.SetActive(false);

            volume.upperShellRenderers.Add(shellRenderer);
            volume.upperShellRenderers.Add(roofRenderer);
            volume.upperShellObjects.Add(shell);
            volume.upperShellObjects.Add(roof);

            volume.SetPlayerInside(true);
            volume.SetPlayerInside(true);

            Assert.That(volume.IsRevealed, Is.True);
            Assert.That(shellRenderer.enabled, Is.False);
            Assert.That(roofRenderer.enabled, Is.False);
            Assert.That(shell.activeSelf, Is.False);
            Assert.That(roof.activeSelf, Is.False);

            volume.SetPlayerInside(false);
            volume.SetPlayerInside(false);

            Assert.That(volume.IsRevealed, Is.False);
            Assert.That(shellRenderer.enabled, Is.True);
            Assert.That(roofRenderer.enabled, Is.False);
            Assert.That(shell.activeSelf, Is.True);
            Assert.That(roof.activeSelf, Is.False);
        }

        PlayerController CreatePlayer(string name)
        {
            GameObject playerObject = new GameObject(name);
            playerObject.transform.SetParent(root.transform, false);
            PlayerController player = playerObject.AddComponent<PlayerController>();
            player.Controller = playerObject.AddComponent<CharacterController>();
            return player;
        }

        TestCarrier CreateCarrier(string name)
        {
            GameObject carrierObject = new GameObject(name);
            carrierObject.transform.SetParent(root.transform, false);
            return carrierObject.AddComponent<TestCarrier>();
        }

        sealed class TestCarrier : MonoBehaviour, ICityPassengerCarrier
        {
            public bool DoorsOpenValue;
            public Vector3 DoorInsideValue = new Vector3(0f, 1.1f, 0f);
            public Vector3 DoorOutsideLocal = new Vector3(0f, .05f, 2f);
            public int Boardings;
            public int Alightings;

            public Transform Hull => transform;
            public bool DoorsOpen => DoorsOpenValue;
            public Vector3 DoorInsideLocal => DoorInsideValue;
            public Vector3 DoorOutside => transform.TransformPoint(DoorOutsideLocal);

            public void RecordBoarding()
            {
                Boardings++;
            }

            public void RecordAlighting()
            {
                Alightings++;
            }
        }
    }
}
