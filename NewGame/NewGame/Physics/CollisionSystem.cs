public class CollisionSystem
{
    public SpatialHash dynamicSpatialHash = new();
    public SpatialHash staticSpatialHash = new();

    private static CollisionSystem? _instance;
    public static CollisionSystem Instance => _instance ??= new CollisionSystem();

    public void Update()
    {
        dynamicSpatialHash.Clear();
        foreach (var e in Game.GetEntities())
        {
            var c = e.GetComponentFast<Collider>();
            var pb = e.GetComponentFast<PhysicsBody>();
            if (c == null || pb == null)
                continue;


            if (!c.isActive || c.isTrigger) continue;

            dynamicSpatialHash.Insert(e);
        }

        foreach (var obj in Game.GetEntities())
        {
            var collider = obj.GetComponentFast<Collider>();
            var physicsBody = obj.GetComponentFast<PhysicsBody>();

            if (collider == null || physicsBody == null)
                continue;

            if (!collider.isActive)
                continue;

            HashSet<Collider> currentCollisions = new();

            float deltaTime = Raylib.GetFrameTime();
            Vector2 move = physicsBody.velocity * deltaTime;

            obj.transform.Translate(new Vector2(0, move.Y));
            collider.UpdateBounds(obj.transform.position);
            HandleCollisions(obj, physicsBody, collider, Axis.Y, ref currentCollisions);

            obj.transform.Translate(new Vector2(move.X, 0));
            collider.UpdateBounds(obj.transform.position);
            HandleCollisions(obj, physicsBody, collider, Axis.X, ref currentCollisions);

            foreach (var other in obj.collidingWith)
            {
                if (!currentCollisions.Contains(other))
                    obj.OnCollisionExit(other)?.Invoke();
            }

            foreach (var other in currentCollisions)
            {
                if (!obj.collidingWith.Contains(other))
                    obj.OnCollisionEnter(other)?.Invoke();
            }

            obj.collidingWith = currentCollisions;
        }
    }


    private void HandleCollisions(Entity obj, PhysicsBody physicsBody, Collider collider, Axis axis, ref HashSet<Collider> currentCollisions)
    {
        foreach (var otherObj in dynamicSpatialHash.QueryNearby(obj).Concat(staticSpatialHash.QueryNearby(obj)))
        {
            if (otherObj == obj) continue;
            if (Vector2.Distance(obj.transform.position, otherObj.transform.position) > Core.UNIT_SIZE * 3)
                continue;

            var otherCollider = otherObj.GetComponentFast<Collider>();
            if (otherCollider == null || !otherCollider.isActive) continue;

            if (!collider.interactableByEntities && !otherCollider.interactableByEntities) continue;

            if (Raylib.CheckCollisionRecs(collider.boxCollider, otherCollider.boxCollider))
            {
                currentCollisions.Add(otherCollider);

                if (otherCollider.isTrigger)
                {
                    continue;
                }

                Rectangle intersection = Raylib.GetCollisionRec(collider.boxCollider, otherCollider.boxCollider);

                if (axis == Axis.Y)
                {
                    if (physicsBody.velocity.Y > 0)
                        obj.transform.SetPositionY(obj.transform.position.Y - intersection.Height);
                    else if (physicsBody.velocity.Y < 0)
                        obj.transform.SetPositionY(obj.transform.position.Y + intersection.Height);

                    physicsBody.velocity.Y = 0;
                }
                else if (axis == Axis.X)
                {
                    if (physicsBody.velocity.X > 0)
                        obj.transform.SetPositionX(obj.transform.position.X - intersection.Width);
                    else if (physicsBody.velocity.X < 0)
                        obj.transform.SetPositionX(obj.transform.position.X + intersection.Width);

                    physicsBody.velocity.X = 0;
                }

                collider.UpdateBounds(obj.transform.position);
            }
        }
    }

    public void UpdateTileSpatialHashAroundEntity(Vector2 entityPosition)
    {
        var tileX = (int)MathF.Floor(entityPosition.X / Core.UNIT_SIZE);
        var tileY = (int)MathF.Floor(entityPosition.Y / Core.UNIT_SIZE);

        var bounds = (
            minX: tileX - Core.TILE_VISIBILITY_RADIUS,
            maxX: tileX + Core.TILE_VISIBILITY_RADIUS,
            minY: tileY - Core.TILE_VISIBILITY_RADIUS,
            maxY: tileY + Core.TILE_VISIBILITY_RADIUS
        );

        for (int x = bounds.minX; x <= bounds.maxX; x++)
        {
            for (int y = bounds.minY; y <= bounds.maxY; y++)
            {
                var tileWorldPos = new Vector2(x * Core.UNIT_SIZE, y * Core.UNIT_SIZE);
                var tile = WorldGeneration.Instance.GetTileAtWorldPosition(tileWorldPos);
                if (tile == null) continue;

                var collider = tile.GetComponentFast<Collider>();
                if (collider != null && collider.isActive && collider.interactableByEntities)
                {
                    collider.UpdateBounds(tile.transform.position);
                    staticSpatialHash.Insert(tile);
                }
            }
        }
    }

    public void UpdateTileSpatialHashForAllEntities()
    {
        staticSpatialHash.Clear();

        foreach (var e in Game.GetEntities())
        {
            var pos = e.transform.position;
            var tileX = (int)MathF.Floor(pos.X / Core.UNIT_SIZE);
            var tileY = (int)MathF.Floor(pos.Y / Core.UNIT_SIZE);

            var bounds = (
                minX: tileX - Core.TILE_VISIBILITY_RADIUS,
                maxX: tileX + Core.TILE_VISIBILITY_RADIUS,
                minY: tileY - Core.TILE_VISIBILITY_RADIUS,
                maxY: tileY + Core.TILE_VISIBILITY_RADIUS
            );

            for (int x = bounds.minX; x <= bounds.maxX; x++)
            {
                for (int y = bounds.minY; y <= bounds.maxY; y++)
                {
                    var tileWorldPos = new Vector2(x * Core.UNIT_SIZE, y * Core.UNIT_SIZE);
                    var tile = WorldGeneration.Instance.GetTileAtWorldPosition(tileWorldPos);
                    if (tile == null) continue;

                    var collider = tile.GetComponentFast<Collider>();
                    if (collider != null && collider.isActive && collider.interactableByEntities)
                    {
                        collider.UpdateBounds(tile.transform.position);
                        staticSpatialHash.Insert(tile);
                    }
                }
            }
        }
    }

    public void AddTileToSpatialHash(GameObject tile)
    {
        staticSpatialHash.Insert(tile);
    }

    public void RemoveTileFromSpatialHash(GameObject tile)
    {
        staticSpatialHash.Remove(tile);
    }

    private enum Axis { X, Y }
}

