using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Tycoon.Tests
{
    public sealed partial class TransactionCoreTests
    {
        [Test] public void ModCashChangesOnlyWalletAndJournal()
        {
            var seed=Seed();seed.unlocked.Clear();seed.revenue=123;seed.cashCollected=60;seed.assistedCash=77;
            var core=Core(seed);var before=core.Snapshot();now=120;
            Assert.That(core.Execute(Command(core,TransactionKind.GrantModCash,"mod")).amount,Is.EqualTo(999999));
            var after=core.Snapshot();Assert.That(after.money,Is.EqualTo(before.money+999999));
            // Chỉ bỏ các trường sổ giao dịch khi so toàn bộ dữ liệu gameplay.
            after.money=before.money;after.revision=before.revision;after.receipts=before.receipts;after.outbox=before.outbox;
            Assert.That(JsonUtility.ToJson(after),Is.EqualTo(JsonUtility.ToJson(before)));
        }
        [Test] public void ModCashDoesNotExpireExistingReservations()
        {
            var core=Core();var reserve=Command(core,TransactionKind.Reserve,"reserve","r");reserve.expiresAt=110;core.Execute(reserve);
            now=120;core.Execute(Command(core,TransactionKind.GrantModCash,"mod"));
            Assert.That(core.Snapshot().reservations.Single().status,Is.EqualTo(ReservationStatus.Active));
        }
        [Test] public void ModCashReplayAndReloadGrantExactlyOnce()
        {
            var store=new FileTransactionStore(path);var core=Core(store:store);var mod=Command(core,TransactionKind.GrantModCash,"mod");
            core.Execute(mod);core.Execute(mod);core=Core(store:store);core.Execute(mod);
            Assert.That(core.Snapshot().money,Is.EqualTo(600+999999));Journal(core,1);
            core.Execute(Command(core,TransactionKind.GrantModCash,"second-click"));
            Assert.That(Core(store:store).Snapshot().money,Is.EqualTo(600+2*999999));
        }
        [Test] public void ModCashRejectsOverflowWithoutChangingState()
        {
            var core=Core(new TransactionState{money=int.MaxValue-999999+1,owners=Seed().owners});
            Rejected(core,Command(core,TransactionKind.GrantModCash,"limit"),"money-limit");
        }
        [Test] public void ModCashRequiresPlayerAuthority()
        {
            var core=Core();var mod=Command(core,TransactionKind.GrantModCash,"worker");mod.actor="worker";
            string before=Json(core);Assert.Throws<TransactionRejectedException>(()=>core.Execute(mod));Assert.That(Json(core),Is.EqualTo(before));
        }
    }
}
