public abstract class Scene
{
    public abstract void Update();
    public abstract void Draw();
}

public class StartScene : Scene
{
    bool changeScene;

    public override void Update()
    {
        if (changeScene) SceneManager.ChangeToScene(SCENE_NAME.SCENE_MAIN);
    }

    public override void Draw()
    {
        Raylib.BeginDrawing();
        Raylib.ClearBackground(Color.SkyBlue);
        changeScene = RayGui.GuiButton(new Raylib_CsLo.Rectangle(100, 100, 400, 200), "Start Game");
        Raylib.EndDrawing();
    }
}

public class MainScene : Scene
{
    PhysicsSystem physicsSystem = new PhysicsSystem();
    Player player = new();

    public MainScene()
    {
        WorldGeneration.Instance.playerRef = player;
        DebugMenu.Instance.playerRef = player;
        ItemFactory.LoadItems("ItemData.JSON");
        LightingSystem.Instance.Initialize();
    }

    public override void Update()
    {
        WorldGeneration.Instance.Update();
        physicsSystem.Update();
        CollisionSystem.Instance.Update();

        CameraSystem.Instance.Update();
        CameraSystem.Instance.SetTarget(Raymath.Vector2Lerp(CameraSystem.Instance.GetTarget(), player.transform.position, 15 * Raylib.GetFrameTime()));

        LightingSystem.Instance.Update();

        foreach ((int, int) chunkIndex in WorldGeneration.Instance.visibleChunks)
        {
            if (!WorldGeneration.Instance.chunkMap.TryGetValue(chunkIndex, out var chunk)) continue;

            foreach (KeyValuePair<(int, int), Tile> tile in chunk.tileMap)
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

            ui.Update();
        }

        SlotUtils.Update();

        player.inventory.Update();

        var interactable = player.GetInteractableTile();
        if (interactable != null && interactable.userInterface != null && interactable.userInterface.IsOpen())
            interactable.userInterface.Update();

        Game.GetGameObjects().RemoveAll(go => (go is DroppedItem d && d.pickedUp) || go.shouldBeDestroyed);
        Game.GetEntities().RemoveAll(go => (go is DroppedItem d && d.pickedUp) || go.shouldBeDestroyed);
    }

    float zoom = CameraSystem.Instance.GetCamera().Zoom;

    public override void Draw()
    {
        LightingSystem.Instance.ComputeLighting();
        LightingSystem.Instance.RenderLightingTexture();
        LightingSystem.Instance.PerformKawaseRenderPass();
        Raylib.BeginTextureMode(CameraSystem.Instance.pixelPerfectTargetTexture);
        Raylib.ClearBackground(Color.SkyBlue);
        Raylib.BeginMode2D(CameraSystem.Instance.GetCamera());

        //Draw world objects here

        DrawInOrder();
        // player.DrawHoveringTile();

        Raylib.EndMode2D();
        LightingSystem.Instance.Draw();

        Raylib.EndTextureMode();

        Raylib.BeginDrawing();
        Raylib.DrawTexturePro(CameraSystem.Instance.pixelPerfectTargetTexture.Texture, CameraSystem.Instance.sourceRec, CameraSystem.Instance.destRec, Vector2.Zero, 0.0f, Color.White);

        zoom = RayGui.GuiSlider(new Raylib_CsLo.Rectangle(50, 100, 100, 10), "Zoom", $"{zoom}", zoom, 0.01f, 2);
        CameraSystem.Instance.SetZoom(zoom);

        DebugMenu.Instance.UpdateDebug();

        player.inventory.Draw();

        // draw any interactable UI that's attached to the player's current tile (keeps it on top)
        if (player.GetInteractableTile() != null && player.GetInteractableTile().userInterface != null)
        {
            if (player.GetInteractableTile().userInterface.IsOpen())
                player.GetInteractableTile().userInterface.Draw();
        }

        SlotUtils.DrawDraggingSlot();

        Raylib.DrawText($"{WorldGeneration.Instance.GetTileIndexAtPosition(player.transform.position)}", 10, 50, 50, Color.Orange);

        Raylib.DrawFPS(10, 10);
        Raylib.EndDrawing();
    }



    public void DrawInOrder()
    {
        foreach (var chunkIndex in WorldGeneration.Instance.visibleChunks)
        {
            if (WorldGeneration.Instance.chunkMap.TryGetValue(chunkIndex, out var chunk))
            {
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
}
