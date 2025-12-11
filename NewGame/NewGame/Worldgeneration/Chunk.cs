using System;
using System.Text.Json;
using System.IO;
using SharpNoise.Modules;
using LibNoise.Filter;
using LibNoise;

public class Chunk
{
    public Vector2 position;
    private int chunkSeed;
    public Dictionary<(int, int), Tile> tileMap = new Dictionary<(int, int), Tile>();
    public Dictionary<(int, int), Foliage> foliageMap = new Dictionary<(int, int), Foliage>();
    public Dictionary<(int, int), TreeTile> treeMap = new Dictionary<(int, int), TreeTile>();
    public Dictionary<(int, int), BackgroundTile> backgroundTileMap = new Dictionary<(int, int), BackgroundTile>();

    // map of modified tiles per local tile index; store tile id + optional component state JSON
    public Dictionary<(int x, int y), ModifiedTileDTO> modifiedTiles = new Dictionary<(int, int), ModifiedTileDTO>();

    // DTO for a saved tile entry
    public class ModifiedTileDTO
    {
        public short TileId { get; set; }
        public string? ComponentType { get; set; }   // e.g. "FurnaceComponent"
        public string? ComponentJson { get; set; }   // JSON payload for the component
    }

    public void MarkTileModified((int x, int y) tileIndex, Tile tile)
    {
        var dto = new ModifiedTileDTO
        {
            TileId = tile.tileId
        };


        Console.WriteLine($"Saving tile at {tileIndex} with ID: {tile.tileId} (Type: {tile.GetType().Name})");

        // if tile has a FurnaceComponent, serialize its state
        var furnace = tile.GetComponent<FurnaceComponent>();
        if (furnace != null)
        {
            dto.ComponentType = "FurnaceComponent";
            dto.ComponentJson = JsonSerializer.Serialize(furnace.ToDTO());
        }

        // add other component serializers here as needed (e.g. CraftingTableComponent)

        modifiedTiles[tileIndex] = dto;
    }

    public void MarkTileRemoved((int, int) tileKey)
    {
        // mark removed tile and ensure foliage removed on load
        modifiedTiles[tileKey] = new ModifiedTileDTO { TileId = -1 };
    }

    public void Unload()
    {
        foreach (var t in tileMap.Values)
        {
            t.shouldBeDestroyed = true;
            CollisionSystem.Instance.staticSpatialHash.Remove(t);
        }

        foreach (var f in foliageMap.Values)
        {
            f.shouldBeDestroyed = true;
            CollisionSystem.Instance.staticSpatialHash.Remove(f);
        }

        foreach (var t in treeMap.Values)
        {
            t.shouldBeDestroyed = true;
            CollisionSystem.Instance.staticSpatialHash.Remove(t);
        }

        tileMap.Clear();
        foliageMap.Clear();
        treeMap.Clear();
        backgroundTileMap.Clear();
    }

    public void ApplyModifications()
    {
        foreach (var kvp in modifiedTiles)
        {
            var index = (kvp.Key.x, kvp.Key.y);
            var dto = kvp.Value;

            if (dto.TileId == -1)
            {
                // Remove tile that should be removed
                if (tileMap.TryGetValue(index, out var t))
                    Game.RemoveGameObject(t);

                tileMap.Remove(index);
                backgroundTileMap.Remove(index);
                foliageMap.Remove(index);
                treeMap.Remove(index);
                Console.WriteLine($"Saving tile at {index} with ID: {t.tileId} (Type: {t.GetType().Name})");
                continue;
            }

            Tile tile = TileFactory.CreateTileFromID(dto.TileId, index);
            if (tile == null)
                Console.WriteLine("Tile is null and is not changed");


            Vector2 pos = new(index.x * Core.UNIT_SIZE, index.y * Core.UNIT_SIZE);
            tile.transform.position = pos;

            tileMap[index] = tile;

            if (tile is MultiTilePart)
                continue;

            if (tile is MultiTile mt)
                mt.OnPlaced((int)mt.transform.position.X, (int)mt.transform.position.Y);



            // restore component state
            if (dto.ComponentType == "FurnaceComponent")
            {
                var comp = tile.GetComponent<FurnaceComponent>();
                comp.FromDTO(JsonSerializer.Deserialize<FurnaceComponent.FurnaceStateDTO>(dto.ComponentJson));
            }

            // then update colliders + spatial hash
            tile.GetComponent<Collider>()?.UpdateBounds(tile.transform.position);
            CollisionSystem.Instance.staticSpatialHash.Insert(tile);
        }
    }

