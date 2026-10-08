using UnityEngine;

namespace Tycoon
{
    public static class StoredItemLayout
    {
        public static Quaternion Rotation(string id) => id switch
        {
            "carrot" or "wheat" or "corn" or "flour" or "animal_feed" or "yarn" =>
                Quaternion.Euler(90, 0, 0),
            _ => Quaternion.identity
        };
    }
}
