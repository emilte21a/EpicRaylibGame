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

    public static void EmitParticles(int amount, Vector2 velocity, Color color, float lifeTime, int size, int brightness, Vector2 position, Vector2 offset)
    {
        for (int i = 0; i < amount; i++)
        {
            var p = GetParticle();
            p.particleColor = color;

            // give each particle a slightly different velocity
            var v = velocity + new Vector2(Random.Shared.Next(-20, 20), Random.Shared.Next(-20, 20));
            p.Init(position + offset, v, lifeTime, size, brightness);

            // ensure the pool's active list contains the particle (some pool impls forget to add it)
            if (!ActiveParticles.Contains(p))
                ActiveParticles.Add(p);
        }

        // Console.WriteLine($"EmitParticles: requested={amount}, activeNow={ActiveParticles.Count}");
    }
}