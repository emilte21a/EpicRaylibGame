public static class SceneManager
{
    static readonly Dictionary<SCENE_NAME, Func<Scene>> sceneFactories = new()
    {
        {SCENE_NAME.SCENE_START, ()=> new StartScene()}
    };

    static Scene currentScene;

    // cleanup global/game state when switching scenes to avoid duplicates
    static void CleanupGlobalStateForSceneChange()
    {
        // unload and clear world/chunks
        try { WorldGeneration.Instance.UnloadAllChunks(); } catch { }

        // clear global references
        Game.player = null;
        CollisionSystem.Instance.dynamicSpatialHash.Clear();
        CollisionSystem.Instance.staticSpatialHash.Clear();

        // clear game lists
        try { Game.GetGameObjects().Clear(); } catch { }

        // clear active particles
        try { ParticlePool.ActiveParticles.Clear(); } catch { }

        // reset any UI drag/drop state if you have it
        try { UIDragContext.Reset(); } catch { }
    }

    public static void ChangeToMainWithSession(GameSession session)
    {
        CleanupGlobalStateForSceneChange();
        currentScene = new MainScene(session);
    }

    public static void UpdateGame()
    {
        currentScene?.Update();
        currentScene?.Draw();

    }

    public static void ChangeToScene(SCENE_NAME scene)
    {
        // If switching back to start menu, clean up the current game state
        CleanupGlobalStateForSceneChange();
        currentScene = sceneFactories[scene]();
        System.Console.WriteLine("changed scene to" + scene);
    }

    public static void Initialize()
    {
        ChangeToScene(SCENE_NAME.SCENE_START);
    }
}

public enum SCENE_NAME
{
    SCENE_START,
    SCENE_MAIN
}