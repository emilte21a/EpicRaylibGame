public static class TileFactory
{
    // map id -> concrete Tile Type
    static Dictionary<short, Type> tileTypes = new Dictionary<short, Type>()
    {
        {-1,null}
    };

    // register by passing a prototype instance (keeps existing usage simple)
    public static void AddTileToTileFactory(Tile tile)
    {
        tileTypes[tile.tileId] = tile.GetType();
    }

    // register directly by type/id (optional helper)
    public static void RegisterTileType<T>(short id) where T : Tile
    {
        tileTypes[id] = typeof(T);
    }

    // Create a fresh tile instance for given id
    public static Tile? CreateTileFromID(short ID, (int x, int y) localIndex)
    {
        if (!tileTypes.TryGetValue(ID, out var type))
            return null;

        // Prevent accidental instantiation of MultiTilePart
        if (typeof(MultiTilePart).IsAssignableFrom(type))
            return null;

        var instance = Activator.CreateInstance(type) as Tile;
        if (instance == null)
            return null;

        instance.tileId = ID;
        return instance;
    }

    public enum TileID
    {
        grass = 1,
        dirt = 2,
        stone = 3,
        torch = 4,
        background = 5,
        copper = 6,
        silver = 7,
        coal = 8,
        furnace = 9,
        craftingTable = 10,
        tree = 11
    }
}