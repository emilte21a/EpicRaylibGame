public abstract class GameObject
{
    public string tag;
    // public int ID;
    public Transform transform;
    public HashSet<Collider> collidingWith = [];
    private bool shouldBeDestroyed = false;

    private List<Component> _components;

    private Dictionary<Type, Component> _componentCache = new();

    public GameObject()
    {
        _components = [];
        transform = new Transform();
        _components.Add(transform);

        Start();
    }

    public void AddComponent<T>() where T : Component, new()
    {
        if (_components.OfType<T>().Any()) return;

        var comp = new T();
        comp.SetParent(this);
        _components.Add(comp);
        comp.Start();
        _componentCache[typeof(T)] = comp;
    }

    public T? GetComponent<T>() where T : Component
    {
        return _components.OfType<T>().FirstOrDefault();
    }

    public T? GetComponentFast<T>() where T : Component
    {
        if (_componentCache.TryGetValue(typeof(T), out var comp))
            return comp as T;

        var found = _components.OfType<T>().FirstOrDefault();
        if (found != null)
            _componentCache[typeof(T)] = found;

        return found;
    }

    public virtual void Start()
    {
        Game.AddGameObjectToGame(this);
        _components.ForEach(c =>
        {
            // c.SetParent(this);
            // c.Start();
        });
    }

    public virtual void Update()
    {
        if (shouldBeDestroyed)
        {
            OnDestroy();
            return;
        }

        _components.ForEach(c =>
        {
            c.Update();
        }
        );
    }

    public virtual void Draw()
    {
    }

    private void DestroyNow()
    {
        // Call OnDestroy on all components
        foreach (var component in _components)
        {
            component.OnDestroy();
        }

        // Remove from spatial hashes
        if (this is Tile)
        {
            CollisionSystem.Instance.RemoveTileFromSpatialHash(this);
        }
        else if (this is Entity)
        {
            CollisionSystem.Instance.RemoveEntityFromSpatialHash(this);
        }

        // Remove from game object lists
        Game.RemoveGameObject(this);

        // Clear components
        _components.Clear();
        _componentCache.Clear();
    }

    public virtual void OnDestroy()
    {
        DestroyNow();
    }

    private void ClearComponents()
    {
        // Call cleanup on all components that need it
        foreach (var component in _components)
        {
            if (component is Lightsource lightsource)
            {
                lightsource.light.SetBrightness(0);
            }
        }

        _components.Clear();
        _componentCache.Clear();
    }

    public virtual Action? OnCollisionEnter(Collider other)
    {
        return null;
    }

    public virtual Action? OnCollisionExit(Collider other)
    {
        return null;
    }

    public int GetAxisX()
    {
        if (Raylib.IsKeyDown(KeyboardKey.A))
            return -1;

        else if (Raylib.IsKeyDown(KeyboardKey.D))
            return 1;

        return 0;
    }
    public int GetAxisY()
    {
        if (Raylib.IsKeyDown(KeyboardKey.W))
            return -1;

        else if (Raylib.IsKeyDown(KeyboardKey.S))
            return 1;

        return 0;
    }

    public void MarkForDestruction()
    {
        shouldBeDestroyed = true;
    }
}