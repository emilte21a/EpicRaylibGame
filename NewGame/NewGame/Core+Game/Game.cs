global using Raylib_cs;
global using System.Numerics;
global using System.Threading;
global using SharpNoise;
global using Raylib_CsLo;
global using Color = Raylib_cs.Color;
global using ConfigFlags = Raylib_cs.ConfigFlags;
global using KeyboardKey = Raylib_cs.KeyboardKey;
global using Raylib = Raylib_cs.Raylib;
global using Rectangle = Raylib_cs.Rectangle;

public class Game
{
    public static int screenWidth = 1920;
    public static int screenHeight = 1080;

    public static int windowWidth;
    public static int windowHeight;

    public static Rectangle screenRectangle;

    private static List<GameObject> gameObjects = [];
    private static List<Entity> entities = [];

    private bool paused = false;

    public Game()
    {
        Raylib.SetConfigFlags(ConfigFlags.ResizableWindow);
        Raylib.InitWindow(screenWidth, screenHeight, "Game");
        Raylib.SetConfigFlags(ConfigFlags.AlwaysRunWindow);
        // Raylib.ToggleFullscreen();
        Raylib.SetExitKey(KeyboardKey.Null);
        // LightingSystem.Instance.EnsureRenderTextureSize();

        windowWidth = Raylib.GetScreenWidth();
        windowHeight = Raylib.GetScreenHeight();

        screenRectangle = new Rectangle(0, 0, screenWidth, screenHeight);

    }

    public void Run()
    {
        while (!Raylib.WindowShouldClose())
        {
            HandleScreen();
            HandlePauseScreen();

            if (!paused)
                SceneManager.UpdateGame();
        }

        Raylib.CloseWindow();
    }

    void HandlePauseScreen()
    {
        if (Raylib.IsKeyPressed(KeyboardKey.Escape) && !paused)
        {
            paused = true;
            Console.WriteLine("Paused: " + paused);
        }
        else if (Raylib.IsKeyPressed(KeyboardKey.Escape) && paused)
        {
            paused = false;
            Console.WriteLine("Paused: " + paused);
        }
    }

    void HandleScreen()
    {
        Vector2 camTarget = CameraSystem.Instance.GetTarget();
        float camZoom = CameraSystem.Instance.GetCamera().Zoom;

        screenRectangle.Width = screenWidth / camZoom;
        screenRectangle.Height = screenHeight / camZoom;
        screenRectangle.X = camTarget.X - screenRectangle.Width / 2f;
        screenRectangle.Y = camTarget.Y - screenRectangle.Height / 2f;

        if (Raylib.IsWindowResized())
        {
            windowWidth = Raylib.GetScreenWidth();
            windowHeight = Raylib.GetScreenHeight();
        }

        if (Raylib.IsKeyPressed(KeyboardKey.F11))
        {
            Raylib.ToggleFullscreen();
        }
    }

    public static void AddGameObjectToGame(GameObject gameObject)
    {
        gameObjects.Add(gameObject);
    }

    public static void AddEntityToGame(Entity entity)
    {
        entities.Add(entity);
    }

    // private static bool needsSort = false;

    // public static void SortByZ()
    // {
    //     if (!needsSort) return;
    //     var sw = System.Diagnostics.Stopwatch.StartNew();
    //     gameObjects.Sort((a, b) =>
    //    {
    //        var aTransform = a.GetComponent<Transform>();
    //        var bTransform = b.GetComponent<Transform>();
    //        float az = aTransform?.z ?? 0;
    //        float bz = bTransform?.z ?? 0;
    //        return az.CompareTo(bz);
    //    });
    //     sw.Stop();
    //     Console.WriteLine($"Sorting took {sw.ElapsedMilliseconds} ms");

    //     if (needsSort) needsSort = false;
    // }

    // public static void MarkNeedsSort()
    // {
    //     needsSort = true;
    // }

    public static List<GameObject> GetGameObjects()
    {
        return gameObjects;
    }

    public static List<Entity> GetEntities()
    {
        return entities;
    }

    public static void RemoveGameObject(GameObject obj)
    {
        if (gameObjects.Contains(obj))
            gameObjects.Remove(obj);
    }
}