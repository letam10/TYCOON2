using System.Collections;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tycoon.Tests
{
    public abstract class CityPedestrianPlayModeFixture
    {
        [UnitySetUp]
        public IEnumerator EnterNativeNavigation()
        {
            // NavMeshAgent chỉ đăng ký và mô phỏng trong PlayMode; graph thuần không cần bước này.
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            yield return new EnterPlayMode();
            CreateWorld();
        }

        [UnityTearDown]
        public IEnumerator LeaveNativeNavigation()
        {
            ClearWorld();
            if (Application.isPlaying) yield return new ExitPlayMode();
        }

        protected virtual void CreateWorld() { }
        protected abstract void ClearWorld();
    }
}
