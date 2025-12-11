public static class ParticlePool
{
    private static readonly Stack<Particle> inactive = new();
    public static readonly List<Particle> ActiveParticles = new();

    public static Particle GetParticle()
    {
        if (inactive.Count > 0)
            return inactive.Pop();

        var p = new Particle();
        return p;
    }

    public static void Recycle(Particle p)
    {
        p.isActive = false;
        ActiveParticles.Remove(p);
        inactive.Push(p);
    }

    public static void EmitParticles(int amount, Vector2 velocity, Color color, float lifeTime, int size, int brightness, Vector2 position, Vector2 offset, bool randomVelocity)
    {
        for (int i = 0; i < amount; i++)
        {
            var p = GetParticle();
            p.particleColor = color;

            var v = velocity + (randomVelocity ? new(Random.Shared.Next(-10, 10), Random.Shared.Next(-10, 10)) : Vector2.Zero);
            p.Init(position + offset, v, lifeTime, size, brightness);

            if (!ActiveParticles.Contains(p))
                ActiveParticles.Add(p);
        }
    }
}