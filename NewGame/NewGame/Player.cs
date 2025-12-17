global using MouseButton = Raylib_cs.MouseButton;
using System.Diagnostics.CodeAnalysis;

public sealed class Player : Entity
{
    float movementSpeed = 120;
    float baseMovementSpeed = 100;
    float jumpPower = 185;
    float acceleration = 9;
    float friction = 9;
    float maxSpeed = 300;

    float coyoteTime = 0.1f;
    float coyoteTimer = 0;

    float jumpBufferTime = 0.1f;
    float jumpBufferTimer = 0;

    float pickupDistance = 5;
    float interactionRange = 4;

    float actionCooldown = 0.2f;
    float actionTimer = 0;
    bool canPerformAction = true;

    public InventoryInterface? inventory;
    public InventoryComponent? inventoryComponent;

    Item currentItem;
    InteractableTile? interactableTile;

    private float itemDamage = 1;
    private int lastDirection = 1;

    bool creativeMode = false;

    public Player()
    {
        originalColor = Color.Blue;
    }

    public override void Start()
    {
        base.Start();
        AddComponent<InventoryComponent>();
        inventoryComponent = GetComponent<InventoryComponent>();
        inventory = new(inventoryComponent);
        AddComponent<WorkBenchComponent>();
        collider!.boxCollider.X = transform.position.X;
        collider.boxCollider.Y = transform.position.Y;
        collider.boxCollider.Height *= 2;
        collider.interactableByEntities = false;
        physicsBody!.weight = 15;
    }

    public void SetPlayerPos(Vector2? pos)
    {
        if (pos != null)
            transform.position = (Vector2)pos;

        else
            transform.position = Vector2.Zero;
    }

    public override void Update()
    {
        base.Update();
        HandleMovement();
        HandleJump();

        inventory.HandleHotbarInput();
        currentItem = inventory.GetSelectedHotbarItem();
        Tool? tool = currentItem as Tool;
        if (tool != null)
        {
            tool.swingDirection = GetLastDirection();
            itemDamage = tool.damage;
            tool.Update();
        }
        else
        {
            itemDamage = 1;
        }


        if (actionTimer > 0)
        {
            actionTimer -= Raylib.GetFrameTime();
            canPerformAction = false;
        }
        else
        {
            actionTimer = 0;
            canPerformAction = true;
        }

        if (inventory.isHovering)
        {
            canPerformAction = false;
        }

        if ((Raylib.IsMouseButtonPressed(MouseButton.Left) || Raylib.IsMouseButtonDown(MouseButton.Left)) && canPerformAction && !UIDragContext.isDragging)
        {
            PerformAction();
            HandleTileDestruction();
            if (tool != null)
                tool.OnUse();
        }
        if ((Raylib.IsMouseButtonPressed(MouseButton.Right) || Raylib.IsMouseButtonDown(MouseButton.Right)) && canPerformAction && !UIDragContext.isDragging)
        {
            HandleTilePlacing();
        }

        HandleItemPickups();
        HandleInteractableTile();

        if (GetAxisX() != 0)
            lastDirection = (int)MathF.Sign(GetAxisX());

        //DEBUG SSHGIIZZZ
        if (Raylib.IsKeyPressed(Raylib_cs.KeyboardKey.L))
        {
            creativeMode = !creativeMode;
        }
        if (Raylib.IsKeyPressed(KeyboardKey.O))
        {
            inventoryComponent.AddItem(ItemFactory.CreateItem((short)ItemFactory.ItemID.torch));
            inventoryComponent.AddItem(ItemFactory.CreateItem((short)ItemFactory.ItemID.furnace));
            inventoryComponent.AddItem(ItemFactory.CreateItem((short)ItemFactory.ItemID.silverPickaxe));
            inventoryComponent.AddItem(ItemFactory.CreateItem((short)ItemFactory.ItemID.coalore));
            inventoryComponent.AddItem(ItemFactory.CreateItem((short)ItemFactory.ItemID.copperore));
            inventoryComponent.AddItem(ItemFactory.CreateItem((short)ItemFactory.ItemID.silverore));
            inventoryComponent.AddItem(ItemFactory.CreateItem((short)ItemFactory.ItemID.workbench));
        }

        if (Raylib.IsKeyPressed(KeyboardKey.K))
        {
            var entity = new Entity();
            entity.transform.position = CameraSystem.Instance.GetMouseWorldPosition();
        }

    }

