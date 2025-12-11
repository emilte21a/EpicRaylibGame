using LibNoise.Primitive;
using SharpNoise.Modules;

public abstract class Scene
{
    protected Texture2D cursorTexture = Raylib.LoadTexture("Textures/cursor.png");
    public abstract void Update();
    public abstract void Draw();

    protected void DrawCursor()
    {
        Vector2 mousePos = Raylib.GetMousePosition();
        var dst = new Rectangle(
                  mousePos.X,
                  mousePos.Y,
                  cursorTexture.Width,
                  cursorTexture.Height
              );

        var origin = Vector2.Zero;
        var src = new Rectangle(0, 0, cursorTexture.Width, cursorTexture.Height);
        Raylib.DrawTexturePro(cursorTexture, src, dst, origin, 0, Color.White);
    }
}

public class StartScene : Scene
{
    List<GameSaveManager.SaveIndexEntry> saveList = null;
    int selectedSaveIndex = -1;

    // UI layout values reused in Update + Draw
    readonly int uiX = 40;
    readonly int uiY = 80;
    readonly int listWidth = 400;
    readonly int listItemHeight = 28;

    void RefreshSaveList()
    {
        saveList = GameSaveManager.GetSaves();
    }

    public override void Update()
    {
        // ensure list populated
        if (saveList == null) RefreshSaveList();

        // handle mouse input here (update step)
        var mouse = Raylib.GetMousePosition();
        // selection clicks
        for (int i = 0; i < (saveList?.Count ?? 0); i++)
        {
            var r = new Raylib_cs.Rectangle(uiX, uiY + i * listItemHeight, listWidth, listItemHeight - 4);
            if (Raylib.IsMouseButtonPressed(MouseButton.Left) && Raylib.CheckCollisionPointRec(mouse, r))
            {
                selectedSaveIndex = i;
            }
        }

        // Load button
        var loadRect = new Raylib_cs.Rectangle(uiX, 520, 120, 36);
        if (Raylib.IsMouseButtonPressed(MouseButton.Left) && Raylib.CheckCollisionPointRec(mouse, loadRect))
        {
            if (selectedSaveIndex >= 0 && saveList != null && selectedSaveIndex < saveList.Count)
            {
                var id = saveList[selectedSaveIndex].Id;
                Console.WriteLine($"Loading save {id}");

                var save = SaveSystem.Load(id);
                if (save == null)
                {
                    Console.WriteLine("Failed to load save.");
                    return;
                }

                var session = new GameSession(save);
                GameSaveManager.CurrentLoadedSaveId = id;
                SceneManager.ChangeToMainWithSession(session);
                return;
            }
        }

        // Delete button
        var deleteRect = new Raylib_cs.Rectangle(uiX + 140, 520, 120, 36);
        if (Raylib.IsMouseButtonPressed(MouseButton.Left) && Raylib.CheckCollisionPointRec(mouse, deleteRect))
        {
            if (selectedSaveIndex >= 0 && saveList != null && selectedSaveIndex < saveList.Count)
            {
                var id = saveList[selectedSaveIndex].Id;
                GameSaveManager.DeleteSave(id);
                RefreshSaveList();
                selectedSaveIndex = -1;
            }
        }

        // New save button
        var newRect = new Raylib_cs.Rectangle(uiX + 280, 520, 120, 36);
        if (Raylib.IsMouseButtonPressed(MouseButton.Left) && Raylib.CheckCollisionPointRec(mouse, newRect))
        {
            var name = "Save " + DateTime.Now.ToString("yyyyMMdd_HHmm");

            // Create save file, get new ID
            var newId = GameSaveManager.CreateAndSave(name);

            // Now load that new save through the unified SaveSystem
            var save = SaveSystem.Load(newId);
            if (save == null)
            {
                Console.WriteLine("Failed to load newly created save.");
                return;
            }

            // Create the in-memory session
            var session = new GameSession(save);
            GameSaveManager.CurrentLoadedSaveId = newId;
            // Start the game using the new session
            SceneManager.ChangeToMainWithSession(session);
            return;
        }
    }

    public override void Draw()
    {
        Raylib.BeginDrawing();
        Raylib.ClearBackground(Color.White);

        // draw UI
        Raylib.DrawText("Load Game", uiX, 40, 36, Color.White);

        for (int i = 0; i < (saveList?.Count ?? 0); i++)
        {
            var s = saveList[i];
            var r = new Raylib_cs.Rectangle(uiX, uiY + i * listItemHeight, listWidth, listItemHeight - 4);
            Color bg = (i == selectedSaveIndex) ? Color.DarkGray : Color.Gray;
            Raylib.DrawRectangleRec(r, bg);
            Raylib.DrawText($"{s.Name} - {s.Timestamp.ToLocalTime()}", uiX + 6, uiY + i * listItemHeight + 4, 14, Color.White);
        }

        // buttons
        var loadRect = new Raylib_cs.Rectangle(uiX, 520, 120, 36);
        Raylib.DrawRectangleRec(loadRect, Color.LightGray);
        Raylib.DrawText("Load", (int)loadRect.X + 18, (int)loadRect.Y + 6, 20, Color.Black);

        var deleteRect = new Raylib_cs.Rectangle(uiX + 140, 520, 120, 36);
        Raylib.DrawRectangleRec(deleteRect, Color.LightGray);
        Raylib.DrawText("Delete", (int)deleteRect.X + 12, (int)deleteRect.Y + 6, 20, Color.Black);

        var newRect = new Raylib_cs.Rectangle(uiX + 280, 520, 120, 36);
        Raylib.DrawRectangleRec(newRect, Color.LightGray);
        Raylib.DrawText("New Save", (int)newRect.X + 8, (int)newRect.Y + 6, 20, Color.Black);
        DrawCursor();

        Raylib.EndDrawing();
    }
}

