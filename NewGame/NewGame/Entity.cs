
using System.Reflection.Metadata.Ecma335;

public class Entity : GameObject
{
    private Color color;
    public Color originalColor;
    public PhysicsBody? physicsBody;
    public Collider? collider;
    public override void Start()
    {
        AddComponent<PhysicsBody>();
        AddComponent<Collider>();
        physicsBody = GetComponent<PhysicsBody>();
        collider = GetComponent<Collider>();
        collider.boxCollider = new Rectangle(0, 0, Core.UNIT_SIZE, Core.UNIT_SIZE);
        collider.colliderType = ColliderType.Dynamic;
        originalColor = Color.Black;
        color = originalColor;
        transform.SetZ(1);
        Game.AddEntityToGame(this);
    }

    public override void Update()
    {
        collider.boxCollider.X = transform.position.X;
        collider.boxCollider.Y = transform.position.Y;
        CollisionSystem.Instance.UpdateTileSpatialHashAroundEntity(transform.position);
    }

    public override void Draw()
    {
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