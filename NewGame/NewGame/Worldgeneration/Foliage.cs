public class Foliage : GameObject
{
    private Renderer renderer;

    List<Texture2D> textures = new()
    {
        TextureManager.LoadTexture("Textures/grassfoliage.png"),
        TextureManager.LoadTexture("Textures/stonefoliage.png")
    };

    int randomTextureIndex = 0;

    public Foliage(int x, int y)
    {
        transform.position = new Vector2(x, y);
    }

    public override void Start()
    {
        AddComponent<Renderer>();
        renderer = GetComponent<Renderer>();
        renderer.sprite = textures[Random.Shared.Next(0, textures.Count)];
        textures.Clear();
        randomTextureIndex = Random.Shared.Next(0, 3);
    }

    public override void Draw()
    {
        if (renderer != null && renderer.sprite.Width > 0)
        {
            var dst = new Rectangle(
                transform.position.X + Core.UNIT_SIZE / 2,
                transform.position.Y + Core.UNIT_SIZE / 2,
                Core.UNIT_SIZE,
                Core.UNIT_SIZE
            );



            var origin = new Vector2(dst.Width / 2f, dst.Height / 2f);

            // int cx = (int)(dst.X + origin.X);
            // int cy = (int)(dst.X + origin.Y);

            var src = new Rectangle(renderer.sprite.Width / 3 * randomTextureIndex, 0, renderer.sprite.Width / 3, renderer.sprite.Height);
            Raylib.DrawTexturePro(renderer.sprite, src, dst, origin, transform.zRotation, Color.White);
            // Raylib.DrawRectangleLines((int)(dst.X - origin.X), (int)(dst.Y - origin.Y), (int)dst.Width, (int)dst.Height, Color.Yellow);
            // Raylib.DrawCircle(cx, cy, 3, Color.Red);
            // // draw top-left marker
            // Raylib.DrawCircle((int)dst.X + 2, (int)dst.Y + 2, 2, Color.Blue);
        }
    }
}