    public override void Draw()
    {
        base.Draw();
        if (currentItem is Tool tool)
        {
            var pos = new Vector2(transform.position.X + Core.UNIT_SIZE / 2 + tool.texture.Width / 4 * lastDirection, transform.position.Y + tool.texture.Height / 2) + new Vector2(tool.xSkew, tool.ySkew);

            Raylib.DrawTexturePro(
     tool.texture,
     new Rectangle(0, 0, tool.texture.Width * GetLastDirection(), tool.texture.Height),
     new Rectangle(pos, tool.texture.Width, tool.texture.Height),
     new Vector2(lastDirection == -1 ? tool.texture.Width : 0, tool.texture.Height),
     tool.rotation,
     Color.White);
        }
    }

    private void HandleMovement()
    {
        float targetSpeed = movementSpeed * GetAxisX();
        float targetSpeedY = movementSpeed * GetAxisY();

        if (GetAxisX() != 0)
        {
            physicsBody.velocity.X = Raymath.Lerp(physicsBody.velocity.X, targetSpeed, acceleration * Raylib.GetFrameTime());
        }
        else
        {
            physicsBody.velocity.X = Raymath.Lerp(physicsBody.velocity.X, 0, friction * Raylib.GetFrameTime());
        }

        if (creativeMode)
        {
            physicsBody.useGravity = false;
            collider.should_NOT_Have_Collisionsbaby = true;
            movementSpeed = 300;
            if (GetAxisY() != 0)
            {
                physicsBody.velocity.Y = Raymath.Lerp(physicsBody.velocity.Y, targetSpeedY, acceleration * Raylib.GetFrameTime());
            }
            else
            {
                physicsBody.velocity.Y = Raymath.Lerp(physicsBody.velocity.Y, 0, friction * Raylib.GetFrameTime());
            }
        }
        else
        {
            physicsBody.useGravity = true;
            collider.should_NOT_Have_Collisionsbaby = false;
            movementSpeed = baseMovementSpeed;
        }

        physicsBody.velocity.X = Raymath.Clamp(physicsBody.velocity.X, -maxSpeed, maxSpeed);
        physicsBody.velocity.Y = Raymath.Clamp(physicsBody.velocity.Y, -maxSpeed, maxSpeed);
    }

    private void HandleJump()
    {
        float deltaTime = Raylib.GetFrameTime();

        if (IsGrounded())
            coyoteTimer = coyoteTime;
        else
            coyoteTimer -= deltaTime;

        if (Raylib.IsKeyPressed(KeyboardKey.Space))
            jumpBufferTimer = jumpBufferTime;
        else
            jumpBufferTimer -= deltaTime;

        if (jumpBufferTimer > 0 && coyoteTimer > 0)
        {
            physicsBody.velocity = new Vector2(physicsBody.velocity.X, -jumpPower);
            jumpBufferTimer = 0;
        }

        if (Raylib.IsKeyReleased(KeyboardKey.Space) && physicsBody.velocity.Y < 0)
        {
            physicsBody.velocity = new Vector2(physicsBody.velocity.X, physicsBody.velocity.Y * 0.5f);
            coyoteTimer = 0;
        }

        if (physicsBody.velocity.Y > 0)
            physicsBody.gravity = new Vector2(0, 35f);

        else
            physicsBody.gravity = new Vector2(0, 20f);
    }

