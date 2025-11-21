public static class TextureManager
{
    private static readonly Dictionary<string, Texture2D> textures = new();

    public static Texture2D LoadTexture(string path)
    {
        if (!textures.TryGetValue(path, out var tex))
        {
            tex = Raylib.LoadTexture(path);
            textures[path] = tex;
        }
        return tex;
    }
}