using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using Object = UnityEngine.Object;

namespace Tycoon.Tests
{
    public sealed class PlayerInteractionTests
    {
        GameSession game,previous;
        GameObject root;
        T Add<T>(string id) where T:Component
        {var child=new GameObject(id);child.SetActive(false);child.transform.SetParent(root.transform);return child.AddComponent<T>();}
        [SetUp]public void SetUp()
        {
            previous=GameSession.Instance;root=new GameObject("PlayerInteractionTest");root.SetActive(false);
            game=root.AddComponent<GameSession>();GameSession.Instance=game;game.Player=Add<PlayerController>("Player");
        }
        [TearDown]public void TearDown(){Object.DestroyImmediate(root);GameSession.Instance=previous;}
        [Test]public void MovementFollowsCameraYawAndClampsDiagonalAtEveryPitch()
        {
            var camera=Add<Camera>("Camera");camera.transform.rotation=Quaternion.Euler(55,40,0);
            Vector3 w=PlayerController.CameraRelativeDirection(Vector2.up,camera.transform);
            Vector3 d=PlayerController.CameraRelativeDirection(Vector2.right,camera.transform);
            Assert.That(Vector3.Dot(w,Vector3.ProjectOnPlane(camera.transform.forward,Vector3.up).normalized),Is.GreaterThan(.999f));
            Assert.That(Vector3.Dot(d,Vector3.ProjectOnPlane(camera.transform.right,Vector3.up).normalized),Is.GreaterThan(.999f));
            Assert.That(w.y,Is.Zero);Assert.That(PlayerController.CameraRelativeDirection(Vector2.one,camera.transform).magnitude,Is.EqualTo(1).Within(.0001));
            camera.transform.rotation=Quaternion.Euler(90,0,0);Assert.That(PlayerController.CameraRelativeDirection(Vector2.up,camera.transform).magnitude,Is.EqualTo(1).Within(.0001));
        }
        [Test]public void PerspectiveCameraHasFiftyFiveDegreePitchAndDampsTowardTarget()
        {
            var camera=Add<Camera>("Camera");var rig=camera.gameObject.AddComponent<FollowCamera>();rig.Target=game.Player.transform;rig.Snap();
            Assert.That(camera.orthographic,Is.False);Assert.That(Mathf.DeltaAngle(camera.transform.eulerAngles.x,55),Is.EqualTo(0).Within(.001));
            Vector3 before=camera.transform.position;game.Player.transform.position+=Vector3.right*5;rig.Follow(.02f);
            Assert.That((camera.transform.position-before).magnitude,Is.InRange(.001f,4.999f));
            for(int i=0;i<100;i++)rig.Follow(.02f);
            Vector3 destination=game.Player.transform.position+rig.FocusOffset-Quaternion.Euler(rig.Pitch,rig.Yaw,0)*Vector3.forward*rig.Distance;
            Assert.That(Vector3.Distance(camera.transform.position,destination),Is.LessThan(.01f));
        }
        [Test]public void CameraZoomClampsPlayerRangeAndPreservesOverviewAndExplicitArtDistance()
        {
            var camera=Add<Camera>("Camera");var rig=camera.gameObject.AddComponent<FollowCamera>();rig.Target=game.Player.transform;
            rig.Zoom(100);Assert.That(rig.Distance,Is.EqualTo(FollowCamera.MinimumDistance));
            rig.Zoom(-100);Assert.That(rig.Distance,Is.EqualTo(FollowCamera.MaximumDistance));
            rig.Zoom(float.NaN);Assert.That(rig.Distance,Is.EqualTo(FollowCamera.MaximumDistance));
            rig.Overview=true;rig.Zoom(1);Assert.That(rig.Distance,Is.EqualTo(FollowCamera.MaximumDistance));
            rig.Overview=false;rig.Distance=4;rig.Snap();
            Assert.That(Vector3.Distance(camera.transform.position,game.Player.transform.position+rig.FocusOffset),Is.EqualTo(4).Within(.001f));
        }
        [Test]public void CameraRejectsInvalidTimeAndSnapsAfterLongDistanceRestore()
        {
            var camera=Add<Camera>("Camera");var rig=camera.gameObject.AddComponent<FollowCamera>();rig.Target=game.Player.transform;rig.Snap();
            Vector3 initial=camera.transform.position;game.Player.transform.position=Vector3.right*35;
            rig.Follow(0);rig.Follow(float.NaN);rig.Follow(float.PositiveInfinity);Assert.That(camera.transform.position,Is.EqualTo(initial));
            rig.Follow(.02f);
            Vector3 destination=game.Player.transform.position+rig.FocusOffset-Quaternion.Euler(rig.Pitch,rig.Yaw,0)*Vector3.forward*rig.Distance;
            Assert.That(Vector3.Distance(camera.transform.position,destination),Is.LessThan(.001f));
        }
        [Test]public void AnalogMovementRetainsInputStrengthWithoutCamera()
        {
            Assert.That(PlayerController.CameraRelativeDirection(new Vector2(.25f,.5f),null),Is.EqualTo(new Vector3(.25f,0,.5f)));
            Assert.That(PlayerController.CameraRelativeDirection(Vector2.zero,null),Is.EqualTo(Vector3.zero));
        }
        sealed class Area:IPlayerInteractionArea
        {
            public InteractionKind Kind {get;set;}
            public Vector3 Center {get;set;}
            public bool Available=>true;
            public int worked,exits;
            public bool Contains(Vector3 p)=>Vector3.Distance(p,Center)<.5f;
            public InteractionResult Perform(InteractionContext c,float delta){worked++;return InteractionResult.Success;}
            public void Exit(EntityId actor){exits++;}
        }
        [Test]public void ZoneSessionStartsAutomaticallyAndLeavingOrInvalidDeltaReleasesExactlyOnce()
        {
            var a=new Area{Kind=InteractionKind.Operate};var b=new Area{Kind=InteractionKind.Pickup,Center=Vector3.right*2};
            var session=new PlayerInteractionSession();var context=new InteractionContext(game.Player,"carrot");var areas=new[]{a,b};
            Assert.That(session.Tick(areas,context,Vector3.zero,.1f).Worked,Is.True);
            session.Tick(areas,context,Vector3.right*2,.1f);Assert.That(a.exits,Is.EqualTo(1));Assert.That(b.worked,Is.EqualTo(1));
            session.Tick(areas,context,Vector3.one*10,.1f);session.Stop();Assert.That(b.exits,Is.EqualTo(1));
            session.Tick(areas,context,Vector3.zero,.1f);session.Tick(areas,context,Vector3.zero,float.NaN);
            Assert.That(a.exits,Is.EqualTo(2));Assert.That(session.Current,Is.Null);
        }
        [Test]public void PickupAndDropNeverSwitchSkuOrDeleteGoodsWhenRejected()
        {
            var storage=Add<StorageStation>("Storage");storage.Inventory=new Inventory(20);storage.Inventory.TryAdd("carrot",5);storage.Inventory.TryAdd("milk",5);
            game.Player.Carry.TryAdd("carrot",11);
            var wrong=storage.Perform(InteractionKind.Pickup,new InteractionContext(game.Player,"milk"),.1f);
            Assert.That(wrong.Worked,Is.False);Assert.That(wrong.Reason,Does.Contain("đổi loại"));Assert.That(game.Player.Carry.Count("carrot"),Is.EqualTo(11));
            Assert.That(storage.Perform(InteractionKind.Pickup,new InteractionContext(game.Player,"carrot"),.1f).Worked,Is.True);
            var full=storage.Perform(InteractionKind.Pickup,new InteractionContext(game.Player,"carrot"),.1f);
            Assert.That(full.Worked,Is.False);Assert.That(full.Reason,Does.Contain("đầy"));Assert.That(game.Player.Carry.Total,Is.EqualTo(12));
            var shelf=Add<ShelfStation>("Shelf");shelf.Inventory=new Inventory(20);shelf.AllowedItems=new[]{"milk"};
            Assert.That(shelf.Perform(InteractionKind.Drop,new InteractionContext(game.Player,"milk"),.1f).Reason,Does.Contain("không nhận"));
            Assert.That(game.Player.Carry.Count("carrot"),Is.EqualTo(12));Assert.That(shelf.Inventory.Total,Is.Zero);
        }
        [Test]public void InputSystemKeepsKeyboardAndGamepadBindingsAndDisposesActions()
        {
            game.Player.Initialize();
            Assert.That(game.Player.Move.bindings,Has.Some.Matches<InputBinding>(x=>x.path=="<Keyboard>/w"));
            Assert.That(game.Player.Move.bindings,Has.Some.Matches<InputBinding>(x=>x.path=="<Keyboard>/upArrow"));
            Assert.That(game.Player.Move.bindings,Has.Some.Matches<InputBinding>(x=>x.path=="<Gamepad>/leftStick"));
            Assert.That(game.Player.Cycle.bindings,Has.Some.Matches<InputBinding>(x=>x.path=="<Gamepad>/rightShoulder"));
            game.Player.CanControl=false;Assert.That(game.Player.ActiveInteraction,Is.Null);
            Assert.That(game.Player.CurrentSpeed,Is.Zero);Assert.That(game.Player.IsSprinting,Is.False);
        }
        [Test]public void FarmMachineCounterAndTableExposeOnlyTheirSeparateActions()
        {
            var crop=Add<ProductionStation>("Crop");crop.ItemId="carrot";
            var machine=Add<MachineStation>("Machine");var counter=Add<CheckoutStation>("Counter");counter.ShopId="farm";counter.Inventory=new Inventory(4);
            var shelf=Add<ShelfStation>("CarrotShelf");shelf.ShopId="farm";shelf.AllowedItems=new[]{"carrot"};game.Shelves.Add(shelf);
            var table=Add<TableStation>("Table");
            Assert.That(StationPlayerActions.Supports(crop,InteractionKind.Drop),Is.False);
            Assert.That(StationPlayerActions.Supports(machine,InteractionKind.Serve),Is.False);
            Assert.That(StationPlayerActions.Supports(counter,InteractionKind.Pickup),Is.False);
            Assert.That(StationPlayerActions.Supports(counter,InteractionKind.Drop),Is.True);
            Assert.That(StationPlayerActions.Supports(counter,InteractionKind.Serve),Is.True);
            Assert.That(StationPlayerActions.Supports(counter,InteractionKind.Collect),Is.True);
            game.Player.Carry.TryAdd("carrot",1);
            Assert.That(counter.Perform(InteractionKind.Drop,new InteractionContext(game.Player,"carrot"),.1f).Worked,Is.True);
            Assert.That(counter.Inventory.Count("carrot"),Is.EqualTo(1));
            game.Player.Carry.TryAdd("milk",1);
            var rejected=counter.Perform(InteractionKind.Drop,new InteractionContext(game.Player,"milk"),.1f);
            Assert.That(rejected.Worked,Is.False);Assert.That(game.Player.Carry.Count("milk"),Is.EqualTo(1));
            Assert.That(StationPlayerActions.Supports(table,InteractionKind.Serve),Is.True);
            Assert.That(StationPlayerActions.Supports(table,InteractionKind.Operate),Is.True);
        }
    }
}
