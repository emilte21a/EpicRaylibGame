public class MiniMap
{

    RenderTexture2D minimap = Raylib.LoadRenderTexture(200, 200);

    private static MiniMap? _instance;
    public static MiniMap Instance => _instance ??= new MiniMap();

    public MiniMap()
    {
        InitializeMinimap();
    }

    private void InitializeMinimap()
    {
        Raylib.BeginTextureMode(minimap);
        Raylib.ClearBackground(Color.Black);
        Raylib.EndTextureMode();
    }

    public void RenderMinimap()
    {
        foreach (var chunkIndex in WorldGeneration.Instance.visibleChunks)
        {
            if (WorldGeneration.Instance.chunkMap.TryGetValue(chunkIndex, out var chunk))
            {
                foreach (var tile in chunk.tileMap)
                {
                    Raylib.BeginTextureMode(minimap);
                    Raylib.ClearBackground(Color.Black);
                    Raylib.DrawRectangle(tile.Key.Item1, tile.Key.Item2, 1, 1, tile.Value.color);
                    Raylib.EndTextureMode();
                }
            }
        }
    }

    public void DrawMiniMap()
    {
        Raylib.DrawTexturePro(minimap.Texture, new Rectangle(0, 0, minimap.Texture.Width, -minimap.Texture.Height),
        new Rectangle(400, 400, minimap.Texture.Width, minimap.Texture.Height), Vector2.Zero, 0, Color.White
        );
    }


}