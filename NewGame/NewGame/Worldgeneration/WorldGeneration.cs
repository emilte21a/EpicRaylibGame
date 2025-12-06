using System.Text.Json;

public class WorldGeneration
{
    public Dictionary<(int, int), Chunk> chunkMap = [];
    public Player? playerRef { get; set; }

    public int chunkSize = Core.CHUNK_SIZE;
    public float leftLimit = 0;
    public float rightLimit = 0;
    public float upperLimit = 0;
    public float lowerLimit = 0;

    public int seed;

    private int generationDistance = 6;

    public List<(int, int)> visibleChunks = [];

    private static WorldGeneration? _instance;
    public static WorldGeneration Instance => _instance ??= new WorldGeneration();

    public GameSession? session;

    public WorldGeneration()
    {
        chunkMap.Clear();
    }

    public void InitializeSeed(int? initialSeed = null)
    {
        if (initialSeed != null)
            seed = (int)initialSeed;

        else
        {
            seed = Random.Shared.Next(-10000, 10000);
            System.Console.WriteLine("initial seed is null and is given a new one: " + seed);
        }

        InitializeChunks(7);
    }

    public void InitializeChunks(int chunkAmount)
    {
        for (int x = -chunkAmount; x <= chunkAmount; x++)
        {
            for (int y = -chunkAmount; y < chunkAmount; y++)
            {
                GenerateWorldFrom(new Vector2(x * chunkSize * Core.UNIT_SIZE, y * chunkSize * Core.UNIT_SIZE));
            }
        }
    }

    public void Update()
    {
        int chunkWidthUnits = chunkSize * Core.UNIT_SIZE;
        int chunkHeightUnits = chunkSize * Core.UNIT_SIZE;
        (int, int) playerChunkIndex = GetChunkIndexAtWorldPosition(playerRef.transform.position);

        visibleChunks.Clear();
        for (int x = -generationDistance; x <= generationDistance; x++)
        {
            for (int y = -generationDistance; y <= generationDistance; y++)
            {
                (int x, int y) chunkIndex = new(playerChunkIndex.Item1 + x, playerChunkIndex.Item2 + y);
                visibleChunks.Add(chunkIndex);
                float chunkStartX = chunkIndex.x * chunkWidthUnits;
                float chunkStartY = chunkIndex.y * chunkHeightUnits;
                if (!chunkMap.ContainsKey(chunkIndex))
                {
                    GenerateWorldFrom(new Vector2(chunkStartX, chunkStartY));
                }
            }
        }

        // UNLOAD chunks that are not in visibleChunks
        var visibleSet = new HashSet<(int, int)>(visibleChunks);
        var keysToUnload = chunkMap.Keys.Where(k => !visibleSet.Contains(k)).ToList();

        foreach (var key in keysToUnload)
        {
            UnloadChunk(key);
        }
    }

    // when generating/loading a chunk, call LoadFromDisk so player edits are restored
    void GenerateWorldFrom(Vector2 position)
    {
        // var sw = System.Diagnostics.Stopwatch.StartNew();

        (int x, int y) chunkIndex = ((int)MathF.Floor(position.X / (chunkSize * Core.UNIT_SIZE)), (int)MathF.Floor(position.Y / (chunkSize * Core.UNIT_SIZE)));
        float chunkStartX = chunkIndex.x * chunkSize * Core.UNIT_SIZE;
        float chunkStartY = chunkIndex.y * chunkSize * Core.UNIT_SIZE;
        if (!chunkMap.ContainsKey(chunkIndex))
        {
            var chunk = new Chunk(new Vector2(chunkStartX, chunkStartY), seed);
            chunkMap[chunkIndex] = chunk;

            if (session != null)
            {
                // System.Console.WriteLine("Applying chunk overrides");
                // Apply preloaded overrides from SaveGame
                session.ApplyChunkOverridesToChunk(chunk);
            }
        }

        // sw.Stop();
        // Console.WriteLine($"Chunk {chunkIndex} generated in {sw.ElapsedMilliseconds} ms");
    }

