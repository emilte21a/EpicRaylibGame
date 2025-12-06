using System.Text.Json;

public static class ItemFactory
{
    private static Dictionary<short, ItemData> itemDataDict;

    public static void LoadItems(string jsonPath)
    {
        using StreamReader reader = new(jsonPath);
        var json = reader.ReadToEnd();
        var items = JsonSerializer.Deserialize<List<ItemData>>(json);
        itemDataDict = items.ToDictionary(i => i.id, i => i);
    }

    public static DroppedItem CreateDroppedItem(short id, Vector2 position)
    {
        Item item = CreateItem(id);
        return new DroppedItem(item, position);
    }

    public static Tile CreateTileFromItem(Item item, Vector2 position)
    {
        if (!itemDataDict.TryGetValue(item.ID, out var data))
            throw new Exception($"Item id {item.ID} not found!");

        Tile tile = data.tileType switch
        {
            "GrassTile" => new GrassTile(),
            "StoneTile" => new StoneTile(),
            "DirtTile" => new DirtTile(),
            "Torch" => new Torch(),
            "CopperOreTile" => new CopperOreTile(),
            "Furnace" => new FurnaceTile(),
            "TreeTile" => new TreeTile(),
            "SilverOreTile" => new SilverOreTile(),
            "CoalOreTile" => new CoalOreTile(),
            "CraftingTable" => new CraftingTableTile(),
            _ => throw new Exception($"Unknown tile type: {data.tileType}")
        };
        tile.transform.position = position;
        tile.dropItemId = item.ID;
        return tile;
    }

    public static Item CreateItem(short id)
    {
        if (!itemDataDict.TryGetValue(id, out var data))
            throw new Exception($"Item id {id} not found!");

        var recipe = new Dictionary<Item, int>();
        if (data.recipe != null)
        {
            foreach (var kvp in data.recipe)
            {
                var ingredientItem = CreateItem(kvp.Key);
                recipe[ingredientItem] = kvp.Value;
            }
        }

        var texture = TextureManager.LoadTexture(data.texturePath);

        Item item;

        switch (data.itemType?.ToLowerInvariant())
        {
            case "tool":
                item = CreateTool(data, texture, recipe);
                break;
            default:
                item = new Item();
                break;
        }

        item.ID = data.id;
        item.name = data.name;
        item.description = data.description;
        item.recipe = recipe;
        item.texture = texture;
        item.placeable = data.placeable;

        return item;
    }

    public static Dictionary<short, int> GetRecipeFromItemID(short itemID)
    {
        if (itemDataDict == null)
            throw new Exception("Item data not loaded.");

        if (itemDataDict.TryGetValue(itemID, out var data) && data.recipe != null)
            return new Dictionary<short, int>(data.recipe);

        return new Dictionary<short, int>();
    }

    public static ItemData? GetitemFromItemID(short itemID)
    {
        if (itemDataDict == null)
            return null;

        if (itemDataDict.TryGetValue(itemID, out var data))
            return data;

        else
            return null;
    }

    private static Tool CreateTool(ItemData data, Texture2D texture, Dictionary<Item, int> recipe)
    {
        float damage = data.damage ?? 0;
        float speed = data.speed ?? 1.0f;

        Tool tool = data.toolType?.ToLowerInvariant() switch
        {
            "silverpickaxe" => new SilverPickaxe(damage, speed),
            _ => throw new Exception($"Unknown tool type: {data.toolType}")
        };

        tool.ID = data.id;
        tool.name = data.name;
        tool.description = data.description;
        tool.recipe = recipe;
        tool.texture = texture;
        tool.placeable = data.placeable;

        return tool;
    }

    public enum ItemID
    {
        grass = 1,
        stone = 2,
        dirt = 3,
        torch = 4,
        copperore = 5,
        furnace = 6,
        wood = 7,
        sapling = 8,
        silverore = 9,
        silverPickaxe = 10,
        coalore = 11,
        copperingot = 12,
        silverIngot = 13,
        craftingtable = 14
    }
}