using System.Dynamic;
using System.Text.Json.Serialization;

public class ItemData
{
    public short id { get; set; }
    public string name { get; set; }
    public string texturePath { get; set; }
    public string tileType { get; set; }
    public Dictionary<short, int> recipe { get; set; }
    public string description { get; set; }
    public bool placeable { get; set; }
    [JsonPropertyName("type")]
    public string? itemType { get; set; }
    public float? damage { get; set; }
    public float? speed { get; set; }
    public string? toolType { get; set; }
}