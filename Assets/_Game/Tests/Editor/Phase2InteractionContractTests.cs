using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Tycoon.Tests
{
    public sealed class Phase2InteractionContractTests
    {
        sealed class FlickeringArea : IPlayerInteractionArea, ILogicalDwellInteractionArea
        {
            public InteractionKind Kind => InteractionKind.Operate;
            public Vector3 Center => Vector3.zero;
            public bool Available => true;
            public bool Inside = true;
            public int WorkCount;
            public int ExitCount;
            public bool Contains(Vector3 point) => Inside;
            public InteractionResult Perform(InteractionContext context, float delta)
            {
                WorkCount++;
                return InteractionResult.Success;
            }
            public void Exit(EntityId actor) => ExitCount++;
        }

        [Test]
        public void DwellSurvivesShortColliderGapWithoutExpandingLogicalArea()
        {
            var playerObject = new GameObject("Phase2InteractionPlayer");
            var player = playerObject.AddComponent<PlayerController>();
            var area = new FlickeringArea();
            var session = new PlayerInteractionSession();
            var context = new InteractionContext(player, "carrot");
            var areas = new List<IPlayerInteractionArea> { area };

            Assert.That(session.Tick(areas, context, Vector3.zero, .20f).Worked, Is.False);
            area.Inside = false;
            Assert.That(session.Tick(areas, context, Vector3.zero, .04f).Worked, Is.False);
            area.Inside = true;
            Assert.That(session.Tick(areas, context, Vector3.zero, .05f).Worked, Is.True);
            Assert.That(area.WorkCount, Is.EqualTo(1));
            Assert.That(area.ExitCount, Is.Zero);

            Object.DestroyImmediate(playerObject);
        }

        [Test]
        public void DwellStopsAfterAreaLossGraceAndReleasesExactlyOnce()
        {
            var playerObject = new GameObject("Phase2InteractionPlayer");
            var player = playerObject.AddComponent<PlayerController>();
            var area = new FlickeringArea();
            var session = new PlayerInteractionSession();
            var context = new InteractionContext(player, "carrot");
            var areas = new List<IPlayerInteractionArea> { area };

            session.Tick(areas, context, Vector3.zero, .10f);
            area.Inside = false;
            session.Tick(areas, context, Vector3.zero, .09f);
            Assert.That(session.Current, Is.Null);
            Assert.That(area.ExitCount, Is.EqualTo(1));

            session.Tick(areas, context, Vector3.zero, .10f);
            session.Stop();
            Assert.That(area.ExitCount, Is.EqualTo(1));

            Object.DestroyImmediate(playerObject);
        }

        [Test]
        public void LogicalDwellContractRemainsQuarterSecond()
        {
            Assert.That(PlayerInteractionSession.LogicalDwellSeconds, Is.EqualTo(.25f));
        }

        [Test]
        public void WorldInteractionAreasUseTheLogicalDwellContract()
        {
            Assert.That(typeof(WorldInteractionArea).GetInterfaces(),
                Does.Contain(typeof(ILogicalDwellInteractionArea)));
        }
    }
}
