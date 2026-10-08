using NUnit.Framework;

namespace Tycoon.Tests
{
    public sealed partial class RuntimeAuthorityTests
    {
        [Test]
        public void SingleCrewLookupReturnsIndependentData()
        {
            var state = game.Transactions.Snapshot();
            var crew = GameSession.CrewFor(Definitions.Upgrade("farmer"));
            crew.count = 2;
            state.crews.RemoveAll(x => x.id == crew.id);
            state.crews.Add(crew);
            if (!state.unlocked.Contains(crew.id)) state.unlocked.Add(crew.id);
            game.Transactions.Detach();
            game.InitializeTransactions(state, false);
            var found = game.FindCrew("farmer");
            Assert.That(found.count, Is.EqualTo(2));
            Assert.That(found.id, Is.EqualTo(crew.id));
            Assert.That(found.role, Is.EqualTo(crew.role));
            Assert.That(found.area, Is.EqualTo(crew.area));
            Assert.That(found.speedLevel, Is.EqualTo(crew.speedLevel));
            found.count = 99;
            found.carryLevel = 99;
            Assert.That(game.FindCrew("farmer").count, Is.EqualTo(2));
            Assert.That(game.FindCrew("farmer").carryLevel, Is.EqualTo(1));
        }

        [Test]
        public void SingleCrewLookupReturnsNullForAbsentCrew()
        {
            Assert.That(game.FindCrew("missing-crew"), Is.Null);
        }
    }
}
