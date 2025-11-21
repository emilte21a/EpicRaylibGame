public static class SceneManager
{
    static readonly Dictionary<SCENE_NAME, Scene> actionsOnScene = new()
    {
        {SCENE_NAME.SCENE_START, new StartScene()},
        {SCENE_NAME.SCENE_MAIN, new MainScene()}

    };

    static SCENE_NAME currentScene = SCENE_NAME.SCENE_START;

    public static void UpdateGame()
    {
        actionsOnScene[currentScene].Update();
        actionsOnScene[currentScene].Draw();
    }

    public static void ChangeToScene(SCENE_NAME scene)
    {
        currentScene = scene;
    }
}

public enum SCENE_NAME
{
    SCENE_START,
    SCENE_MAIN
}