    private void HandleTileDestruction()
    {
        if (inventory.isHovering) return;

        if (interactableTile != null)
        {
            if (interactableTile.userInterface != null)
            {
                if (interactableTile.userInterface.isHovering)
                    return;
            }
        }

        Vector2 mouseWorldPos = CameraSystem.Instance.GetMouseWorldPosition();

        if (Vector2.Distance(transform.position, mouseWorldPos) > interactionRange * Core.UNIT_SIZE) return;

        foreach (var chunkIndex in WorldGeneration.Instance.visibleChunks)
        {
            if (!WorldGeneration.Instance.chunkMap.TryGetValue(chunkIndex, out var chunk)) continue;

            foreach (var kvp in chunk.tileMap.ToList())
            {
                var tileKey = kvp.Key;
                var tile = kvp.Value;
                var collider = tile.GetComponent<Collider>();
                if (collider == null) continue;
                if (!tile.canBeDestroyed) continue;

                if (!Raylib.CheckCollisionPointRec(mouseWorldPos, collider.boxCollider)) continue;

                if (!collider.isActive) break;

                tile.hitsRequired -= itemDamage;
                tile.OnHit();

                if (tile.hitsRequired <= 0)
                {
                    Console.WriteLine("Destroyed: " + tile.tag);
                    tile.OnDestruction();
                    chunk.MarkTileRemoved(tileKey);
                    if (tile is FurnaceTile furnaceTile && furnaceTile.userInterface != null)
                    {
                        furnaceTile.userInterface.Close();
                        var furnaceComponent = furnaceTile.GetComponentFast<FurnaceComponent>();

                        for (int i = 0; i < furnaceComponent.fuelSlot.amount; i++)
                        {
                            ItemFactory.CreateDroppedItem(furnaceComponent.fuelSlot.itemInSlot.ID, tile.transform.position);
                        }
                        for (int i = 0; i < furnaceComponent.inputSlot.amount; i++)
                        {
                            ItemFactory.CreateDroppedItem(furnaceComponent.inputSlot.itemInSlot.ID, tile.transform.position);
                        }
                        for (int i = 0; i < furnaceComponent.resultSlot.amount; i++)
                        {
                            ItemFactory.CreateDroppedItem(furnaceComponent.resultSlot.itemInSlot.ID, tile.transform.position);
                        }
                    }

                    collider.UpdateBounds(tile.transform.position);
                    CollisionSystem.Instance.staticSpatialHash.Remove(tile);
                    Game.RemoveGameObject(tile);
                    chunk.RemoveTileAt(tileKey);

                    if (chunk.foliageMap.ContainsKey(tileKey))
                        chunk.foliageMap.Remove(tileKey);

                    WorldGeneration.Instance.SaveChunk(chunkIndex);
                    // if (chunk.treeMap.ContainsKey((tileKey.Item1, tileKey.Item2 - 1)))
                    // {
                    //     chunk.treeMap.Remove(tileKey);
                    // }
                    return;
                }
            }
        }
    }

    private void HandleItemPickups()
    {
        if (inventoryComponent.IsFull())
            return;

        foreach (var obj in CollisionSystem.Instance.dynamicSpatialHash.QueryNearby(this))
        {
            if (inventoryComponent.IsFull()) break;
            if (obj is not DroppedItem item) continue;

            if (!item.CanBePickedUp || item.pickedUp)
                continue;


            if (Vector2.Distance(transform.position, item.transform.position) < pickupDistance * Core.UNIT_SIZE)
            {
                item.attractProgress += Raylib.GetFrameTime();
                item.collider.should_NOT_Have_Collisionsbaby = true;
                float t = MathF.Pow(item.attractProgress, 2);
                t = MathF.Min(t, 1f);
                Vector2 playerPos = transform.position + new Vector2(collider.boxCollider.Width / 2, collider.boxCollider.Height / 2);
                item.transform.position = Vector2.Lerp(item.transform.position, playerPos, t);

                if (Raymath.Vector2Distance(playerPos, item.transform.position) < Core.UNIT_SIZE)
                {
                    inventoryComponent.AddItem(item.item);
                    item.pickedUp = true;
                    Console.WriteLine($"Item {item.item.ID} picked up");
                    break;
                }
            }
            else
            {
                item.attractProgress = 0;
                item.collider.should_NOT_Have_Collisionsbaby = false;
            }

        }
    }

