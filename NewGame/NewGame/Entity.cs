public class Entity : GameObject
{
    private Color color;
    public Color originalColor;
    public PhysicsBody? physicsBody;
    public Collider? collider;
    public Renderer? renderer;
    public override void Start()
    {
        base.Start();
        AddComponent<PhysicsBody>();
        AddComponent<Collider>();
        AddComponent<Renderer>();
        physicsBody = GetComponent<PhysicsBody>();
        renderer = GetComponent<Renderer>();
        collider = GetComponent<Collider>();
        collider.boxCollider = new Rectangle(0, 0, Core.UNIT_SIZE, Core.UNIT_SIZE);
        collider.colliderType = ColliderType.Dynamic;
        originalColor = Color.Black;
        color = originalColor;
        transform.SetZ(1);
    }

    public override void Update()
    {
        base.Update();

        collider.boxCollider.X = transform.position.X;
        collider.boxCollider.Y = transform.position.Y;

        CollisionSystem.Instance.UpdateTileSpatialHashAroundEntity(transform.position);
    }

    public override void Draw()
    {
        base.Draw();
        Raylib.DrawRectangleRec(collider.boxCollider, color);
    }

    public override Action? OnCollisionEnter(Collider other)
    {
        color = Color.Red;
        return base.OnCollisionEnter(other);
    }

    public override Action? OnCollisionExit(Collider other)
    {
        color = originalColor;
        return base.OnCollisionExit(other);
    }
}