public class Item
{
    public short ID;
    public string name;
    public Texture2D texture;
    public Dictionary<Item, int> recipe;
    public string description;
    public bool placeable;
    public string itemType;
    public float damage;
    public float speed;
    public string toolType;
}

public enum ToolType
{
    pickaxe,
    sword,
    staff
}

public abstract class Tool : Item
{
    public ToolType toolVariant;
    public bool isUsing = false;
    protected float useTimer = 0f;
    protected float useDuration = 0.15f;
    protected float recoveryDuration = 0.3f;
    protected bool isRecovering = false;
    protected float recoveryTimer = 0f;

    public int useDirection = 1; // 1 = right, -1 = left
    public float rotation = 0;
    public float xSkew = 0;
    public float ySkew = 0;

    public virtual void OnUse()
    {
        if (!isUsing)
        {
            isUsing = true;
            useTimer = 0f;
        }
    }

    public virtual void Update()
    {

    }

    // --- Easing functions ---
    protected float EaseInOutSine(float t)
    {
        return -(MathF.Cos(MathF.PI * t) - 1f) / 2f;
    }

    protected float EaseOutCubic(float t)
    {
        return 1 - MathF.Pow(1 - t, 3);
    }
}

public abstract class Pickaxe : Tool
{
    public override void Update()
    {
        float dt = Raylib.GetFrameTime();

        if (isUsing)
        {
            useTimer += dt * speed; // speed now truly scales duration
            float t = MathF.Min(useTimer / useDuration, 1f);

            float swingAngle;

            // Slight backswing (shorter phase)
            if (t < 0.15f)
            {
                float phase = t / 0.15f;
                float eased = EaseOutCubic(phase);
                swingAngle = Raymath.Lerp(0, -45, eased);
                xSkew = Raymath.Lerp(0, -3 * useDirection, eased);
                ySkew = Raymath.Lerp(0, -2, eased);
            }
            else
            {
                // Main swing: happens fast for impact feel
                float phase = (t - 0.15f) / 0.85f;
                float eased = EaseOutCubic(phase);
                swingAngle = Raymath.Lerp(-45, 75, eased);
                xSkew = Raymath.Lerp(-3 * useDirection, 5 * useDirection, eased);
                ySkew = Raymath.Lerp(-2, 3, eased);
            }

            // Correct rotation based on facing direction
            rotation = swingAngle * useDirection;

            if (t >= 1f)
            {
                isUsing = false;
                isRecovering = true;
                recoveryTimer = 0;
            }
        }
        else if (isRecovering)
        {
            // Smoothly return to idle
            recoveryTimer += dt;
            float rT = MathF.Min(recoveryTimer / recoveryDuration, 1f);

            float eased = EaseOutCubic(rT);
            rotation = Raymath.Lerp(rotation, 0, eased);
            xSkew = Raymath.Lerp(xSkew, 0, eased);
            ySkew = Raymath.Lerp(ySkew, 0, eased);

            if (rT >= 1f)
            {
                isRecovering = false;
                rotation = 0;
                xSkew = 0;
                ySkew = 0;
            }
        }
    }


}

public class Sword : Tool
{
    public override void Update()
    {
        float dt = Raylib.GetFrameTime();

        if (isUsing)
        {
            useTimer += dt * speed;
            float t = MathF.Min(useTimer / useDuration, 1f);

            float swingAngle;

            if (t < 0.15f)
            {
                float phase = t / 0.15f;
                float eased = EaseOutCubic(phase);
                swingAngle = Raymath.Lerp(0, -45, eased);
                xSkew = Raymath.Lerp(0, -3 * useDirection, eased);
                ySkew = Raymath.Lerp(0, -2, eased);
            }
            else
            {
                float phase = (t - 0.15f) / 0.85f;
                float eased = EaseOutCubic(phase);
                swingAngle = Raymath.Lerp(-45, 75, eased);
                xSkew = Raymath.Lerp(-3 * useDirection, 5 * useDirection, eased);
                ySkew = Raymath.Lerp(-2, 3, eased);
            }

            rotation = swingAngle * useDirection;

            if (t >= 1f)
            {
                isUsing = false;
                isRecovering = true;
                recoveryTimer = 0;
            }
        }
        else if (isRecovering)
        {
            recoveryTimer += dt;
            float rT = MathF.Min(recoveryTimer / recoveryDuration, 1f);

            float eased = EaseOutCubic(rT);
            rotation = Raymath.Lerp(rotation, 0, eased);
            xSkew = Raymath.Lerp(xSkew, 0, eased);
            ySkew = Raymath.Lerp(ySkew, 0, eased);

            if (rT >= 1f)
            {
                isRecovering = false;
                rotation = 0;
                xSkew = 0;
                ySkew = 0;
            }
        }
    }
}