public class MainScene : Scene
{
    PhysicsSystem physicsSystem = new PhysicsSystem();
    bool isPaused = false;
    Player player;
    GameSession session;
    // UIButton uIButton = new UIButton(100, 50, UIElement.PositionOnScreen.MIDDLE, UIElement.PositionOnScreen.MIDDLE, "buton", 10, Color.Black);

    public MainScene(GameSession gameSession)
    {
        session = gameSession;
        SlotUtils.Clear();
        player = new();
        WorldGeneration.Instance.session = session;
        WorldGeneration.Instance.InitializeSeed(session.Seed);
        WorldGeneration.Instance.playerRef = player;
        player.SetPlayerPos(session.PlayerStartPosition);
        DebugMenu.Instance.playerRef = player;
        ItemFactory.LoadItems("ItemData.JSON");
        LightingSystem.Instance.Initialize();
    }

    public override void Update()
    {
        if (Raylib.IsKeyPressed(KeyboardKey.Escape)) isPaused = !isPaused;
        if (isPaused) return;

        WorldGeneration.Instance.Update();
        physicsSystem.Update();
        CollisionSystem.Instance.Update();

        CameraSystem.Instance.Update();
        CameraSystem.Instance.SetTarget(Raymath.Vector2Lerp(CameraSystem.Instance.GetTarget(), player.transform.position, 15 * Raylib.GetFrameTime()));
        DayNightSystem.Instance.Update();

        // LightingSystem.Instance.Update();

        foreach ((int, int) chunkIndex in WorldGeneration.Instance.visibleChunks)
        {
            if (!WorldGeneration.Instance.chunkMap.TryGetValue(chunkIndex, out var chunk)) continue;

            foreach (KeyValuePair<(int, int), Tile> tile in chunk.tileMap)
            {
                tile.Value.Update();
            }
            foreach (var tile in chunk.backgroundTileMap)
            {
                tile.Value.Update();
            }
        }

        foreach (Entity entity in Game.GetEntities().ToList())
        {
            entity.Update();
        }

        foreach (var p in ParticlePool.ActiveParticles.ToList())
            p.Update();

        foreach (var go in Game.GetGameObjects())
        {
            if (go is not UserInterface ui) continue;
            if (go is UIElement) continue;
            ui.Update();
        }

        SlotUtils.Update();

        var interactable = player.GetInteractableTile();
        if (interactable != null && interactable.userInterface != null && interactable.userInterface.IsOpen())
            interactable.userInterface.Update();

        Game.GetGameObjects().RemoveAll(go => (go is DroppedItem d && d.pickedUp) || go.shouldBeDestroyed);
        Game.GetEntities().RemoveAll(go => (go is DroppedItem d && d.pickedUp) || go.shouldBeDestroyed);
    }

    float zoom = CameraSystem.Instance.GetCamera().Zoom;

