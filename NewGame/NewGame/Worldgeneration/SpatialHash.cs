public class SpatialHash
{
    private Dictionary<(int, int), HashSet<GameObject>> grid = new();

    public void Clear()
    {
        grid.Clear();
    }

    public void Insert(GameObject obj)
    {
        var collider = obj.GetComponent<Collider>();
        if (collider == null) return;

        Rectangle bounds = collider.boxCollider;

        int minX = (int)MathF.Floor(bounds.X / Core.UNIT_SIZE);
        int minY = (int)MathF.Floor(bounds.Y / Core.UNIT_SIZE);
        int maxX = (int)MathF.Floor((bounds.X + bounds.Width) / Core.UNIT_SIZE);
        int maxY = (int)MathF.Floor((bounds.Y + bounds.Height) / Core.UNIT_SIZE);

        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                var key = (x, y);
                if (!grid.TryGetValue(key, out var set))
                {
                    set = new HashSet<GameObject>();
                    grid[key] = set;
                }

                set.Add(obj);
            }
        }
    }

    public void Remove(GameObject obj)
    {
        var collider = obj.GetComponent<Collider>();
        if (collider == null) return;

        Rectangle bounds = collider.boxCollider;

        int minX = (int)MathF.Floor(bounds.X / Core.UNIT_SIZE);
        int minY = (int)MathF.Floor(bounds.Y / Core.UNIT_SIZE);
        int maxX = (int)MathF.Floor((bounds.X + bounds.Width) / Core.UNIT_SIZE);
        int maxY = (int)MathF.Floor((bounds.Y + bounds.Height) / Core.UNIT_SIZE);

        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                var key = (x, y);
                if (grid.TryGetValue(key, out var list))
                {
                    list.Remove(obj);

                    if (list.Count == 0)
                        grid.Remove(key);
                }
            }
        }
    }

    public IEnumerable<GameObject> QueryNearby(GameObject obj)
    {
        var collider = obj.GetComponent<Collider>();
        if (collider == null) yield break;

        Rectangle bounds = collider.boxCollider;

        int minX = (int)MathF.Floor(bounds.X / Core.UNIT_SIZE) - 1;
        int minY = (int)MathF.Floor(bounds.Y / Core.UNIT_SIZE) - 1;
        int maxX = (int)MathF.Floor((bounds.X + bounds.Width) / Core.UNIT_SIZE) + 1;
        int maxY = (int)MathF.Floor((bounds.Y + bounds.Height) / Core.UNIT_SIZE) + 1;

        HashSet<GameObject> visited = [];

        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                var key = (x, y);
                if (grid.TryGetValue(key, out var set))
                {
                    foreach (var candidate in set)
                    {
                        if (visited.Add(candidate))
                            yield return candidate;
                    }
                }
            }
        }
    }

    public Dictionary<(int, int), HashSet<GameObject>> GetAllObjects()
    {
        return grid;
    }

    public void RemoveUsingBounds(GameObject obj, Rectangle bounds)
    {
        int minX = (int)MathF.Floor(bounds.X / Core.UNIT_SIZE);
        int minY = (int)MathF.Floor(bounds.Y / Core.UNIT_SIZE);
        int maxX = (int)MathF.Floor((bounds.X + bounds.Width) / Core.UNIT_SIZE);
        int maxY = (int)MathF.Floor((bounds.Y + bounds.Height) / Core.UNIT_SIZE);

        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                var key = (x, y);
                if (grid.TryGetValue(key, out var set))
                {
                    set.Remove(obj);
                    if (set.Count == 0)
                        grid.Remove(key);
                }
            }
        }
    }
}