    private void HandleTilePlacing()
    {
        if (currentItem == null || !currentItem.placeable || inventory.isHovering)
            return;

        Vector2 mouseWorldPos = CameraSystem.Instance.GetMouseWorldPosition();
        if (Vector2.Distance(transform.position, mouseWorldPos) > interactionRange * Core.UNIT_SIZE)
            return;

        int tileX = (int)MathF.Floor(mouseWorldPos.X / Core.UNIT_SIZE);
        int tileY = (int)MathF.Floor(mouseWorldPos.Y / Core.UNIT_SIZE);

        (int, int) chunkIndex = (
            (int)MathF.Floor((float)tileX / Core.CHUNK_SIZE),
            (int)MathF.Floor((float)tileY / Core.CHUNK_SIZE));

        if (!WorldGeneration.Instance.chunkMap.TryGetValue(chunkIndex, out var chunk))
            return;

        var tileIndex = (tileX, tileY);

        var existing = WorldGeneration.Instance.GetTileAtTileCoordinate(tileIndex);

        if (existing != null)
            return;



        bool hasSupport = false;

        var belowTile = WorldGeneration.Instance.GetTileAtTileCoordinate((tileX, tileY + 1));

        if (belowTile != null)
        {
            if (belowTile.GetComponent<Collider>()?.isActive == true)
                hasSupport = true;
        }

        if (!hasSupport)
        {
            var aboveTile = WorldGeneration.Instance.GetTileAtTileCoordinate((tileX, tileY - 1));
            if (aboveTile != null)
            {
                if (aboveTile.GetComponent<Collider>()?.isActive == true)
                    hasSupport = true;
            }
        }

        if (!hasSupport)
        {
            var leftTile = WorldGeneration.Instance.GetTileAtTileCoordinate((tileX - 1, tileY));
            if (leftTile != null)
            {
                if (leftTile.GetComponent<Collider>()?.isActive == true)
                    hasSupport = true;
            }

            var rightTile = WorldGeneration.Instance.GetTileAtTileCoordinate((tileX + 1, tileY));
            if (rightTile != null)
            {
                if (rightTile.GetComponent<Collider>()?.isActive == true)
                    hasSupport = true;
            }
        }

        if (chunk.backgroundTileMap.TryGetValue(tileIndex, out BackgroundTile? value) && value != null)
            hasSupport = true;

        int finalTileY = tileY;
        bool snapped = false;

        var maybeMulti = ItemFactory.CreateTileFromItem(currentItem, Vector2.Zero);
        if (maybeMulti is MultiTile multiTile)
        {
            for (int y = tileY; y < tileY + 20; y++)
            {
                var solidBelow = WorldGeneration.Instance.GetTileAtTileCoordinate((tileX, y + 1));
                if (solidBelow?.GetComponent<Collider>()?.isActive == true)
                {
                    finalTileY = y - (multiTile.heightInTiles - 1);
                    snapped = true;
                    break;
                }
            }

            if (!snapped)
            {
                Console.WriteLine("No ground found under multi-tile placement!");
                return;
            }

            tileY = finalTileY;
            hasSupport = true;
        }

        if (!hasSupport)
        {
            Console.WriteLine($"No support found for placement at {tileX},{tileY}");
            Console.WriteLine($"Below tile: {(belowTile != null ? belowTile.tag : "null")}");
            Console.WriteLine($"Above tile: {(WorldGeneration.Instance.GetTileAtTileCoordinate((tileX, tileY - 1)) != null ? "exists" : "null")}");
            Console.WriteLine($"Left tile: {(WorldGeneration.Instance.GetTileAtTileCoordinate((tileX - 1, tileY)) != null ? "exists" : "null")}");
            Console.WriteLine($"Right tile: {(WorldGeneration.Instance.GetTileAtTileCoordinate((tileX + 1, tileY)) != null ? "exists" : "null")}");
            return;
        }

        var finalIndex = (tileX, tileY);
        Vector2 tileWorldPos = new(finalIndex.tileX * Core.UNIT_SIZE, finalIndex.tileY * Core.UNIT_SIZE);

        Tile tile = ItemFactory.CreateTileFromItem(currentItem, tileWorldPos);

        if (tile is MultiTile multiTilePlaced)
        {
            if (!chunk.CanPlaceArea(finalIndex.tileX, finalIndex.tileY, multiTilePlaced.widthInTiles, multiTilePlaced.heightInTiles))
            {
                Game.RemoveGameObject(multiTilePlaced);
                return;
            }

            chunk.PlaceMultiTile(multiTilePlaced, finalIndex.tileX, finalIndex.tileY);

            // mark every tile cell of the multi-tile as modified so it persists
            for (int ox = 0; ox < multiTilePlaced.widthInTiles; ox++)
            {
                for (int oy = 0; oy < multiTilePlaced.heightInTiles; oy++)
                {
                    var partIndex = (finalIndex.tileX + ox, finalIndex.tileY + oy);
                    if (chunk.tileMap.TryGetValue(partIndex, out var placedPart) && placedPart != null)
                    {
                        chunk.MarkTileModified(partIndex, placedPart);
                    }
                }
            }
        }
        else
        {
            chunk.tileMap[finalIndex] = tile;
            // mark single-tile placement
            chunk.MarkTileModified(finalIndex, tile);
        }

        if (tile is TreeTile tree)
        {
            tree.ResetAge();
        }

        inventoryComponent!.DecreaseItemAmount(currentItem, 1);

        // ensure transform + collisions are set before saving
        tile.transform.position = tileWorldPos;
        var tileCollider = tile.GetComponentFast<Collider>();
        tileCollider?.UpdateBounds(tile.transform.position);
        CollisionSystem.Instance.staticSpatialHash.Insert(tile);

        // Persist changes to disk immediately (safe small IO). Use WorldGeneration helper.
        var chunkIndexLocal = chunkIndex; // capture
        WorldGeneration.Instance.SaveChunk(chunkIndexLocal);
    }

