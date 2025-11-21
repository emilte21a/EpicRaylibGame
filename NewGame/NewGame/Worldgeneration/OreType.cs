public record OreType(
    Type TileType,         // Type of tile to spawn
    float BaseChance,      // Chance near surface
    float DeepChance      // Chance deep underground
);