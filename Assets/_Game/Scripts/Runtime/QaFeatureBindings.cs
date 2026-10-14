using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Tycoon
{
    public sealed partial class QaDriver
    {
        IEnumerator FeatureBindingsAcceptance()
        {
            game.Hud.OpenMap(); FeatureClick("MapTravel_tycoon"); yield return null;
            game.Hud.OpenSettings(); FeatureClick("SettingsBindingsButton"); yield return null;
            FeatureClick("Bind_map"); yield return FeatureKey(Key.W);
            Check(GameKeyBindings.Path("map") == "<Keyboard>/m",
                "Duplicate movement key is rejected without changing map key");
            yield return FeatureKey(Key.Escape);
            Check(SettingsObject<RectTransform>("BindingsPanel").gameObject.activeInHierarchy,
                "Escape cancels capture without closing bindings screen");
            FeatureClick("Bind_map"); yield return FeatureKey(Key.P);
            Check(GameKeyBindings.Path("map") == "<Keyboard>/p", "UI rebind records map P");
            FeatureClick("ResetBindings"); yield return null;
            Check(GameKeyBindings.Path("map") == "<Keyboard>/m", "UI reset removes persisted map override");
            FeatureClick("Bind_map"); yield return FeatureKey(Key.P);
            FeatureClick("Bind_forward"); yield return FeatureKey(Key.I);
            FeatureClick("Bind_save"); yield return FeatureKey(Key.F6);
            yield return Capture("key-bindings.png");
            FeatureClick("CloseBindings"); FeatureClick("CloseSettings"); yield return null;
            Vector3 before = game.Player.transform.position;
            Keys(Key.W); yield return new WaitForSeconds(.25f); Keys(); yield return null;
            Check(Vector3.Distance(before, game.Player.transform.position) < .1f, "Original W stops moving after rebinding forward");
            Keys(Key.I); yield return new WaitForSeconds(.25f); Keys(); yield return new WaitForSeconds(.2f);
            Check(Vector3.Distance(before, game.Player.transform.position) > .5f, "Rebound I moves through actual player input");
            yield return FeatureKey(Key.M);
            Check(!game.Hud.MapOpen, "Old M no longer opens map after rebinding");
            yield return FeatureKey(Key.P);
            Check(game.Hud.MapOpen, "Rebound P opens full map");
            yield return FeatureKey(Key.P);
            Check(!game.Hud.MapOpen && game.Player.CanControl, "Rebound P closes map and restores gameplay");
        }
    }
}
