using UnityEngine;
using UnityEngine.AI;

namespace Tycoon
{
    public sealed partial class GameSession
    {
        const int WorkerBuildsPerFrame = 2;
        int workerBuildFrame = -1;
        int workerBuildCount;
        public int ActiveWorkerCount => CountWorkforce(false);
        public int RetiringWorkerCount => CountWorkforce(true);
        public int PendingWorkerRetirements => Transactions == null ? RetiringWorkerCount :
            WorkforceRules.PendingRetirements(Transactions.View);

        int CountWorkforce(bool retiring)
        {
            int count = 0;
            foreach (var worker in Workers)
                if (worker && worker.Retiring == retiring) count++;
            return count;
        }

        bool WorkersReady
        {
            get
            {
                var crews = Transactions == null ? crewStates : Transactions.View.crews;
                int expected = 0;
                foreach (var crew in crews) expected += WorkforceRules.ActiveCount(crew);
                return ActiveWorkerCount == expected;
            }
        }

        public void SyncWorkers()
        {
            using var frameProbe = QaFrameProbe.Measure("GameSession.SyncWorkers");
            foreach (var worker in Workers)
                if (WorkforceRules.IsExcess(worker.WorkerId, worker.UpgradeId)) worker.RequestRetirement();
            PendingWorkers.RemoveAll(x => x.retired);
            if (WorkersReady && PendingWorkers.Count == 0) return;
            using var operation = CityOperationTiming.Measure(this, "Workers.BuildPending");
            if (workerBuildFrame != Time.frameCount)
            {
                workerBuildFrame = Time.frameCount;
                workerBuildCount = 0;
            }
            var crews = Transactions == null ? crewStates : Transactions.View.crews;
            foreach (var crew in crews)
            {
                for (int slot = 0; slot < WorkforceRules.ActiveCount(crew); slot++)
                {
                    string key = crew.id + ":" + slot;
                    if (Workers.Exists(x => x.WorkerId == key)) continue;
                    if (!BuildWorker(crew, key, slot, PendingWorkers.Find(x => x.id == key))) return;
                }
            }
            // Nhan vien du chi duoc dung lai de hoan tat hang va cho giu tu save cu.
            for (int index = PendingWorkers.Count - 1; index >= 0; index--)
            {
                var saved = PendingWorkers[index];
                if (!saved.retiring && !WorkforceRules.IsExcess(saved.id, saved.upgrade)) continue;
                var crew = crews.Find(x => x.id == saved.upgrade);
                if (crew == null || Workers.Exists(x => x.WorkerId == saved.id)) continue;
                if (!BuildWorker(crew, saved.id, 0, saved)) return;
            }
        }

        bool BuildWorker(CrewState crew, string key, int slot, WorkerSave saved)
        {
            if (Application.isPlaying && workerBuildCount >= WorkerBuildsPerFrame) return false;
            var storage = StorageFor(crew.area);
            if (!storage) return true;
            var root = new GameObject("Worker_" + key);
            root.transform.SetParent(transform);
            Vector3 position = saved != null
                ? new Vector3(saved.x, .05f, saved.z)
                : storage.InteractionPoint + Vector3.left * (2 + slot);
            if (NavMesh.SamplePosition(position, out var hit, 3, NavMesh.AllAreas)) position = hit.position;
            root.transform.position = position;
            var model = Art.Model("player", Vector3.zero, root.transform);
            model.AddComponent<ActorView>();
            var worker = root.AddComponent<WorkerAgent>();
            worker.WorkerId = key;
            worker.Initialize(Definitions.Upgrade(crew.id));
            Workers.Add(worker);
            Transactions?.BindWorker(worker, saved);
            if (saved != null)
            {
                worker.Restore(saved);
                PendingWorkers.Remove(saved);
            }
            if (WorkforceRules.IsExcess(key, crew.id)) worker.RequestRetirement();
            UpgradeModelView.AttachWorker(this, worker, false);
            workerBuildCount++;
            return true;
        }
    }
}
