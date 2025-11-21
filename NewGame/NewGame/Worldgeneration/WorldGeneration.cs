public class WorldGeneration
{
    public Dictionary<(int, int), Chunk> chunkMap = new();
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

    public WorldGeneration()
    {
        chunkMap.Clear();
        seed = Random.Shared.Next(-10000, 10000);
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

    void GenerateWorldFrom(Vector2 position)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();

        (int x, int y) chunkIndex = ((int)MathF.Floor(position.X / (chunkSize * Core.UNIT_SIZE)), (int)MathF.Floor(position.Y / (chunkSize * Core.UNIT_SIZE)));
        float chunkStartX = chunkIndex.x * chunkSize * Core.UNIT_SIZE;
        float chunkStartY = chunkIndex.y * chunkSize * Core.UNIT_SIZE;
        if (!chunkMap.ContainsKey(chunkIndex))
        {
            var chunk = new Chunk(new Vector2(chunkStartX, chunkStartY), seed);
            chunkMap[chunkIndex] = chunk;

            while (chunk.position.X < leftLimit)
                leftLimit = chunk.position.X;
            while (chunk.position.X + chunkSize * Core.UNIT_SIZE > rightLimit)
                rightLimit = chunk.position.X + chunkSize * Core.UNIT_SIZE;

            while (chunk.position.Y < upperLimit)
                upperLimit = chunk.position.Y;
            while (chunk.position.Y + chunkSize * Core.UNIT_SIZE > lowerLimit)
                lowerLimit = chunk.position.Y + chunkSize * Core.UNIT_SIZE;
        }

        sw.Stop();
        Console.WriteLine($"Chunk {chunkIndex} generated in {sw.ElapsedMilliseconds} ms");
    }

    public void Update()
    {
        int chunkWidthUnits = chunkSize * Core.UNIT_SIZE;
        int chunkHeightUnits = chunkSize * Core.UNIT_SIZE;
        (int, int) playerChunkIndex = GetChunkIndexAtPosition(playerRef.transform.position);

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

    }

    public Tile? GetTileAt(Vector2 worldPos)
    {
        (int, int) chunkIndex = GetChunkIndexAtPosition(worldPos);
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
        (int, int) chunkIndex = GetChunkIndexAtPosition(new Vector2(pos.Item1, pos.Item2));

        if (!chunkMap.TryGetValue(chunkIndex, out var chunk)) return null;

        if (!chunk.tileMap.TryGetValue(tileCoordinates, out var tile)) return null;

        if (tile is MultiTilePart part) return part.parent;
        return tile;
    }

    public (int, int) GetChunkIndexAtPosition(Vector2 position)
    {
        var chunkIndex = ((int)MathF.Floor(position.X / (chunkSize * Core.UNIT_SIZE)), (int)MathF.Floor(position.Y / (chunkSize * Core.UNIT_SIZE)));
        return chunkIndex;
    }

    public (int, int) GetTileIndexAtPosition(Vector2 position)
    {
        var tileIndex = ((int)MathF.Floor(position.X / Core.UNIT_SIZE), (int)MathF.Floor(position.Y / Core.UNIT_SIZE));
        return tileIndex;
    }

 
}