    private class LoadedEntry
    {
        public int X { get; set; }
        public int Y { get; set; }
        public ModifiedTileDTO Data { get; set; } = new ModifiedTileDTO();
    }

    public float leftLimit = 0;
    public float rightLimit = 0;
    public float upperLimit = 0;
    public float lowerLimit = 0;

    private const float CAVE_MIN_THRESHOLD = 0.05f;
    private const float CAVE_MAX_THRESHOLD = 0.25f;
    private const float CAVE_TUNNEL_THRESHOLD_MAX = 0.42f;

    private const float SURFACE_NOISE_SCALE = 10;
    private const int SURFACE_OFFSET = 30;

    Perlin surfaceNoise = new Perlin();
    Perlin caveNoise = new Perlin();
    Perlin caveNoiseOctave = new Perlin();
    int chunkSize = Core.CHUNK_SIZE;
    int chunkHeight;

    private static readonly OreType[] ores =
    [
        new OreType(typeof(CoalOreTile),   0.95f, 1f),
        new OreType(typeof(CopperOreTile), 0.98f, 0.9f),
        new OreType(typeof(SilverOreTile), 0.99f, 0.8f)
    ];

    private Perlin[] oreNoises;

    public Chunk(Vector2 position, int seed)
    {
        this.position = position;
        chunkSeed = seed;
        leftLimit = position.X;
        rightLimit = position.X + chunkSize * Core.UNIT_SIZE;
        upperLimit = position.Y;
        lowerLimit = position.Y + chunkSize * Core.UNIT_SIZE;

        chunkHeight = chunkSize;

        surfaceNoise.Frequency = 2f;
        surfaceNoise.Seed = seed;
        caveNoise.Frequency = 3f;
        caveNoise.Seed = seed;
        caveNoiseOctave.Frequency = 4.5f;
        caveNoiseOctave.Seed = seed;

        oreNoises = new Perlin[ores.Length];
        for (int i = 0; i < ores.Length; i++)
        {
            oreNoises[i] = new Perlin();
            oreNoises[i].Seed = seed + i * 100;
            oreNoises[i].Frequency = 2f;
        }

        GenerateChunk();
        ApplyModifications();
    }

    void GenerateChunk()
    {
        for (int x = 0; x < chunkSize; x++)
        {
            int worldTileX = (int)((position.X / Core.UNIT_SIZE) + x);
            double nx = worldTileX * 0.01;

            int rngSeed = chunkSeed ^ (worldTileX * 73856093);
            var rng = new Random(rngSeed);

            int surfaceAmplitude = chunkSize * 2;
            float noiseVal = (float)surfaceNoise.GetValue(nx, 0, 0);
            float norm = (noiseVal + 1f) * 1f;
            int surfaceWorldY = SURFACE_OFFSET + (int)(norm * surfaceAmplitude);

            for (int localY = 0; localY < chunkSize; localY++)
            {
                int worldTileY = (int)((position.Y / Core.UNIT_SIZE) + localY);

                if (worldTileY < surfaceWorldY)
                    continue;

                var index = ((int)((position.X + x * Core.UNIT_SIZE) / Core.UNIT_SIZE), worldTileY);
                if (tileMap.ContainsKey(index)) continue;

                double ny = worldTileY * 0.01;
                float depthNormalized = Math.Clamp((worldTileY - surfaceWorldY) / (float)(chunkHeight - surfaceWorldY), 0f, 1f);

                const float caveBaseChance = 1.1f;
                const float caveMaxChance = 1.7f;
                float caveChance = Raymath.Lerp(caveBaseChance, caveMaxChance, depthNormalized / 2);
                double caveValue = (caveNoise.GetValue(nx, ny, 0) + 1.0) * 0.5 / caveChance;

                double caveTunnelNoise = (caveNoiseOctave.GetValue(nx, ny, 0) + 1.0) * 0.5d;

                if (caveValue > CAVE_MIN_THRESHOLD && caveValue < CAVE_MAX_THRESHOLD)
                    PlaceBackgroundTile(x, localY, index, surfaceWorldY);
                else if (caveTunnelNoise > CAVE_MAX_THRESHOLD && caveTunnelNoise < CAVE_TUNNEL_THRESHOLD_MAX)
                    PlaceBackgroundTile(x, localY, index, surfaceWorldY);
                else
                    PlaceSolidTile(x, localY, index, surfaceWorldY, caveValue, rng);
            }

            // Try to place a tree once per column if the surface tile ended up inside this chunk
            int localSurfaceY = surfaceWorldY - (int)(position.Y / Core.UNIT_SIZE);
            if (localSurfaceY >= 0 && localSurfaceY < chunkSize)
            {
                var surfaceIndex = ((int)((position.X + x * Core.UNIT_SIZE) / Core.UNIT_SIZE), surfaceWorldY);
                if (tileMap.TryGetValue(surfaceIndex, out var maybeGrass) && maybeGrass is GrassTile)
                {
                    // use the same rng seeded per-column
                    if (rng.Next(0, 10) > 6)
                    {
                        var tree = new TreeTile();
                        int originX = (int)((position.X / Core.UNIT_SIZE) + x);
                        int originY = surfaceWorldY - tree.heightInTiles; // top-left origin so base sits on surface
                        if (originY >= 0 && CanPlaceArea(originX, originY, tree.widthInTiles, tree.heightInTiles))
                        {
                            PlaceMultiTile(tree, originX, originY);
                        }
                    }
                }
            }
        }
    }

