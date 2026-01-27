public class Particle : GameObject
{
    public bool isActive;
    public float lifetime;
    public Vector2 velocity;
    public float age;
    public Color particleColor = new Color(255, 255, 255, 0);
    public Color lightColor = Color.White;
    private float size;
    public Lightsource? lightSource;
    private int initialBrightness;
    private Texture2D texture;
    private float rot = 0;
    private int rotSpeed;

    public void Init(Vector2 position, Vector2 velocity, float lifetime, float size, int brightness)
    {
        transform.position = position;
        AddComponent<Lightsource>();
        lightSource = GetComponent<Lightsource>();
        lightSource.light = new Light(position, brightness, lightColor);

        this.velocity = velocity;
        this.lifetime = lifetime;
        initialBrightness = brightness;
        age = 0;
        isActive = true;
        this.size = size;
        texture = TextureManager.LoadTexture("Textures/particle.png");
        rotSpeed = Random.Shared.Next(-10, 10);
    }

    public override void Update()
    {
        if (!isActive) return;
        age += Raylib.GetFrameTime();
        float ageNormalized = age / lifetime;
        size -= ageNormalized;

        float brightness = Raymath.Lerp(initialBrightness, 0, ageNormalized);
        lightSource.light.SetBrightness((int)brightness);

        if (age >= lifetime || size <= 0)
        {
            isActive = false;
            ParticlePool.Recycle(this);
            return;
        }
        transform.position += velocity * Raylib.GetFrameTime();
    }

    public override void Draw()
    {
        if (!isActive) return;
        rot += rotSpeed * Raylib.GetFrameTime();
        // Raylib.DrawCircleGradient((int)(transform.position.X + size / 2), (int)(transform.position.Y + size / 2), size, particleColor, Color.Blank);
        // Raylib.DrawRectangle((int)transform.position.X, (int)transform.position.Y, (int)size, (int)size, particleColor);
        // Raylib.DrawCircle((int)transform.position.X, (int)transform.position.Y, size, particleColor);
        Raylib.DrawTexturePro(texture,
        new Raylib_cs.Rectangle(0, 0, texture.Width, texture.Height),
        new Raylib_cs.Rectangle(transform.position.X, transform.position.Y, size * 2, size * 2),
        new Vector2(size * 2 / 2, size * 2 / 2), rot, particleColor);

    }

    public float GetSize()
    {
        return size;
    }
}