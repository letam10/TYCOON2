using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace Tycoon.Tests
{
    public sealed class WorldInteractionAreaTests
    {
        GameObject root;
        GameObject carrierObject;
        GameObject playerObject;

        [TearDown]
        public void TearDown()
        {
            if (playerObject) Object.DestroyImmediate(playerObject);
            if (carrierObject) Object.DestroyImmediate(carrierObject);
            if (root) Object.DestroyImmediate(root);
        }

        [Test]
        public void DynamicTaxiAreaPreservesTaxiAction()
        {
            root = new GameObject("TaxiRegistry");
            var registry = root.AddComponent<WorldInteractionRegistry>();
            carrierObject = new GameObject("TaxiCarrier");
            var carrier = carrierObject.AddComponent<TestCarrier>();

            MethodInfo addCarrier = typeof(WorldInteractionRegistry).GetMethod(
                "AddCarrierArea", BindingFlags.Instance | BindingFlags.NonPublic);
            addCarrier.Invoke(registry, new object[]
            {
                carrier,
                WorldInteractionAction.CallTaxi,
                "Đón xe",
                "Đang chờ cửa xe mở.",
                "vehicle"
            });

            var area = carrierObject.GetComponent<WorldInteractionArea>();
            Assert.That(area, Is.Not.Null);
            Assert.That(area.Action, Is.EqualTo(WorldInteractionAction.CallTaxi));
            Assert.That(area.Label, Is.EqualTo("Đón xe"));
            Assert.That(area.CarrierSource, Is.EqualTo(carrier));
        }

        [Test]
        public void TaxiAreaBoardsPassengerCarrier()
        {
            root = new GameObject("TaxiAreaRoot");
            carrierObject = new GameObject("TaxiCarrier");
            var carrier = carrierObject.AddComponent<TestCarrier>();
            var area = WorldInteractionArea.Add(
                root.transform,
                "taxi",
                "Đón xe",
                "Đang chờ cửa xe mở.",
                WorldInteractionAction.CallTaxi,
                Vector3.zero,
                2.8f);
            area.CarrierSource = carrier;

            playerObject = new GameObject("TaxiPlayer");
            var player = playerObject.AddComponent<PlayerController>();
            player.Controller = playerObject.AddComponent<CharacterController>();
            var result = area.Perform(new InteractionContext(player, "carrot"), .3f);

            Assert.That(result.Worked, Is.True, result.Reason);
            Assert.That(carrier.Boardings, Is.EqualTo(1));
            Assert.That(player.transform.parent, Is.EqualTo(carrier.transform));
            Assert.That(player.transform.localPosition, Is.EqualTo(carrier.DoorInsideLocal));
        }

        [Test]
        public void LandmarkAndDistrictAreasExposeSpecificActions()
        {
            root = new GameObject("InteractionRegistry");
            var bankObject = new GameObject("Bank");
            bankObject.transform.SetParent(root.transform, false);
            var bank = bankObject.AddComponent<CityLandmark>();
            bank.Id = "Bank";
            bank.Footprint = new Vector3(38, 12, 38);

            WorldInteractionRegistry.Build(root.transform);

            WorldInteractionArea bankArea = bankObject.GetComponent<WorldInteractionArea>();
            WorldInteractionArea restaurant = root.transform.Find("WorldInteraction_restaurant")
                .GetComponent<WorldInteractionArea>();
            WorldInteractionArea forest = root.transform.Find("WorldInteraction_forest")
                .GetComponent<WorldInteractionArea>();

            Assert.That(bankArea.Action, Is.EqualTo(WorldInteractionAction.Banking));
            Assert.That(restaurant.Action, Is.EqualTo(WorldInteractionAction.Dine));
            Assert.That(forest.Action, Is.EqualTo(WorldInteractionAction.Explore));
        }

        sealed class TestCarrier : MonoBehaviour, ICityPassengerCarrier
        {
            public int Boardings;
            public int Alightings;
            public Transform Hull => transform;
            public bool DoorsOpen => true;
            public Vector3 DoorInsideLocal => new(0, 1.15f, .2f);
            public Vector3 DoorOutside => transform.position + Vector3.forward * 2f;
            public void RecordBoarding() => Boardings++;
            public void RecordAlighting() => Alightings++;
        }
    }
}
