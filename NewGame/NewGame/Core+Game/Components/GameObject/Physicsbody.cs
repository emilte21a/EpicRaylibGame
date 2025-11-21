public class PhysicsBody : Component
{
    public Vector2 acceleration = Vector2.Zero;

    public Vector2 velocity = Vector2.Zero;

    public Vector2 gravity = new Vector2(0, 10f);
    
    public bool useGravity = true;

    public PhysicsContext physicsContext = PhysicsContext.grounded;

    public enum PhysicsContext
    {
        grounded = 1,
        air = 1,
        liquid = 4
    }

    public float weight = 150;
}