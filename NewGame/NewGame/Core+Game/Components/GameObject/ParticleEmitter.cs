using SharpNoise.Modules;

public class ParticleEmitter : Component
{
    private float particleCooldown;
    public float particleSpawnDelay;
    public Color color = Color.White;
    public int particleAmount = 1;
    public int yVelocity = -10;
    public int perlinFrequency = 1;
    public int brightness = Core.MAX_BRIGHTNESS;
    public float lifeTime = 100;
    public int size = 4;

    public override void Start()
    {
        base.Start();
        ResetCooldown();
    }

    public override void Update()
    {
        particleCooldown -= Raylib.GetFrameTime();
        if (!CanEmit())
            return;

        EmitParticles(perlinFrequency, yVelocity, particleAmount);
        ResetCooldown();
    }

    public void EmitParticles(float perlinFrequency, float yVelocity, int particleAmount)
    {
        for (int i = 0; i < particleAmount; i++)
        {
            var perlin = new Perlin();
            perlin.Frequency = perlinFrequency;
            Vector2 velocity = new Vector2(
                (float)perlin.GetValue(i, 0, 0),
                yVelocity
            );

            Vector2 offset = new Vector2(
                parent.GetComponent<Collider>().boxCollider.Width / 2f,
                7
            );

            ParticlePool.EmitParticles(
                1,
                velocity,
                color,
                lifeTime, size,
                brightness,
                parent.transform.position,
                offset
            );
        }
    }

    public bool CanEmit()
    {
        return particleCooldown <= 0;
    }

    public void ResetCooldown()
    {
        particleCooldown = particleSpawnDelay;
    }
}