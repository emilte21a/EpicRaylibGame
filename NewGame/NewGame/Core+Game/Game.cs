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
using Image = Raylib_cs.Image;

public class Game
{
    public static int screenWidth = 1920;
    public static int screenHeight = 1080;

    public static int windowWidth;
    public static int windowHeight;

    public static Rectangle screenRectangle;

    private static List<GameObject> gameObjects = [];

    private bool paused = false;

    Image icon;
    public static Player? player;

    public Game()
    {
        Raylib.SetConfigFlags(ConfigFlags.ResizableWindow);
        Raylib.SetTargetFPS(300);
        icon = Raylib.LoadImage("Textures/grass.png");
        Raylib.SetWindowIcon(icon);
        Raylib.InitWindow(screenWidth, screenHeight, "Game");
        Raylib.SetConfigFlags(ConfigFlags.AlwaysRunWindow);
        // Raylib.ToggleFullscreen();
        Raylib.SetExitKey(KeyboardKey.Null);
        Raylib.HideCursor();
        // LightingSystem.Instance.EnsureRenderTextureSize();

        windowWidth = Raylib.GetScreenWidth();
        windowHeight = Raylib.GetScreenHeight();

        screenRectangle = new Rectangle(0, 0, screenWidth, screenHeight);
        SceneManager.Initialize();

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

        // Raylib.CloseWindow();
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

    public static List<GameObject> GetGameObjects()
    {
        return gameObjects;
    }

    public static void RemoveGameObject(GameObject obj)
    {
        if (gameObjects.Contains(obj))
            gameObjects.Remove(obj);
    }
}