using System.Numerics;

public class ParallaxLayer : GameObject
{
    readonly Renderer? renderer;
    public Rectangle rectangle;
    public float factor;

    public ParallaxLayer(Texture2D texture, float factor)
    {
        AddComponent<Renderer>();
        renderer = GetComponent<Renderer>();
        renderer.sprite = texture;
        rectangle = new Rectangle(0, 0, renderer.sprite.Width, renderer.sprite.Height);
        this.factor = factor;
    }
}

public static class ParallaxHandler
{

    private static List<ParallaxLayer> parallaxLayers = new List<ParallaxLayer>
    {
        new(TextureManager.LoadTexture("Textures/parallaxlayer2.png"), 35f),
        new(TextureManager.LoadTexture("Textures/parallaxlayer1.png"), 75f)
    };

    public static void Draw()
    {
        var cam = CameraSystem.Instance.GetPixelPerfectCamera();
        float cameraX = cam.Target.X;

        foreach (var p in parallaxLayers)
        {
            p.rectangle.X = cameraX * p.factor / 500;
            p.rectangle.Y = (int)Raymath.Lerp(p.transform.position.Y, 300 - (cam.Target.Y / Core.UNIT_SIZE / Core.CHUNK_SIZE), p.factor);
            Raylib.DrawTexturePro(p.GetComponent<Renderer>().sprite, p.rectangle,
            new Raylib_cs.Rectangle(0, 0, CameraSystem.Instance.pixelPerfectTargetTexture.Texture.Width, CameraSystem.Instance.pixelPerfectTargetTexture.Texture.Height), Vector2.Zero, 0, Color.White);
        }
    }

    public static void DrawOcclusionMask()
    {
        // Raylib.BeginBlendMode(Raylib_cs.BlendMode.Alpha);
        // var cam = CameraSystem.Instance.GetPixelPerfectCamera();
        // float cameraX = cam.Target.X;
        // foreach (var p in parallaxLayers)
        // {
        //     p.transform.position = new Vector2(0, Raymath.Lerp(p.transform.position.Y, 0, Raylib.GetFrameTime() * p.factor));
        //     p.rectangle.X = cameraX * p.factor / 500;
        //     Raylib.DrawTextureRec(p.GetComponent<Renderer>().sprite, p.rectangle, p.transform.position, Color.White);
        // }
        // Raylib.EndBlendMode();
    }
}