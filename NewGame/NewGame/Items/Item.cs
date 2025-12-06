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

public abstract class Tool : Item
{
    public bool IsSwinging = false;
    protected float swingTimer = 0f;
    protected float swingDuration = 0.15f;
    protected float recoveryDuration = 0.3f;
    protected bool isRecovering = false;
    protected float recoveryTimer = 0f;

    public int swingDirection = 1; // 1 = right, -1 = left
    public float rotation = 0;
    public float xSkew = 0;
    public float ySkew = 0;

    public abstract void OnUse();

    public virtual void Update()
    {
        float dt = Raylib.GetFrameTime();

        if (IsSwinging)
        {
            swingTimer += dt * speed; // speed now truly scales duration
            float t = MathF.Min(swingTimer / swingDuration, 1f);

            float swingAngle;

            // Slight backswing (shorter phase)
            if (t < 0.15f)
            {
                float phase = t / 0.15f;
                float eased = EaseOutCubic(phase);
                swingAngle = Raymath.Lerp(0, -45, eased);
                xSkew = Raymath.Lerp(0, -3 * swingDirection, eased);
                ySkew = Raymath.Lerp(0, -2, eased);
            }
            else
            {
                // Main swing: happens fast for impact feel
                float phase = (t - 0.15f) / 0.85f;
                float eased = EaseOutCubic(phase);
                swingAngle = Raymath.Lerp(-45, 75, eased);
                xSkew = Raymath.Lerp(-3 * swingDirection, 5 * swingDirection, eased);
                ySkew = Raymath.Lerp(-2, 3, eased);
            }

            // Correct rotation based on facing direction
            rotation = swingAngle * swingDirection;

            if (t >= 1f)
            {
                IsSwinging = false;
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

public class SilverPickaxe : Tool
{
    public SilverPickaxe(float damage, float speed)
    {
        this.damage = damage;
        this.speed = speed;
    }

    public override void OnUse()
    {
        if (!IsSwinging)
        {
            IsSwinging = true;
            swingTimer = 0f;
        }
    }

}