    private void HandleInteractableTile()
    {
        interactableTile = null;

        float maxDist = 6 * Core.UNIT_SIZE;
        float bestDist = float.MaxValue;

        foreach ((int, int) chunkIndex in WorldGeneration.Instance.visibleChunks)
        {
            if (!WorldGeneration.Instance.chunkMap.TryGetValue(chunkIndex, out var chunk))
                continue;

            foreach (var kvp in chunk.tileMap)
            {
                if (kvp.Value is InteractableTile tile)
                {
                    float d = Raymath.Vector2Distance(transform.position, tile.transform.position);
                    if (d <= maxDist && d < bestDist)
                    {
                        interactableTile = tile;
                        bestDist = d;
                    }
                }
            }
        }

        if (Raylib.IsKeyPressed(KeyboardKey.Tab))
        {
            inventory.showTiledInventory = !inventory.showTiledInventory;
        }

        if (InteractableTile.ActiveInteractable != null)
        {
            float activeDist = Raymath.Vector2Distance(
                transform.position,
                InteractableTile.ActiveInteractable.transform.position
            );

            if (activeDist > maxDist)
            {
                var ui = InteractableTile.ActiveInteractable.userInterface;
                if (ui != null && ui.IsOpen())
                {
                    ui.Close();
                    SlotUtils.RemoveInterface(ui);
                }

                InteractableTile.ActiveInteractable = null;
            }
        }

        if (interactableTile != null)
        {
            if (Raylib.IsKeyPressed(KeyboardKey.E))
            {
                interactableTile.OnInteract();
            }
        }
    }

    private void PerformAction()
    {
        actionTimer = actionCooldown;
    }

    public InteractableTile? GetInteractableTile()
    {
        return interactableTile;
    }

    private int GetLastDirection()
    {
        return lastDirection;
    }

    private bool IsGrounded()
    {
        return physicsBody.velocity.Y == 0;
    }
}