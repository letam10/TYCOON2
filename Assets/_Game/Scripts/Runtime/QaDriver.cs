using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace Tycoon
{
    public sealed partial class QaDriver : MonoBehaviour
    {
        GameSession game;
        Keyboard keyboard;
        Gamepad gamepad;
        Mouse mouse;
        QaReport report = new();
        string failure;
        readonly List<InputDevice> physicalDevices = new();
        IEnumerator Start()
        {
            game = GameSession.Instance;
            // Chỉ cô lập input trong tiến trình QA để phím người dùng không làm sai phép thử.
            foreach(var device in InputSystem.devices)
                if(device.enabled && (device is Keyboard || device is Gamepad || device is Mouse)) { physicalDevices.Add(device); InputSystem.DisableDevice(device); }
            keyboard = InputSystem.AddDevice<Keyboard>("QA_Keyboard");
            gamepad = InputSystem.AddDevice<Gamepad>("QA_Gamepad");
            mouse = InputSystem.AddDevice<Mouse>("QA_Mouse");
            report.startedUtc = DateTime.UtcNow.ToString("o");
            report.graphicsDevice = SystemInfo.graphicsDeviceName;
            var routines = new Stack<IEnumerator>();
            var arguments = Environment.GetCommandLineArgs();
            bool load = Array.Exists(arguments,x=>x=="--qa-load"), full = Array.Exists(arguments,x=>x=="--qa-progression");
            bool stage45=Array.Exists(arguments,x=>x=="--qa-stage45");bool stage67=Array.Exists(arguments,x=>x=="--qa-stage67");
            bool art = Array.Exists(arguments,x=>x=="--qa-art");
            bool redesign=Array.Exists(arguments,x=>x=="--qa-v2");
            bool stage14=Array.Exists(arguments,x=>x=="--qa-stage14"),layout14=Array.Exists(arguments,x=>x=="--qa-stage14-layout"),visual14=Array.Exists(arguments,x=>x=="--qa-stage14-visual");
            bool milestones14=Array.Exists(arguments,x=>x=="--qa-stage14-milestones");
            bool assets=Array.Exists(arguments,x=>x=="--qa-assets");
            bool town=Array.Exists(arguments,x=>x=="--qa-town"),townLayout=Array.Exists(arguments,x=>x=="--qa-town-layout"),townLoad=Array.Exists(arguments,x=>x=="--qa-town-load");
            string mode = redesign ? "redesign" : art ? "visual" : load ? "load" : stage67?"stage67":stage45?"stage45":full ? "progression" : game.Milestone == 0 ? "foundation" : "vertical";
            bool resume = Array.Exists(arguments,x=>x=="--qa-resume");
            if(stage14||layout14||visual14)mode=stage14?"stage14-progression":layout14?"stage14-layout":"stage14-visual";
            if(milestones14)mode="stage14-milestones";
            if(assets)mode="asset-library";
            if(town||townLayout||townLoad)mode=townLoad?"town-load":townLayout?"town-layout":"town-redesign";
            if (Array.Exists(arguments, argument => argument == "--qa-completion"))
            {
                mode = "completion";
                routines.Push(CompletionAudit());
            }
            else if (Array.Exists(arguments, argument => argument == "--qa-completion-load"))
            {
                mode = "completion-load";
                routines.Push(CompletionRelaunch());
            }
            else if (Array.Exists(arguments, argument => argument == "--qa-save-recovery"))
            {
                mode = "save-recovery";
                routines.Push(SaveRecovery());
            }
            else if (Array.Exists(arguments, argument => argument == "--qa-physical-migration"))
            {
                mode = "physical-migration";
                routines.Push(PhysicalMigrationAcceptance());
            }
            else if (Array.Exists(arguments, argument => argument is
                "--qa-physical-load" or "--qa-physical-visuals" or "--qa-physical-performance"))
            {
                bool visuals = Array.Exists(arguments, argument => argument == "--qa-physical-visuals");
                bool performance = Array.Exists(arguments, argument => argument == "--qa-physical-performance");
                mode = performance ? "physical-performance" : visuals ? "physical-visuals" : "physical-load";
                routines.Push(PhysicalSavedAcceptance(visuals, performance));
            }
            else if (Array.Exists(arguments, argument => argument == "--qa-physical-carry"))
            {
                mode = "physical-carry";
                routines.Push(PhysicalCashAcceptance());
            }
            else
                routines.Push(
                    townLoad ? TownRelaunch()
                    : town || townLayout ? TownRedesign(townLayout)
                    : assets ? AssetLibraryVisual()
                    : milestones14 ? FinalMilestones()
                    : stage14 ? FinalProgression()
                    : layout14 ? FinalLayout()
                    : visual14 ? FinalVisual()
                    : redesign ? Redesign()
                    : art ? VisualInspection()
                    : load ? LoadCheck()
                    : stage67 ? LivestockAndCrew()
                    : stage45 ? FarmStarterAndPurchase()
                    : game.Milestone == 0 ? Foundation()
                    : full || resume ? Progression(resume)
                    : Vertical());
            while (routines.Count > 0)
            {
                object yielded = null; bool running = false;
                try { running = routines.Peek().MoveNext(); if (running) yielded = routines.Peek().Current; }
                catch (Exception error) { failure = error.ToString(); }
                if (failure != null) break;
                if (!running) { routines.Pop(); continue; }
                if (yielded is IEnumerator child) { routines.Push(child); continue; }
                yield return yielded;
            }
            Keys();
            if (failure != null) { WriteState("failure-state.txt"); yield return Capture("failure.png"); }
            report.failure = failure ?? "";
            report.runtimeErrors = game.RuntimeErrors.ToArray();
            report.passed = failure == null && game.RuntimeErrors.Count == 0;
            report.distanceWalked = game.Player.DistanceWalked;
            report.interactions = game.Player.SuccessfulInteractions;
            report.finishedUtc = DateTime.UtcNow.ToString("o");
            File.WriteAllText(Path.Combine(game.QaDirectory, mode + "-report.json"), JsonUtility.ToJson(report, true));
            InputSystem.RemoveDevice(keyboard); InputSystem.RemoveDevice(gamepad); InputSystem.RemoveDevice(mouse);
            foreach(var device in physicalDevices) if(device.added) InputSystem.EnableDevice(device);
            Debug.Log(mode.ToUpperInvariant()+"_QA " + (report.passed ? "PASS" : "FAIL " + report.failure));
#if UNITY_EDITOR
            UnityEditor.EditorApplication.Exit(report.passed ? 0 : 2);
#else
            Application.Quit(report.passed ? 0 : 2);
#endif
        }
        IEnumerator Travel(Vector3 target)
        {
            var path = new UnityEngine.AI.NavMeshPath();
            if (game.NavigationReady && UnityEngine.AI.NavMesh.CalculatePath(game.Player.transform.position, target, UnityEngine.AI.NavMesh.AllAreas, path) && path.status == UnityEngine.AI.NavMeshPathStatus.PathComplete)
            {
                var route=new List<string>{"from="+game.Player.transform.position+" to="+target+" status="+path.status};
                foreach(var corner in path.corners)route.Add("corner="+corner);
                File.WriteAllLines(Path.Combine(game.QaDirectory,"navigation-route.txt"),route);
                for (int i = 1; i < path.corners.Length; i++) yield return WalkTo(path.corners[i],i==path.corners.Length-1);
            }
            else yield return WalkTo(target);
            Keys();InputSystem.QueueStateEvent(gamepad,new GamepadState()); yield return new WaitForSeconds(.2f);
        }
        IEnumerator WalkTo(Vector3 target,bool settle=true)
        {
            float end = Time.realtimeSinceStartup + 25;
            Vector3 previous=game.Player.transform.position;float blocked=0;int repaths=0,corner=0;Vector3[] detour=Array.Empty<Vector3>();
            while (Vector2.Distance(new Vector2(target.x, target.z), new Vector2(game.Player.transform.position.x, game.Player.transform.position.z)) > (settle?.4f:.2f))
            {
                if (Time.realtimeSinceStartup > end) throw new Exception("Movement timeout: " + target + " from " + game.Player.transform.position);
                float moved=(game.Player.transform.position-previous).sqrMagnitude;previous=game.Player.transform.position;blocked=moved<.0001f?blocked+Time.deltaTime:0;
                if(blocked>2&&repaths<3)
                {
                    var path=new UnityEngine.AI.NavMeshPath();var start=previous;
                    if(UnityEngine.AI.NavMesh.SamplePosition(start,out var hit,1,UnityEngine.AI.NavMesh.AllAreas))start=hit.position;
                    if(UnityEngine.AI.NavMesh.CalculatePath(start,target,UnityEngine.AI.NavMesh.AllAreas,path)&&path.status==UnityEngine.AI.NavMeshPathStatus.PathComplete){detour=path.corners;corner=Vector3.Distance(previous,start)>.2f?0:1;}
                    repaths++;blocked=0;
                }
                while(corner<detour.Length&&Vector3.Distance(game.Player.transform.position,detour[corner])<.3f)corner++;
                Vector3 goal=corner<detour.Length?detour[corner]:target;
                Vector3 direction = goal - game.Player.transform.position; direction.y = 0; direction.Normalize();
                Vector3 forward = Camera.main.transform.forward; forward.y = 0; forward.Normalize();
                Vector3 right = Camera.main.transform.right; right.y = 0; right.Normalize();
                float x = Vector3.Dot(direction, right), y = Vector3.Dot(direction, forward);
                // Stick liên tục theo góc NavMesh; WASD tám hướng có thể cắt góc tường.
                float distance=Vector3.Distance(game.Player.transform.position,goal);
                float approach=Mathf.Clamp01(distance/Mathf.Max(.5f,6.2f*Time.deltaTime*1.4f));
                Keys();InputSystem.QueueStateEvent(gamepad,new GamepadState{leftStick=new Vector2(x,y)*approach});yield return null;
            }
            if(settle){Keys();InputSystem.QueueStateEvent(gamepad,new GamepadState()); yield return new WaitForSeconds(.2f);}
        }
        void Keys(params Key[] keys) => InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys));
        void WriteState(string name)
        {
            var lines=new List<string>{"UTC="+DateTime.UtcNow.ToString("o"),"timeScale="+Time.timeScale+" money="+game.Economy.Money+" pending="+game.Economy.PendingCash+" transactions="+game.Economy.Transactions,"player="+game.Player.transform.position+" nearest="+game.NearestStation(game.Player.transform.position)?.Id+" input="+game.Player.Use.IsPressed(),"unlocked="+string.Join(",",game.Economy.Unlocked)};
            foreach(var station in game.Stations)if(station.Inventory!=null)lines.Add(station.Id+" total="+station.Inventory.Total+" "+JsonUtility.ToJson(new InventorySave(station.Id,station.Inventory)));
            foreach(var machine in game.Machines)lines.Add(machine.Id+" input="+JsonUtility.ToJson(new InventorySave(machine.Id,machine.Input))+" running="+machine.Running+" remaining="+machine.Remaining+" batches="+machine.Batches);
            foreach(var worker in game.Workers)lines.Add(worker.Diagnostic());
            foreach(var lane in game.Checkouts)lines.Add(lane.Id+" cash="+lane.Cash+" queue="+lane.Queue.Count);
            foreach(var customer in game.Commerce.Customers)lines.Add(customer.Diagnostic());
            File.WriteAllLines(Path.Combine(game.QaDirectory,name),lines);
        }
        void Check(bool condition, string label)
        {
            if (!condition) throw new Exception("QA failed: " + label);
            report.checks.Add(label);
        }
        static Bounds Bounds(GameObject root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>();
            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
            return bounds;
        }
        [Serializable]
        public sealed class QaReport
        {
            public bool passed;
            public string startedUtc, finishedUtc, graphicsDevice, failure;
            public float distanceWalked, playerHeight;
            public int interactions;
            public int materialSlotsChecked;
            public List<string> checks = new();
            public List<string> screenshots = new();
            public string[] runtimeErrors;
        }
    }
}
