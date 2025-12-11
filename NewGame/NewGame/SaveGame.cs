using System;
using System.IO;
using System.Text.Json;
using System.Collections.Generic;

// A plain DTO for JSON (uses your existing Chunk DTO type)
public class SaveGame
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public float PlayerX { get; set; }
    public float PlayerY { get; set; }
    public int WorldSeed { get; set; }

    // NOTE: use the Chunk DTO type you already have (nested in GameSaveManager in your earlier code)
    public Dictionary<string, List<GameSaveManager.ChunkLoadedEntryDTO>> Chunks { get; set; }
        = new Dictionary<string, List<GameSaveManager.ChunkLoadedEntryDTO>>();
}

public static class SaveSystem
{
    // default to the same relative path pattern your previous manager used
    static string DefaultGamesFolder => Path.Combine(Environment.CurrentDirectory, "saves", "games");

    // Load by id. Optional parameter lets you pass a different folder for tests / editor.
    public static SaveGame? Load(string id, string? gamesFolder = null)
    {
        var folder = gamesFolder ?? DefaultGamesFolder;
        var path = Path.Combine(folder, $"{id}.json");
        if (!File.Exists(path)) return null;

        var json = File.ReadAllText(path);
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        return JsonSerializer.Deserialize<SaveGame>(json, options);
    }

    // Save (creates folder if necessary). Optional folder override.
    public static void Save(SaveGame save, string? gamesFolder = null)
    {
        var folder = gamesFolder ?? DefaultGamesFolder;
        Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, $"{save.Id}.json");
        var options = new JsonSerializerOptions { WriteIndented = true };
        File.WriteAllText(path, JsonSerializer.Serialize(save, options));
    }
}

// A simple runtime holder (no globals)
public class GameSession
{
    public SaveGame Save { get; }

    public GameSession(SaveGame save)
    {
        Save = save ?? throw new ArgumentNullException(nameof(save));
    }

    public Vector2 PlayerStartPosition => new Vector2(Save.PlayerX, Save.PlayerY);
    public int Seed => Save.WorldSeed;
    public Dictionary<string, List<GameSaveManager.ChunkLoadedEntryDTO>> ChunkOverrides => Save.Chunks;

    public void ApplyChunkOverridesToChunk(Chunk chunk)
    {
        var cIndex = WorldGeneration.Instance.GetChunkIndexAtWorldPosition(chunk.position);
        int cx = cIndex.Item1;
        int cy = cIndex.Item2;

        // try a few common key formats
        string keyUnderscore = $"{cx}_{cy}";
        string keyParenComma = $"({cx},{cy})";
        string keyComma = $"{cx},{cy}";

        if (!ChunkOverrides.TryGetValue(keyUnderscore, out var entries) &&
            !ChunkOverrides.TryGetValue(keyParenComma, out entries) &&
            !ChunkOverrides.TryGetValue(keyComma, out entries))
            return;

        foreach (var entry in entries)
        {
            var tileIndex = (entry.X, entry.Y);
            var dto = entry.Data;
            chunk.modifiedTiles[tileIndex] = dto;

            if (dto.TileId == -1)
            {
                chunk.RemoveTileAt(tileIndex);
                continue;
            }

            var tile = TileFactory.CreateTileFromID(dto.TileId, tileIndex);

            if (tile is MultiTile mt)
                mt.OnPlaced((int)mt.transform.position.X, (int)mt.transform.position.Y);

            System.Console.WriteLine(tile == null ? "tile is null" : "tile is not null");
            if (tile != null)
            {
                tile.transform.position = new Vector2(
                    tileIndex.Item1 * Core.UNIT_SIZE,
                    tileIndex.Item2 * Core.UNIT_SIZE
                );

                chunk.tileMap[tileIndex] = tile;

                if (dto.ComponentType == "FurnaceComponent" && !string.IsNullOrEmpty(dto.ComponentJson))
                {
                    var f = tile.GetComponent<FurnaceComponent>();
                    var dtoObj = JsonSerializer.Deserialize<FurnaceComponent.FurnaceStateDTO>(dto.ComponentJson);
                    if (f != null && dtoObj != null)
                        f.FromDTO(dtoObj);
                }

                tile.Start();

                var col = tile.GetComponentFast<Collider>();
                if (col != null && col.isActive)
                    CollisionSystem.Instance.staticSpatialHash.Insert(tile);

                // Game.AddGameObjectToGame(tile);
            }
        }
    }
}