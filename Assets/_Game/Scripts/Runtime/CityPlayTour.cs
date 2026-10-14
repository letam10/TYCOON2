using System.Collections;
using System.Linq;
using UnityEngine;

namespace Tycoon
{
    public sealed partial class CityPlaySession
    {
        IEnumerator Tour()
        {
            var destinations = new[] { "processing", "supermarket", "bakery", "restaurant", "farm_shop", "farm" };
            int lap = 0;
            do
            {
                foreach (string area in destinations)
                {
                    var storage = game.StorageFor(area);
                    yield return Walk(storage.InteractionPoint + Vector3.back * 4);
                    if (lap == 0)
                    {
                        yield return Capture("district-" + area);
                        completed.Add("Đi tới khu " + area);
                    }
                    yield return Hold(1);
                }
                var plot = game.Producers.First(x => x.Id == "field_carrot");
                yield return Walk(plot.InteractionPoint);
                yield return Wait(() => game.Player.Carry.Count("carrot") >= 48, 90,
                    "Thu hoạch chồng hàng lớn trong lượt chơi");
                yield return Walk(plot.InteractionPoint + Vector3.back * 2);
                yield return Capture("large-stack-" + lap);
                yield return StoreGoods(game.Storage);
                yield return Walk(CityDistricts.ParkCenter + Vector3.back * 8);
                if (lap == 0) yield return Capture("central-park");
                lap++;
            } while (Time.realtimeSinceStartupAsDouble - started < 240);
            yield return Hold(1);
            foreach (var landmark in game.GetComponentsInChildren<CityLandmark>())
            {
                yield return Walk(landmark.Approach);
                yield return Capture("visited-" + landmark.Id);
                completed.Add("Đi bộ tới khu mới " + landmark.Id);
                if (landmark.Id is not ("Harbor" or "Marina")) continue;
                float pier = landmark.Id == "Harbor" ? 18 : 12;
                yield return Walk(landmark.transform.position + new Vector3(14, 0, -pier));
                yield return Walk(landmark.transform.position + new Vector3(14, 0, pier));
                completed.Add("Đi bộ thật ra cả hai cầu gỗ bến tàu");
            }
        }
    }
}
