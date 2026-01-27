using System.Text.Json;
using System.IO;
using System.Collections.Generic;
using System;

public static class GameSaveManager
{
    static readonly string SavesRoot = Path.Combine(Environment.CurrentDirectory, "saves");
    static readonly string ChunksFolder = Path.Combine(SavesRoot, "chunks"); // used by Chunk.LoadFromDisk
    static readonly string GamesFolder = Path.Combine(SavesRoot, "games");
    static readonly string IndexPath = Path.Combine(GamesFolder, "index.json");

    public static bool UsePerChunkSaves = true; // existing toggle

    // in-memory currently loaded save id (null when none)
    public static string? CurrentLoadedSaveId { get; set; } = null;

    static GameSaveManager()
    {
        Directory.CreateDirectory(SavesRoot);
        Directory.CreateDirectory(ChunksFolder);
        Directory.CreateDirectory(GamesFolder);
        if (!File.Exists(IndexPath))
        {
            File.WriteAllText(IndexPath, "[]");
        }
    }

    // Save index entry used to show available saves
    public class SaveIndexEntry
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string FilePath { get; set; } = "";
    }

    // complete save payload
    public class SaveData
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        // simple player snapshot (expand as you add player/inventory DTOs)
        public float PlayerX { get; set; }
        public float PlayerY { get; set; }
        public int WorldSeed { get; set; }

        // Per-chunk saved modified tiles.
        // Key: chunk key "cx_cy" (chunk coords), Value: list of entries with local X/Y and DTO
        public Dictionary<string, List<ChunkLoadedEntryDTO>> Chunks { get; set; } = new Dictionary<string, List<ChunkLoadedEntryDTO>>();
    }

    // matches the LoadedEntry shape Chunk.LoadFromDisk expects
    public class ChunkLoadedEntryDTO
    {
        public int X { get; set; }
        public int Y { get; set; }
        public Chunk.ModifiedTileDTO Data { get; set; } = new Chunk.ModifiedTileDTO();
    }

    // get index
    public static List<SaveIndexEntry> GetSaves()
    {
        try
        {
            var json = File.ReadAllText(IndexPath);
            return JsonSerializer.Deserialize<List<SaveIndexEntry>>(json) ?? new List<SaveIndexEntry>();
        }
        catch
        {
            return new List<SaveIndexEntry>();
        }
    }

    // Save the index file
    static void WriteIndex(List<SaveIndexEntry> list)
    {
        var json = JsonSerializer.Serialize(list, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(IndexPath, json);
    }

    // Create a new save (calls SaveAll)
    public static string CreateAndSave(string name)
    {
        var id = Guid.NewGuid().ToString("N");
        // generate a fresh seed for this new save so each save can have its own world
        int newSeed = Random.Shared.Next(int.MinValue / 2, int.MaxValue / 2);
        SaveAll(id, name, newSeed);
        return id;
    }

    // Save entire game to a single JSON (SaveAll)
    // This collects modifiedTiles from all loaded chunks and a small player snapshot.
    public static void SaveAll(string saveId, string saveName, int? overrideSeed = null)
    {
        Directory.CreateDirectory(GamesFolder);

        var save = new SaveData();
        save.Id = saveId;
        save.Name = saveName;
        save.Timestamp = DateTime.UtcNow;

        // prefer provided seed, otherwise fall back to current world seed
        save.WorldSeed = overrideSeed ?? WorldGeneration.Instance.seed;

        // player snapshot (best-effort)
        var player = Game.player;
        if (player != null)
        {
            save.PlayerX = player.transform.position.X;
            save.PlayerY = player.transform.position.Y;
            // TODO: add inventory DTO serialization when available
        }

        // gather per-chunk modifiedTiles into save.Chunks
        foreach (var kv in WorldGeneration.Instance.chunkMap)
        {
            var chunkIndex = kv.Key; // (int,int)
            var chunk = kv.Value;
            if (chunk == null) continue;

            // convert chunk.modifiedTiles to list of DTO entries
            var entries = new List<ChunkLoadedEntryDTO>();
            foreach (var mod in chunk.modifiedTiles)
            {
                entries.Add(new ChunkLoadedEntryDTO
                {
                    X = mod.Key.x,
                    Y = mod.Key.y,
                    Data = mod.Value
                });
            }

            if (entries.Count > 0)
            {
                // store using "cx_cy" chunk coords (matches loader)
                string chunkKey = $"{chunkIndex.Item1}_{chunkIndex.Item2}";
                save.Chunks[chunkKey] = entries;
            }
        }

        // write save file
        var savePath = Path.Combine(GamesFolder, $"{saveId}.json");
        var options = new JsonSerializerOptions { WriteIndented = true };
        File.WriteAllText(savePath, JsonSerializer.Serialize(save, options));

        // update index
        var index = GetSaves();
        var existing = index.Find(x => x.Id == saveId);
        if (existing != null)
        {
            existing.Name = saveName;
            existing.Timestamp = save.Timestamp;
        }
        else
        {
            index.Add(new SaveIndexEntry { Id = saveId, Name = saveName, Timestamp = save.Timestamp, FilePath = savePath });
        }
        WriteIndex(index);
    }

    // Load a save by id. This will write per-chunk files into saves/chunks so the existing chunk loading
    // logic (Chunk.LoadFromDisk) picks up saved modifiedTiles. It does NOT immediately spawn the world.
    public static bool LoadSaveIntoChunkFiles(string saveId)
    {
        var savePath = Path.Combine(GamesFolder, $"{saveId}.json");
        if (!File.Exists(savePath)) return false;

        try
        {
            var json = File.ReadAllText(savePath);
            var save = JsonSerializer.Deserialize<SaveData>(json);
            if (save == null) return false;

            // clear existing chunk files in saves/chunks (so no leftover from other saves)
            foreach (var f in Directory.GetFiles(ChunksFolder, "chunk_*.json"))
            {
                try { File.Delete(f); } catch { }
            }

            // write each saved chunk into saves/chunks as chunk_{startX}_{startY}.json
            foreach (var chunkPair in save.Chunks)
            {
                var keyParts = chunkPair.Key.Split('_');
                if (keyParts.Length != 2) continue;
                if (!int.TryParse(keyParts[0], out int cx)) continue;
                if (!int.TryParse(keyParts[1], out int cy)) continue;

                // chunk world position in world units (match how Chunk.SaveToDisk names files)
                int chunkStartX = cx * WorldGeneration.Instance.chunkSize * Core.UNIT_SIZE;
                int chunkStartY = cy * WorldGeneration.Instance.chunkSize * Core.UNIT_SIZE;
                var entries = chunkPair.Value;

                // build the array shape expected by Chunk.LoadFromDisk (list of {X,Y,Data})
                var outList = new List<object>();
                foreach (var e in entries)
                {
                    outList.Add(new
                    {
                        X = e.X,
                        Y = e.Y,
                        Data = e.Data
                    });
                }

                string fileName = $"chunk_{chunkStartX}_{chunkStartY}.json";
                string path = Path.Combine(ChunksFolder, fileName);
                File.WriteAllText(path, JsonSerializer.Serialize(outList, new JsonSerializerOptions { WriteIndented = true }));
            }

            CurrentLoadedSaveId = saveId;
            // disable per-chunk auto-load from disk if we want to rely on this single-file load mode
            UsePerChunkSaves = true; // keep true so chunks pick these files up; set false if you want different behavior

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"LoadSaveIntoChunkFiles failed: {ex.Message}");
            return false;
        }
    }

    // Delete save
    public static bool DeleteSave(string saveId)
    {
        var index = GetSaves();
        var entry = index.Find(x => x.Id == saveId);
        if (entry == null) return false;

        try
        {
            var path = Path.Combine(GamesFolder, $"{saveId}.json");
            if (File.Exists(path)) File.Delete(path);
            index.RemoveAll(x => x.Id == saveId);
            WriteIndex(index);
            return true;
        }
        catch
        {
            return false;
        }
    }

    // Rename save
    public static bool RenameSave(string saveId, string newName)
    {
        var index = GetSaves();
        var entry = index.Find(x => x.Id == saveId);
        if (entry == null) return false;
        entry.Name = newName;
        WriteIndex(index);
        return true;
    }

    // Utility: Start a fresh game (do not apply previous per-chunk files)
    public static void StartNewGame(bool clearChunkFiles = false)
    {
        UsePerChunkSaves = false;
        CurrentLoadedSaveId = null;
        if (clearChunkFiles)
        {
            foreach (var f in Directory.GetFiles(ChunksFolder, "chunk_*.json"))
            {
                try { File.Delete(f); } catch { }
            }
        }
    }
}