    public void UnloadChunk((int, int) chunkIndex)
    {
        if (chunkMap.TryGetValue(chunkIndex, out var chunk))
        {

            // perform chunk.Unload() if implemented (clear references / mark objects destroyed)
            chunk.Unload(); // adapt to your Unload implementation; call directly if method exists

            chunkMap.Remove(chunkIndex);
        }
    }

    public Tile? GetTileAtWorldPosition(Vector2 worldPos)
    {
        (int, int) chunkIndex = GetChunkIndexAtWorldPosition(worldPos);
        if (chunkMap.TryGetValue(chunkIndex, out var chunk))
        {
            var tileIndex = ((int)MathF.Floor(worldPos.X / Core.UNIT_SIZE), (int)MathF.Floor(worldPos.Y / Core.UNIT_SIZE));

            if (chunk.tileMap.TryGetValue(tileIndex, out var tile))
            {
                if (tile is MultiTilePart part) return part.parent;
                return tile;
            }
        }
        return null;
    }

    public Tile? GetTileAtTileCoordinate((int x, int y) tileCoordinates)
    {
        var pos = (tileCoordinates.x * Core.UNIT_SIZE, tileCoordinates.y * Core.UNIT_SIZE);
        (int, int) chunkIndex = GetChunkIndexAtWorldPosition(new Vector2(pos.Item1, pos.Item2));

        if (!chunkMap.TryGetValue(chunkIndex, out var chunk)) return null;

        if (!chunk.tileMap.TryGetValue(tileCoordinates, out var tile)) return null;

        if (tile is MultiTilePart part) return part.parent;
        return tile;
    }

    public (int, int) GetChunkIndexAtWorldPosition(Vector2 position)
    {
        var chunkIndex = ((int)MathF.Floor(position.X / (chunkSize * Core.UNIT_SIZE)), (int)MathF.Floor(position.Y / (chunkSize * Core.UNIT_SIZE)));
        return chunkIndex;
    }

    public (int, int) GetTileIndexAtWorldPosition(Vector2 position)
    {
        var tileIndex = ((int)MathF.Floor(position.X / Core.UNIT_SIZE), (int)MathF.Floor(position.Y / Core.UNIT_SIZE));
        return tileIndex;
    }

    public bool GetSurfaceIndexAtWorldX(int x, out int? surfaceIndex)
    {
        surfaceIndex = null;

        int worldTileX = x / Core.UNIT_SIZE;

        int chunkX = (int)MathF.Floor((float)worldTileX / chunkSize);

        var chunkRows = chunkMap.Keys
                        .Where(k => k.Item1 == chunkX)
                        .Select(k => k.Item2)
                        .Distinct()
                        .OrderBy(y => y)
                        .ToList();

        if (chunkRows.Count == 0)
        {
            return false;
        }

        foreach (var chunkRow in chunkRows)
        {
            var chunkIndex = (chunkX, chunkRow);
            if (!chunkMap.TryGetValue(chunkIndex, out var chunk))
                continue;

            int chunkStartTileY = chunkRow * chunkSize;
            int chunkEndTileY = chunkStartTileY + chunkSize - 1;

            for (int worldY = chunkStartTileY; worldY <= chunkEndTileY; worldY++)
            {
                var key = (worldTileX, worldY);
                if (chunk.tileMap.TryGetValue(key, out var tile) && tile != null && tile.isSolid)
                {
                    surfaceIndex = worldY;
                    return true;
                }
            }
        }

        return false; 
    }

