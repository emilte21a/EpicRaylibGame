public class DroppedItem : Entity
{
    public Item item;
    public float pickupDelay = 0.2f;
    public float attractProgress = 0;
    private float age = 0;
    public bool pickedUp = false;

    public DroppedItem(Item item, Vector2 position)
    {
        this.item = item;
        transform.position = position;
    }

    public override void Start()
    {
        base.Start();
        collider.boxCollider = new Rectangle(transform.position, Core.UNIT_SIZE - 6, Core.UNIT_SIZE - 6);
        collider.colliderType = ColliderType.Dynamic;
        collider.isActive = true;
        collider.isTrigger = false;
        collider.interactableByEntities = false;
        physicsBody.useGravity = true;

        physicsBody.velocity = new Vector2(Random.Shared.Next(-10, 10), -20);
        physicsBody.weight = 5;
    }

    public override void Update()
    {
        base.Update();
        age += Raylib.GetFrameTime();
        physicsBody.velocity.X = Raymath.Lerp(physicsBody.velocity.X, 0, Raylib.GetFrameTime());
        if (pickedUp) MarkForDestruction();
    }

    public override void Draw()
    {
        float pulse = 0.1f * MathF.Sin((float)Raylib.GetTime() * 3f) + 0.7f; // Scale between 0.9 and 1.1

        Vector2 center = new Vector2(
            transform.position.X + collider.boxCollider.Width / 2f,
            transform.position.Y + collider.boxCollider.Height / 2f
        );

        float size = item.texture.Width * pulse / 2f;

        Raylib.DrawTexturePro(
            item.texture,
            new Rectangle(0, 0, item.texture.Width, item.texture.Height),
            new Rectangle(center.X, center.Y, item.texture.Width * pulse, item.texture.Height * pulse),
            new Vector2(size, size),
            0,
            Color.White
        );
    }

    public bool CanBePickedUp => age >= pickupDelay;
}