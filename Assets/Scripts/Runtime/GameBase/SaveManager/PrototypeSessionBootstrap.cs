using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>TestScene explicitly owns an in-memory session; it can never inherit a player's writable slot.</summary>
public static class PrototypeSessionBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Hook()
    {
        SceneManager.sceneLoaded -= Enter;
        SceneManager.sceneLoaded += Enter;
    }
    private static void Enter(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "TestScene" && !GameSession.Instance.IsSandbox)
            GameSession.Instance.BeginSandbox();
    }
}