    public void SaveChunk((int, int) chunkIndex)
    {
        if (!chunkMap.TryGetValue(chunkIndex, out var chunk)) return;
        if (session == null) return;

        try
        {
            var entries = new List<GameSaveManager.ChunkLoadedEntryDTO>();

            foreach (var kv in chunk.modifiedTiles)
            {
                var tileKey = kv.Key; 
                var dto = kv.Value;  

                var entry = new GameSaveManager.ChunkLoadedEntryDTO
                {
                    X = tileKey.Item1,
                    Y = tileKey.Item2,
                    Data = new Chunk.ModifiedTileDTO() 
                    {
                        TileId = dto.TileId,
                        ComponentType = dto.ComponentType,
                        ComponentJson = dto.ComponentJson
                    }
                };

                entries.Add(entry);
            }

            // canonical key format: "x_y"
            string chunkKey = $"{chunkIndex.Item1}_{chunkIndex.Item2}";

            // ensure the session save game dictionary exists
            if (session.Save.Chunks == null)
                session.Save.Chunks = new Dictionary<string, List<GameSaveManager.ChunkLoadedEntryDTO>>();

            session.Save.Chunks[chunkKey] = entries;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"SaveChunk error for {chunkIndex}: {ex.Message}");
        }
    }

    public void ApplyChunkOverrides(Dictionary<string, List<GameSaveManager.ChunkLoadedEntryDTO>> overrides)
    {
        foreach (var kvp in overrides)
        {
            string chunkKey = kvp.Key; // "(x,y)"
            if (!TryParseChunkKey(chunkKey, out var chunkIndex))
                continue;

            if (!chunkMap.TryGetValue(chunkIndex, out var chunk))
                continue;

            foreach (var entry in kvp.Value)
            {
                var dto = entry.Data;
                int localX = entry.X - chunkIndex.x * Core.CHUNK_SIZE;
                int localY = entry.Y - chunkIndex.y * Core.CHUNK_SIZE;
                var tileKey = (localX, localY);
                // store override (so any future save collects it)
                chunk.modifiedTiles[tileKey] = dto;

                // Apply removal
                if (dto.TileId == -1)
                {
                    chunk.RemoveTileAt(tileKey);
                    continue;
                }

                // Create tile
                var tile = TileFactory.CreateTileFromID(dto.TileId, tileKey);

                if (tile is MultiTilePart) continue;

                if (tile != null)
                {
                    tile.transform.position = new Vector2(
                        chunkIndex.x * Core.CHUNK_SIZE * Core.UNIT_SIZE + localX * Core.UNIT_SIZE,
                        chunkIndex.y * Core.CHUNK_SIZE * Core.UNIT_SIZE + localY * Core.UNIT_SIZE
                    );

                    if (tile is MultiTile multiTile)
                        chunk.PlaceMultiTile(multiTile, (int)multiTile.transform.position.X, (int)multiTile.transform.position.Y);
                    // i.OnPlaced((int)tile.transform.position.X, (int)tile.transform.position.Y);

                    chunk.tileMap[tileKey] = tile;
                    tile.Start();

                    // Restore component state
                    if (dto.ComponentType == "FurnaceComponent")
                    {
                        var furnace = tile.GetComponent<FurnaceComponent>();
                        if (!string.IsNullOrEmpty(dto.ComponentJson))
                        {
                            var state = JsonSerializer.Deserialize<FurnaceComponent.FurnaceStateDTO>(dto.ComponentJson);
                            furnace?.FromDTO(state);
                        }
                    }
                }
            }
        }
    }

    private bool TryParseChunkKey(string key, out (int x, int y) result)
    {
        result = default;
        if (string.IsNullOrWhiteSpace(key)) return false;

        key = key.Trim();

        // Format: "x_y"
        if (key.Contains("_"))
        {
            var parts = key.Split('_');
            if (parts.Length == 2 &&
                int.TryParse(parts[0], out var x) &&
                int.TryParse(parts[1], out var y))
            {
                result = (x, y);
                return true;
            }
        }

        // Remove parentheses if present: "(x,y)" or "x,y"
        var trimmed = key.Trim('(', ')').Trim();
        if (trimmed.Contains(","))
        {
            var parts = trimmed.Split(',');
            if (parts.Length == 2 &&
                int.TryParse(parts[0].Trim(), out var x) &&
                int.TryParse(parts[1].Trim(), out var y))
            {
                result = (x, y);
                return true;
            }
        }

        return false;
    }

    // Unload all loaded chunks and clear maps (safe to call when leaving to menu)
    public void UnloadAllChunks()
    {
        foreach (var kv in chunkMap.ToList())
        {
            var chunk = kv.Value;
            try
            {
                chunk.Unload();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error unloading chunk {kv.Key}: {ex.Message}");
            }
        }
        chunkMap.Clear();
        visibleChunks.Clear();
    }

}


