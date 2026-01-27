public abstract class Component
{
    protected GameObject parent;
    public void SetParent(GameObject newParent)
    {
        parent = newParent;
    }
    public virtual void Start() { }
    public virtual void Update() { }
    public virtual void OnDestroy() { }

}


public class Collider : Component
{
    public Rectangle boxCollider;
    public bool isActive = true;
    public bool isTrigger = false;
    public bool interactableByEntities = true;
    public ColliderType colliderType = ColliderType.Static;
    public bool should_NOT_Have_Collisionsbaby = false;

    public void UpdateBounds(Vector2 position)
    {
        if (boxCollider.Width != 0)
        {
            boxCollider.X = position.X;
            boxCollider.Y = position.Y;
        }
    }
}

public enum ColliderType
{
    Static,
    Dynamic
}


public class Renderer : Component
{
    public Texture2D sprite { get; set; }
}