    public override void Draw()
    {
        // LightingSystem.Instance.RenderAll();
        Raylib.BeginTextureMode(CameraSystem.Instance.pixelPerfectTargetTexture);
        DayNightSystem.Instance.DrawSkyBackground();
        DayNightSystem.Instance.Draw();
        ParallaxHandler.Draw();
        Raylib.BeginMode2D(CameraSystem.Instance.GetCamera());

        DrawInOrder();

        Raylib.EndMode2D();
        // LightingSystem.Instance.Draw();

        Raylib.EndTextureMode();

        Raylib.BeginDrawing();
        Raylib.DrawTexturePro(CameraSystem.Instance.pixelPerfectTargetTexture.Texture, CameraSystem.Instance.sourceRec, CameraSystem.Instance.destRec, Vector2.Zero, 0.0f, Color.White);

        zoom = RayGui.GuiSlider(new Raylib_CsLo.Rectangle(50, 100, 100, 10), "Zoom", $"{zoom}", zoom, 0.01f, 2);
        CameraSystem.Instance.SetZoom(zoom);
        Raylib.DrawText($"{DayNightSystem.Instance.GetCurrentTime() / DayNightSystem.TimeScale}", 50, 400, 20, Color.Black);
        DayNightSystem.TimeScale = RayGui.GuiSlider(new Raylib_CsLo.Rectangle(50, 600, 100, 10), "Time Scale", $"{DayNightSystem.TimeScale}", DayNightSystem.TimeScale, 1f, 60f);
        DebugMenu.Instance.UpdateDebug();

        player.inventory.Draw();

        // draw any interactable UI that's attached to the player's current tile (keeps it on top)
        if (player.GetInteractableTile() != null && player.GetInteractableTile().userInterface != null)
        {
            if (player.GetInteractableTile().userInterface.IsOpen())
                player.GetInteractableTile().userInterface.Draw();
        }

        SlotUtils.DrawDraggingSlot();

        Raylib.DrawText($"{WorldGeneration.Instance.GetTileIndexAtWorldPosition(player.transform.position)}", 10, 50, 50, Color.Orange);
        var shouldExit = false;
        if (isPaused)
        {
            bool exitButton = RayGui.GuiButton(new Raylib_CsLo.Rectangle(Game.screenWidth / 2 - 100, Game.screenHeight / 4, 200, 100), "Save and Exit");
            if (exitButton)
            {
                shouldExit = true;
            }
            bool shouldGoToMenu = RayGui.GuiButton(new Raylib_CsLo.Rectangle(Game.screenWidth / 2 - 100, Game.screenHeight / 4 * 2f, 200, 100), "Save and go to menu");
            if (shouldGoToMenu)
            {
                Save();
                SceneManager.ChangeToScene(SCENE_NAME.SCENE_START);
            }
        }
        // Raylib.DrawText($"{SlotUtils.hoveredSlot}", 200, 400, 20, Color.White);

        for (int i = 0; i < SlotUtils.GetInterfaces().Count; i++)
        {
            Raylib.DrawText($"{SlotUtils.GetInterfaces()[i].tag}", 200, 500 + i * 20, 20, Color.White);
        }
        // isOpen: {SlotUtils.GetInterfaces()[i].IsOpen()}
        Raylib.DrawText($"go count: {Game.GetGameObjects().Count}", 200, 900, 20, Color.White);
        if (SlotUtils.hoveredSlot != null)
            Raylib.DrawText($"hoveredSlot: {SlotUtils.hoveredSlot}", 200, 1000, 20, Color.White);

        DrawCursor();

        Raylib.DrawFPS(10, 10);
        Raylib.EndDrawing();
        if (shouldExit)
        {
            Save();
            Raylib.CloseWindow();
        }
    }

    public void DrawInOrder()
    {
        foreach (var chunkIndex in WorldGeneration.Instance.visibleChunks)
        {
            if (WorldGeneration.Instance.chunkMap.TryGetValue(chunkIndex, out var chunk))
            {
                foreach (var tile in chunk.backgroundTileMap.Values)
                {

                    if (tile is MultiTilePart) continue;

                    var col = tile.GetComponent<Collider>();
                    if (col != null && Raylib.CheckCollisionRecs(Game.screenRectangle, col.boxCollider))
                    {
                        tile.Draw();
                    }
                }
                foreach (var tile in chunk.tileMap.Values)
                {
                    // skip lightweight parts (they are markers only)
                    if (tile is MultiTilePart) continue;

                    var col = tile.GetComponent<Collider>();
                    if (col != null && Raylib.CheckCollisionRecs(Game.screenRectangle, col.boxCollider))
                    {
                        tile.Draw();
                    }
                }
                // Raylib.DrawRectangleLinesEx(new Raylib_cs.Rectangle(chunk.position.X, chunk.position.Y, Core.CHUNK_SIZE * Core.UNIT_SIZE, Core.CHUNK_SIZE * Core.UNIT_SIZE), 1, Color.Green);
            }
        }

        foreach (var p in ParticlePool.ActiveParticles)
            p.Draw();

        foreach (var obj in Game.GetGameObjects().ToList())
        {
            if (obj is Tile)
                continue;

            if (obj is Particle)
                continue;

            if (obj is UserInterface)
                continue;

            if (obj is ParallaxLayer)
                continue;

            if (obj is UIElement)
                continue;

            obj.Draw();
            // foreach (var item in CollisionSystem.Instance.staticSpatialHash.QueryNearby(obj))
            // {
            //     Raylib.DrawRectangleLinesEx(item.GetComponent<Collider>().boxCollider, 1, Color.Yellow);
            // }
            // foreach (var item in CollisionSystem.Instance.dynamicSpatialHash.QueryNearby(obj))
            // {
            //     Raylib.DrawRectangleLinesEx(item.GetComponent<Collider>().boxCollider, 1, Color.DarkBlue);
            // }
        }
    }

    #region Saving session

    private void Save()
    {
        foreach (var kvp in WorldGeneration.Instance.chunkMap)
        {
            var chunkIndex = kvp.Key;
            var chunk = kvp.Value;

            var list = chunk.modifiedTiles.Select(kvp2 => new GameSaveManager.ChunkLoadedEntryDTO
            {
                X = kvp2.Key.x,
                Y = kvp2.Key.y,
                Data = kvp2.Value
            }).ToList();

            session.Save.Chunks[$"({chunkIndex.Item1},{chunkIndex.Item2})"] = list;
        }

        session.Save.PlayerX = player.transform.position.X;
        session.Save.PlayerY = player.transform.position.Y;

        SaveSystem.Save(session.Save);
    }
    #endregion
}
