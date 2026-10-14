using UnityEditor;
using UnityEditor.Build;

namespace Tycoon.Editor
{
    public static class OptimizedPlayerBuilder
    {
        public static void BuildWindows()
        {
            var target = NamedBuildTarget.Standalone;
            var previous = PlayerSettings.GetScriptingBackend(target);
            PlayerSettings.SetScriptingBackend(target, ScriptingImplementation.IL2CPP);
            try
            {
                // Giữ nguyên scene, shader và tùy chọn build để đối chứng chi phí CPU.
                ProjectBuilder.BuildWindows();
            }
            finally
            {
                PlayerSettings.SetScriptingBackend(target, previous);
                AssetDatabase.SaveAssets();
            }
        }
    }
}
