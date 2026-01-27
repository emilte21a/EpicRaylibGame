public class Projectile : Entity
{
    public bool isActive;
    protected float lifetime;
    public float age;
    public Color projectileColor = new Color(255, 255, 255, 0);
    public Color lightColor = Color.White;
    protected float size;
    public Lightsource? lightSource;
    public bool homing = false;
    public bool destroyOnCollision = false;
    public bool friendly = true;

    public void Init(Vector2 position, Vector2 velocity, float lifetime, float size, int brightness, bool friendly)
    {
        transform.position = position;
        AddComponent<Lightsource>();
        lightSource = GetComponent<Lightsource>();
        lightSource.light = new Light(position, brightness, lightColor);

        physicsBody.velocity = velocity;
        this.lifetime = lifetime;
        age = 0;
        isActive = true;
        this.size = size;
        collider.boxCollider = new Rectangle(transform.position.X, transform.position.Y, size, size);
        collider.colliderType = ColliderType.Dynamic;
        collider.isActive = true;
        collider.isTrigger = false;
        collider.interactableByEntities = false;
        this.friendly = friendly;
    }

    public override void Update()
    {
        base.Update();

        if (!isActive) return;

        age += Raylib.GetFrameTime();
        size -= age / lifetime;

        if (age >= lifetime || size <= 0)
        {
            isActive = false;
            MarkForDestruction();
            return;
        }
        if (destroyOnCollision)
        {
            if (collidingWith.Count > 0)
            {
                if (collidingWith.Any(c => !c.isTrigger))
                {
                    isActive = false;
                    MarkForDestruction();
                }
            }
        }
    }

    public override void Draw()
    {
        if (!isActive) return;
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        // ProjectilePool.Recycle(this);
    }

    public float GetSize()
    {
        return size;
    }
}

public class FireBallProjectile : Projectile
{
    static Color[] particleColors = [new Color(255, 117, 0), new Color(240, 127, 19), new Color(215, 53, 2)];

    public override void Draw()
    {
        base.Draw();
        Raylib.DrawCircle((int)transform.position.X, (int)transform.position.Y, size, projectileColor);
        Raylib.DrawCircleLines((int)transform.position.X, (int)transform.position.Y, size, lightColor);
        Raylib.DrawCircleGradient((int)transform.position.X, (int)transform.position.Y, size * 2, lightColor, Color.Blank);
    }

    public override void Update()
    {
        base.Update();
        if ((int)age % 2.0 == 0)
        {
            ParticlePool.EmitParticles(1, -physicsBody.velocity / 10 + new Vector2(Random.Shared.Next(-10, 10), Random.Shared.Next(-10, 10)), particleColors[Random.Shared.Next(0, 3)], lightColor, 10, (int)size, Core.MAX_BRIGHTNESS, transform.position, Vector2.Zero, true);
        }
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        for (int i = 0; i < 15; i++)
        {
            ParticlePool.EmitParticles(1,
            new Vector2(Random.Shared.Next(-10, 10) * 15, Random.Shared.Next(-10, 10) * 15),
            lightColor, lightColor, 50, 3, Core.MAX_BRIGHTNESS, transform.position, Vector2.Zero, true);
        }
    }
}

