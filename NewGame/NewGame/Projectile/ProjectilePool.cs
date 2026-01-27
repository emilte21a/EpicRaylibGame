public static class ProjectilePool
{
    private static readonly Stack<Projectile> inactive = new();
    public static readonly List<Projectile> ActiveProjectiles = new();

    public static Projectile GetProjectile()
    {
        if (inactive.Count > 0)
            return inactive.Pop();

        var p = new Projectile();
        return p;
    }

    public static T GetProjectile<T>(T projectile) where T : Projectile
    {
        if (inactive.Count > 0)
        {
            return (T)inactive.Pop();
        }
        return projectile;
    }

    public static void Recycle(Projectile p)
    {
        p.isActive = false;
        ActiveProjectiles.Remove(p);
        inactive.Push(p);
        try
        {
            // CollisionSystem.Instance.RemoveEntityFromSpatialHash(p);
        }
        catch { }
    }

    public static void EmitProjectile(int amount, Vector2 position, Color particleColor, Color lightColor)
    {
        // for (int i = 0; i < amount; i++)
        // {
        //     var p = GetProjectile();
        //     p.particleColor = particleColor;
        //     p.lightColor = lightColor;

        //     var v = velocity + (randomVelocity ? new(Random.Shared.Next(-10, 10), Random.Shared.Next(-10, 10)) : Vector2.Zero);
        //     p.Init(position + offset, v, lifeTime, size, brightness);

        //     if (!ActiveProjectiles.Contains(p))
        //         ActiveProjectiles.Add(p);
        // }
    }
}