public abstract class GameObject
{
    public string tag;
    // public int ID;
    public Transform transform;
    public HashSet<Collider> collidingWith = [];
    public bool shouldBeDestroyed = false;

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
        // ensure component knows its parent and is initialized immediately
        comp.SetParent(this);
        _components.Add(comp);
        // run component Start so it can initialize internal state (e.g. Light)
        comp.Start();
        // keep cache consistent
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
        _components.ForEach(c => c.Start());
    }

    public virtual void Update()
    {
        _components.ForEach(c =>
        {
            c.SetParent(this);
            c.Update();
        }
        );
    }

    public virtual void Draw()
    {

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


}