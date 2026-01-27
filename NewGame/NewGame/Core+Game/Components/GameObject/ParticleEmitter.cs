using SharpNoise.Modules;

public class ParticleEmitter : Component
{
    private float particleCooldown;
    public float particleSpawnDelay;
    public Color color = Color.White;
    public Color lightColor = Color.White;
    public int particleAmount = 1;
    public Vector2 velocity = new(0, -10);
    public int perlinFrequency = 1;
    public int brightness = Core.MAX_BRIGHTNESS;
    public float lifeTime = 100;
    public int size = 4;
    public Vector2 offset;

    public ParticleEmitter()
    {

    }

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

        EmitParticles(perlinFrequency, velocity, particleAmount, true, offset);
        ResetCooldown();
    }

    public void EmitParticles(float? perlinFrequency, Vector2 velocity, int particleAmount, bool randomVelocity, Vector2 offset)
    {
        for (int i = 0; i < particleAmount; i++)
        {
            var perlin = new Perlin();
            if (perlinFrequency != null)
            {
                perlin.Frequency = (double)perlinFrequency;
                velocity.X += (float)perlin.GetValue(i, 0, 0);
            }


            ParticlePool.EmitParticles(
                1,
                velocity,
                color,
                lightColor,
                lifeTime, size,
                brightness,
                parent.transform.position,
                offset,
                randomVelocity
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