public class Staff : Tool
{
    protected Vector2 direction;
    protected int projectileSpeed;
    public override void Update()
    {
        float dt = Raylib.GetFrameTime();

        if (isUsing)
        {
            useTimer += dt * speed;
            if (useTimer >= useDuration)
            {
                isUsing = false;
            }
        }

        direction = CameraSystem.Instance.GetMouseWorldPosition()
                        - Game.player.GetHeldItemPos();

        direction = Vector2.Normalize(direction);

        float angle = MathF.Atan2(direction.Y, direction.X) * (360 / (MathF.PI * 2));

        if (Game.player.GetLastDirection() == -1)
        {
            angle -= 270f;
        }

        angle += 45f;

        rotation = angle;
    }
    public override void OnUse()
    {
        base.OnUse();
        // ParticlePool.EmitParticles(1, direction * speed, Color.White, Color.White, 100, 5, Core.MAX_BRIGHTNESS, WorldGeneration.Instance.playerRef.GetHeldItemPos(), Vector2.Zero, false);
    }
}

public class SilverPickaxe : Pickaxe
{
    public SilverPickaxe(float damage, float speed)
    {
        this.damage = damage;
        this.speed = speed;
        toolVariant = ToolType.pickaxe;
    }
}

public class SilverSword : Sword
{
    public SilverSword(float damage, float speed)
    {
        this.damage = damage;
        this.speed = speed;
        toolVariant = ToolType.sword;
    }
}

public class WoodenPickaxe : Pickaxe
{
    public WoodenPickaxe(float damage, float speed)
    {
        this.damage = damage;
        this.speed = speed;
        toolVariant = ToolType.pickaxe;
    }
}

public class FlameStaff : Staff
{
    public FlameStaff(float damage, float speed)
    {
        this.damage = damage;
        this.speed = speed;
        projectileSpeed = 400;
        toolVariant = ToolType.staff;
    }

    public override void OnUse()
    {
        base.OnUse();
        var p = ProjectilePool.GetProjectile(new FireBallProjectile());
        Color fireColor = new Color(240, 127, 19, 255);
        p.projectileColor = Color.Yellow;
        p.lightColor = fireColor;
        p.destroyOnCollision = true;

        var v = direction * projectileSpeed;
        p.physicsBody.useGravity = true;
        p.physicsBody.gravity /= 8;
        p.Init(Game.player.GetHeldItemPos() + direction * Core.UNIT_SIZE * 2, v, 200, 6, Core.MAX_BRIGHTNESS, true);

        for (int i = 0; i < 10; i++)
        {
            //Smoke
            ParticlePool.EmitParticles(1,
            v / 10,
            new Color(120, 120, 120, 150), Color.Black, 300, 5, 0, Game.player.GetHeldItemPos(), -direction * Core.UNIT_SIZE * (2 - (i + 1 / 5)), true);
            
            // //Following Fire
            // ParticlePool.EmitParticles(3,
            // v / 2,
            // fireColor, p.lightColor, 60, 12 - i, Core.MAX_BRIGHTNESS / 2, Game.player.GetHeldItemPos(), direction * Core.UNIT_SIZE * (2 - (i + 1 / 5)), true);

            //Explosion when shot
            // ParticlePool.EmitParticles(1,
            // new Vector2(Random.Shared.Next(-5, 5), Random.Shared.Next(-5, 5)) * 70,
            // p.projectileColor, p.lightColor, 10, 10, Core.MAX_BRIGHTNESS / 2, Game.player.GetHeldItemPos(), direction * Core.UNIT_SIZE * 2, true);
        }

        if (!ProjectilePool.ActiveProjectiles.Contains(p))
            ProjectilePool.ActiveProjectiles.Add(p);
    }
}