    // update PlaceSolidTile to accept and forward rng to foliage placement
    void PlaceSolidTile(int x, int y, (int, int) index, int surfaceWorldY, double noiseValue, Random rng)
    {

        // compute the world Y for this local y so decisions use global coordinates
        int worldY = (int)((position.Y / Core.UNIT_SIZE) + y);
        Tile tile;

        // surface tile (grass) when the worldY equals surfaceWorldY
        if (worldY == surfaceWorldY)
        {
            tile = new GrassTile();

            PlaceFoliage(x, y, index, rng); // deterministic foliage
        }
        else if (worldY > surfaceWorldY && worldY <= surfaceWorldY + 5)
        {

            tile = (noiseValue <= 0.2f) ? new StoneTile() : new DirtTile();
        }
        else
        {
            int worldTileX = (int)((position.X / Core.UNIT_SIZE) + x);
            int worldTileY = (int)((position.Y / Core.UNIT_SIZE) + y);

            float depthNormalized = Math.Clamp((worldTileY - surfaceWorldY) / (float)(chunkHeight - surfaceWorldY), 0f, 1f);

            tile = PlaceOresOrOther(worldTileX, worldTileY, depthNormalized);
        }

        // set transform using local y and chunk position (keeps tile inside this chunk)
        tile.transform.position = new Vector2(x * Core.UNIT_SIZE + position.X, y * Core.UNIT_SIZE + position.Y);
        tileMap[index] = tile;
    }

    void PlaceBackgroundTile(int x, int y, (int, int) index, int surfaceWorldY)
    {
        Tile? tile = null;

        int worldY = (int)((position.Y / Core.UNIT_SIZE) + y);

        if (worldY > surfaceWorldY)
            tile = new BackgroundTile();

        if (tile != null)
        {
            tile.transform.position = new Vector2(x * Core.UNIT_SIZE + position.X, y * Core.UNIT_SIZE + position.Y);
            backgroundTileMap[index] = (BackgroundTile)tile;
        }
    }

    public void PlaceTrees(int chunkWidth)
    {
        int minTreeSpacing = 2;
        int flatnessCheckRange = 2;

        HashSet<(int, int)> usedPositions = new();

        var groundTile = tileMap.Values.FirstOrDefault(t =>
             t is GrassTile);

        if (groundTile != null && Random.Shared.Next(0, 10) > 6)
        {
            var gx = (int)(groundTile.transform.position.X / Core.UNIT_SIZE);
            var gy = (int)(groundTile.transform.position.Y / Core.UNIT_SIZE);

            var tree = new TreeTile();
            int originX = gx;
            int originY = gy - tree.heightInTiles;

            if (originY >= 0 && CanPlaceArea(originX, originY, tree.widthInTiles, tree.heightInTiles) && originX % minTreeSpacing == 0)
                PlaceMultiTile(tree, originX, originY);
        }
    }

