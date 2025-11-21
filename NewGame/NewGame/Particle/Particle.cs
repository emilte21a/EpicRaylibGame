public class Particle : GameObject
{
    public bool isActive;
    public float lifetime;
    public Vector2 velocity;
    public float age;
    public Color particleColor = new Raylib_cs.Color(255, 255, 255, 0);
    private float size;
    public Lightsource? lightSource;

    public void Init(Vector2 position, Vector2 velocity, float lifetime, float size, int brightness)
    {
        transform.position = position;
        this.velocity = velocity;
        this.lifetime = lifetime;
        age = 0;
        isActive = true;
        this.size = size;
        AddComponent<Lightsource>();
        lightSource = GetComponent<Lightsource>();
        lightSource.light = new Light(position, brightness, particleColor);
    }

    public override void Update()
    {
        if (!isActive) return;
        age += Raylib.GetFrameTime();
        size -= age / 20f;
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
        if (isActive)
        {
            // Raylib.DrawCircleGradient((int)(transform.position.X + size / 2), (int)(transform.position.Y + size / 2), size, particleColor, Color.Blank);
            Raylib.DrawRectangle((int)transform.position.X, (int)transform.position.Y, (int)size, (int)size, particleColor);
        }
    }

    public float GetSize()
    {
        return size;
    }
}