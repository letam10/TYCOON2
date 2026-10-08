using System.Collections;
using System.Linq;
using UnityEngine;

namespace Tycoon
{
    public sealed partial class QaDriver
    {
        IEnumerator PhysicalActorCollisionAcceptance()
        {
            Vector3 original = game.Player.transform.position;
            bool control = game.Player.CanControl;
            Time.timeScale = 0;
            game.Player.CanControl = false;
            var point = game.Storage.transform.position;
            var obstacle = game.Storage.GetComponentsInChildren<Collider>()
                .First(x => x.enabled && x.bounds.size.y > .5f);
            var edge = obstacle.bounds;
            var start = new Vector3(edge.min.x - 1, point.y + .05f, edge.center.z);
            game.Player.Controller.enabled = false;
            game.Player.transform.position = start;
            game.Player.Controller.enabled = true;
            Physics.SyncTransforms();
            for (int i = 0; i < 45; i++) game.Player.Controller.Move(Vector3.right * .08f);
            Check(game.Player.transform.position.x < edge.min.x,
                "physical: player cannot walk through the real warehouse collider");
            var root = new GameObject("NpcCollisionAcceptance");
            root.transform.position = start + Vector3.back * .4f;
            var agent = Navigation.Agent(root);
            agent.enabled = false;
            var motion = root.GetComponent<ActorPhysicalMotion>();
            Physics.SyncTransforms();
            for (int i = 0; i < 45; i++) motion.ApplyStep(Vector3.right * .08f, .02f);
            Check(root.transform.position.x < edge.min.x,
                "physical: NPC capsule cannot pass through the real warehouse collider");
            Check(motion.Speed < .1f, "physical: blocked NPC animation speed is zero");
            Object.Destroy(root);
            game.Player.Controller.enabled = false;
            game.Player.transform.position = original;
            game.Player.Controller.enabled = true;
            game.Player.CanControl = control;
            Time.timeScale = 1;
            yield return null;
        }
    }
}