    public void PlaceMultiTile(MultiTile parent, int originTileX, int originTileY)
    {
        parent.OnPlaced(originTileX, originTileY);

        // Set parent's transform snapped to tile grid
        parent.transform.position = new Vector2(originTileX * Core.UNIT_SIZE, originTileY * Core.UNIT_SIZE);

        // store parent at origin in tile map
        tileMap[(originTileX, originTileY)] = parent;

        // If it's a tree, keep the treeMap entry (only for tree behaviour)
        if (parent is TreeTile tree)
        {
            treeMap[(originTileX, originTileY)] = tree;
        }

        // Place parts and set their transforms
        for (int xx = 0; xx < parent.widthInTiles; xx++)
        {
            for (int yy = 0; yy < parent.heightInTiles; yy++)
            {
                var coord = (originTileX + xx, originTileY - yy); // matches your storage convention
                if (xx == 0 && yy == 0)
                {
                    // parent already placed at origin
                    continue;
                }

                var part = new MultiTilePart(parent, xx, yy);
                // Snap part transform to the correct tile position
                part.transform.position = new Vector2(coord.Item1 * Core.UNIT_SIZE, coord.Item2 * Core.UNIT_SIZE);

                // optionally set collider for part if needed (or leave off to avoid double-colliders)
                var pcol = part.GetComponentFast<Collider>();
                if (pcol != null)
                    pcol.boxCollider = new Rectangle(part.transform.position.X, part.transform.position.Y, Core.UNIT_SIZE, Core.UNIT_SIZE);

                tileMap[coord] = part;
            }
        }

        parent.Start();

        var parentCol = parent.GetComponentFast<Collider>();
        if (parentCol != null)
        {
            parentCol.boxCollider = new Rectangle(parent.transform.position.X, parent.transform.position.Y,
                                                 Core.UNIT_SIZE * parent.widthInTiles,
                                                 Core.UNIT_SIZE * parent.heightInTiles);

            // Insert the parent into spatial hash AFTER collider is set
            CollisionSystem.Instance.staticSpatialHash.Insert(parent);
        }
    }

    public bool CanPlaceArea(int tileX, int tileY, int widthTiles, int heightTiles)
    {
        for (int yy = 0; yy < heightTiles; yy++)
            for (int xx = 0; xx < widthTiles; xx++)
            {
                var key = (tileX + xx, tileY + yy);
                if (tileMap.TryGetValue(key, out var existing))
                {
                    // Allow placement if the existing tile is replaceable (grass, background or foliage placeholder)
                    // Disallow if it's a multi-tile/part or other solid content
                    if (existing is MultiTilePart) return false;
                    if (existing is MultiTile) return false;

                    if (existing is not BackgroundTile) return false;
                    if (existing is BackgroundTile) continue;

                    var col = existing.GetComponentFast<Collider>();
                    if (col != null && col.isActive) return false;

                    return false;
                }
            }
        return true;
    }

    public void RemoveTileAt((int x, int y) tileIndex)
    {
        if (!tileMap.TryGetValue(tileIndex, out var tile))
            return;

        MultiTile parent = null;

        if (tile is MultiTile multi)
            parent = multi;
        else if (tile is MultiTilePart part)
            parent = part.parent;

        if (parent != null)
        {
            RemoveMultiTile(parent);

            if (parent is TreeTile tree)
            {
                treeMap.Remove((tree.originTileX, tree.originTileY));
            }

            modifiedTiles[tileIndex] = new ModifiedTileDTO { TileId = -1 };
            return;
        }

        if (foliageMap.TryGetValue(tileIndex, out Foliage? value))
        {
            Game.RemoveGameObject(value);
            foliageMap.Remove(tileIndex);
        }

        Game.RemoveGameObject(tile);
        tileMap.Remove(tileIndex);

        // mark removed tile so on reload it stays removed
        modifiedTiles[tileIndex] = new ModifiedTileDTO { TileId = -1 };
    }

    void RemoveMultiTile(MultiTile parent)
    {
        for (int yy = 0; yy < parent.heightInTiles; yy++)
            for (int xx = 0; xx < parent.widthInTiles; xx++)
            {
                var key = (parent.originTileX + xx, parent.originTileY - yy);
                tileMap.Remove(key);
                foliageMap.Remove(key);
            }

        Game.RemoveGameObject(parent);
    }

    public int GetChunkHeight()
    {
        return chunkHeight;
    }

    public int GetChunkWidth()
    {
        return chunkSize;
    }

    private void PlaceFoliage(int x, int y, (int, int) index, Random rng)
    {
        // deterministic check using provided rng
        if (rng.NextDouble() > 0.6 &&
            !foliageMap.Keys.Any(k => k.Item1 == index.Item1))
        {
            var foliage = new Foliage(
                (int)(x * Core.UNIT_SIZE + position.X),
                (int)(y * Core.UNIT_SIZE - Core.UNIT_SIZE + position.Y));
            foliageMap[index] = foliage;
        }
    }

    private Tile PlaceOresOrOther(int worldTileX, int worldTileY, float depthNormalized)
    {
        for (int i = 0; i < ores.Length; i++)
        {
            var ore = ores[i];
            double noise = oreNoises[i].GetValue(worldTileX * 0.1, worldTileY * 0.1, 0);
            float normalizedNoise = (float)((noise + 1) * 0.5);

            float chance = Raymath.Lerp(ore.BaseChance, ore.DeepChance, depthNormalized);

            if (normalizedNoise > chance)
                return (Tile)Activator.CreateInstance(ore.TileType)!;
        }

        return new StoneTile();
    }
}