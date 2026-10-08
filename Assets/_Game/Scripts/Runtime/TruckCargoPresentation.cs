using System.Linq;
using UnityEngine;

namespace Tycoon
{
    public sealed partial class TruckLogistics
    {
        Transform pickupMarker;
        Transform dropMarker;
        TextMesh cargoCount;

        void UpdateCargoPresentation()
        {
            if (!pickupMarker)
            {
                pickupMarker = CargoMarker("LẤY KIỆN", "#C9AD6C");
                dropMarker = CargoMarker("GIAO KIỆN", "#73B499");
                cargoCount = Art.Label("", Vector3.zero, game.Player.transform, .13f).GetComponent<TextMesh>();
            }
            pickupMarker.position = PlayerCargoPoint(true);
            dropMarker.position = PlayerCargoPoint(false);
            pickupMarker.gameObject.SetActive(PlayerCargoAvailable);
            dropMarker.gameObject.SetActive(PlayerCargoAvailable);
            var held = game.Transactions.View.crates.Where(x => x.holder == "player").ToArray();
            int items = held.Sum(x => game.Transactions.View.stacks.Where(s => s.owner == x.id).Sum(s => s.quantity));
            cargoCount.gameObject.SetActive(items > 0);
            cargoCount.text = items + " / " + game.Player.Carry.Capacity;
            cargoCount.transform.localPosition = new Vector3(0, 1.55f + (held.Length - 1) / 6 * .52f, .65f);
        }

        Transform CargoMarker(string label, string color)
        {
            var marker = new GameObject(label).transform;
            marker.SetParent(transform);
            Art.Cylinder("Zone", new Vector3(0, .025f, 0), new Vector3(1.4f, .015f, 1.4f), color, marker);
            Art.Label(label, new Vector3(0, .3f, 0), marker, .11f);
            return marker;
        }
    }
}
