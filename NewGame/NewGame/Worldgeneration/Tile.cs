using LibNoise.Combiner;
using SharpNoise.Modules;

public abstract class Tile : GameObject
{
    protected Collider? collider;
    protected Renderer? renderer;

    public bool isSolid = true;
    public string tileType = "default";
    public Color color = Color.Beige;
    public bool blocksLight => isSolid;
    public bool canBeDestroyed = true;
    public float hitsRequired = 5;

    public Dictionary<short, int> itemIdsDropAmounts;
    public short dropItemId;
    public short tileId;
    private int dropAmount = 1;
    public virtual bool IsMultiTilePart => false;
    float whiteOverlayOpacity = 0;

    public override void Start()
    {
        base.Start();
        TileFactory.AddTileToTileFactory(this);

        itemIdsDropAmounts = [];
        tag = "Tile";
        AddComponent<Collider>();
        AddComponent<Renderer>();

        collider = GetComponent<Collider>();
        renderer = GetComponent<Renderer>();

        collider.boxCollider = new Rectangle(transform.position.X, transform.position.Y, Core.UNIT_SIZE, Core.UNIT_SIZE);
        collider.colliderType = ColliderType.Static;
        collider.isTrigger = false;
        collider.interactableByEntities = true;
    }

    public override void Draw()
    {
        base.Draw();
        if (renderer != null && renderer.sprite.Width > 0)
        {
            var dst = new Rectangle(
                collider.boxCollider.X + collider.boxCollider.Width / 2,
                collider.boxCollider.Y + collider.boxCollider.Height / 2,
                collider.boxCollider.Width,
                collider.boxCollider.Height
            );

            var origin = new Vector2(dst.Width / 2f, dst.Height / 2f);

            var src = new Rectangle(0, 0, renderer.sprite.Width, renderer.sprite.Height);
            Raylib.DrawTexturePro(renderer.sprite, src, dst, origin, transform.zRotation, Color.White);
            if (whiteOverlayOpacity > 0)
            {
                Raylib.BeginBlendMode(Raylib_cs.BlendMode.Additive);
                Raylib.DrawRectangle((int)((int)dst.X - origin.X), (int)((int)dst.Y - origin.Y), (int)dst.Width, (int)dst.Height, new Color(255, 255, 255, 255 * whiteOverlayOpacity));
                Raylib.EndBlendMode();
            }
        }
        else
            Raylib.DrawRectangleRec(collider.boxCollider, color);
    }

    public override void Update()
    {
        base.Update();
        transform.position = new Vector2(
            MathF.Floor(transform.position.X / Core.UNIT_SIZE) * Core.UNIT_SIZE,
            MathF.Floor(transform.position.Y / Core.UNIT_SIZE) * Core.UNIT_SIZE
        );
        collider.boxCollider.X = transform.position.X;
        collider.boxCollider.Y = transform.position.Y;
        transform.zRotation = (int)Raymath.Lerp(transform.zRotation, 0, Raylib.GetFrameTime() * 2);
        whiteOverlayOpacity = Raymath.Lerp(whiteOverlayOpacity, 0, Raylib.GetFrameTime() * 40);
    }

    public virtual void OnDestruction()
    {
        Vector2 particleOffset = new Vector2(collider.boxCollider.Width / 2f, collider.boxCollider.Height / 2f);
        for (int i = 0; i < 10; i++)
        {
            Vector2 particleVelocity = new Vector2(Random.Shared.Next(-40, 40), Random.Shared.Next(-40, 40));
            ParticlePool.EmitParticles(1, particleVelocity, color, Color.Blank, 50, Core.UNIT_SIZE / 5, 0, transform.position, particleOffset, true);
        }

        Console.WriteLine("Tile destroyed at " + transform.position);
        foreach (var item in itemIdsDropAmounts)
        {
            for (int i = 0; i < item.Value; i++)
            {
                var dropped = ItemFactory.CreateDroppedItem(item.Key, transform.position);
                dropped.originalColor = color;
                Game.AddEntityToGame(dropped);
                dropped.Start();
                Console.WriteLine("Dropped item created at " + dropped.transform.position);
            }
        }
    }

    public virtual void OnHit()
    {
        int rand = Random.Shared.Next(-20, 20) > 0 ? 20 : -20;

        transform.zRotation = rand;
        whiteOverlayOpacity = 1;
    }

    public virtual Tile Clone()
    {
        return this;
    }
}

public class GrassTile : Tile
{
    public override void Start()
    {
        base.Start();
        tileType = "GrassTile";
        itemIdsDropAmounts.Add((short)ItemFactory.ItemID.grass, 1);
        tileId = (short)TileFactory.TileID.grass;
        TileFactory.RegisterTileType<GrassTile>(tileId);
        color = Color.DarkGreen;
        renderer.sprite = TextureManager.LoadTexture("Textures/grass.png");
        hitsRequired = 3;
    }
}

public class StoneTile : Tile
{
    public override void Start()
    {
        base.Start();
        tileType = "StoneTile";
        itemIdsDropAmounts.Add((short)ItemFactory.ItemID.stone, 1);
        tileId = (short)TileFactory.TileID.stone;
        TileFactory.RegisterTileType<StoneTile>(tileId);
        color = Color.DarkGray;
        renderer.sprite = TextureManager.LoadTexture("Textures/stone.png");
    }
}

public class DirtTile : Tile
{
    public override void Start()
    {
        base.Start();
        tileType = "DirtTile";
        itemIdsDropAmounts.Add((short)ItemFactory.ItemID.dirt, 1);
        tileId = (short)TileFactory.TileID.dirt;
        TileFactory.RegisterTileType<DirtTile>(tileId);
        color = Color.DarkBrown;
        renderer.sprite = TextureManager.LoadTexture("Textures/dirt.png");
        hitsRequired = 3;
    }
}

public class BackgroundTile : Tile
{
    public override void Start()
    {
        tag = "Tile";
        tileId = (short)TileFactory.TileID.background;
        TileFactory.RegisterTileType<BackgroundTile>(tileId);
        AddComponent<Collider>();
        AddComponent<Renderer>();
        collider = GetComponentFast<Collider>();
        renderer = GetComponentFast<Renderer>();
        tileType = "BackgroundTile";
        collider.boxCollider = new Rectangle(transform.position.X, transform.position.Y, Core.UNIT_SIZE, Core.UNIT_SIZE);
        collider.colliderType = ColliderType.Static;
        color = Color.Purple;
        collider.isActive = false;
        canBeDestroyed = false;
        isSolid = false;

        if (renderer != null)
            renderer.sprite = TextureManager.LoadTexture("Textures/background.png");
    }
}

public class Torch : Tile
{
    Animator? animator;
    Lightsource? lightsource;
    ParticleEmitter? particleEmitter;

    public override void Start()
    {
        base.Start();
        AddComponent<Animator>();
        animator = GetComponent<Animator>();
        color = new Color(120, 120, 120, 40);

        AddComponent<Lightsource>();
        lightsource = GetComponent<Lightsource>();
        lightsource.color = color;
        lightsource.light.SetBrightness(Core.MAX_BRIGHTNESS);

        AddComponent<ParticleEmitter>();
        particleEmitter = GetComponent<ParticleEmitter>();
        if (particleEmitter != null)
        {
            particleEmitter.offset = new Vector2(collider.boxCollider.Width / 2, collider.boxCollider.Height / 2 - 5);
            particleEmitter.color = color;
            particleEmitter.lightColor = new Color(255, 196, 119, 255);
            particleEmitter.particleAmount = 4;
            particleEmitter.yVelocity = -15;
            particleEmitter.perlinFrequency = 2;
            particleEmitter.brightness = Core.MAX_BRIGHTNESS;
            particleEmitter.particleSpawnDelay = 0.2f;
            particleEmitter.lifeTime = 300;
            particleEmitter.size = Core.UNIT_SIZE / 3;
            particleEmitter.ResetCooldown();
        }

        tileType = "Torch";
        itemIdsDropAmounts.Add((short)ItemFactory.ItemID.torch, 1);
        tileId = (short)TileFactory.TileID.torch;
        TileFactory.RegisterTileType<Torch>(tileId);

        isSolid = false;
        hitsRequired = 1;
        transform.SetZ(2);
        collider.interactableByEntities = false;
        renderer.sprite = TextureManager.LoadTexture("Textures/torch.png");
    }

    public override void Update()
    {
        base.Update();
    }

    public override void Draw()
    {
        animator.PlayAnimation(renderer.sprite, 1, 5, transform.position);
    }
}

public class OreTile : Tile
{
    protected ParticleEmitter? particleEmitter;
    public override void Start()
    {
        base.Start();
        AddComponent<ParticleEmitter>();
        particleEmitter = GetComponent<ParticleEmitter>();
        if (particleEmitter != null)
        {
            particleEmitter.color = color;
            particleEmitter.particleAmount = 1;
            particleEmitter.yVelocity = Random.Shared.Next(-1, 1);
            particleEmitter.perlinFrequency = 10;
            particleEmitter.brightness = 1;
            particleEmitter.particleSpawnDelay = 7f + Random.Shared.Next(-1, 2);
            particleEmitter.lifeTime = 1;
            particleEmitter.size = 1;
            particleEmitter.ResetCooldown();
        }
    }
}

public class CopperOreTile : OreTile
{
    public override void Start()
    {
        base.Start();
        tileType = "CopperOreTile";
        itemIdsDropAmounts.Add((short)ItemFactory.ItemID.copperore, Random.Shared.Next(1, 5));
        tileId = (short)TileFactory.TileID.copper;
        TileFactory.RegisterTileType<CopperOreTile>(tileId);
        color = Color.Orange;
        renderer.sprite = TextureManager.LoadTexture("Textures/copperoretile.png");
    }
}
public class SilverOreTile : OreTile
{
    public override void Start()
    {
        base.Start();
        tileType = "SilverOreTile";
        itemIdsDropAmounts.Add((short)ItemFactory.ItemID.silverore, Random.Shared.Next(1, 3));
        tileId = (short)TileFactory.TileID.silver;
        TileFactory.RegisterTileType<SilverOreTile>(tileId);
        color = Color.LightGray;
        renderer.sprite = TextureManager.LoadTexture("Textures/silveroretile.png");
    }
}
public class CoalOreTile : OreTile
{
    public override void Start()
    {
        base.Start();
        tileType = "CoalOreTile";
        itemIdsDropAmounts.Add((short)ItemFactory.ItemID.coalore, Random.Shared.Next(2, 4));
        tileId = (short)TileFactory.TileID.coal;
        TileFactory.RegisterTileType<CoalOreTile>(tileId);
        color = Color.Black;
        renderer.sprite = TextureManager.LoadTexture("Textures/coaloretile.png");
    }
}

public class MultiTile : Tile
{
    public int widthInTiles = 1;
    public int heightInTiles = 1;

    public int originTileX;
    public int originTileY;

    public virtual void OnPlaced(int originTileX, int originTileY)
    {
        this.originTileX = originTileX;
        this.originTileY = originTileY;
    }
}

public class MultiTilePart : Tile
{
    public MultiTile parent;
    public int offsetX;
    public int offsetY;
    public override bool IsMultiTilePart => true;
    public MultiTilePart(MultiTile parent, int offX, int offY)
    {
        this.parent = parent;
        offsetX = offX;
        offsetY = offY;
        canBeDestroyed = false;
        isSolid = parent.isSolid;
    }
}

public class InteractableTile : MultiTile
{
    public UserInterface? userInterface;

    public static InteractableTile? ActiveInteractable;

    public override void Start()
    {
        base.Start();
        isSolid = false;
    }

    public void ToggleInterface()
    {
        if (userInterface == null) return;

        if (userInterface.IsOpen())
        {
            userInterface.Close();
            if (ActiveInteractable == this) ActiveInteractable = null;
            SlotUtils.RemoveInterface(userInterface);
            userInterface.isHovering = false;
            return;
        }

        if (ActiveInteractable != null && ActiveInteractable != this)
        {
            ActiveInteractable.userInterface?.Close();
            SlotUtils.RemoveInterface(ActiveInteractable.userInterface);
            ActiveInteractable = null;
        }
        userInterface.Open();
        ActiveInteractable = this;
        SlotUtils.AddInterface(userInterface);
    }

    public virtual void OnInteract()
    {
        ToggleInterface();
    }
}

public class FurnaceTile : InteractableTile
{
    private FurnaceComponent? furnaceComponent;
    private Lightsource? lightSource;
    public override void OnPlaced(int originTileX, int originTileY)
    {
        base.OnPlaced(originTileX, originTileY);
        transform.position = new Vector2(originTileX * Core.UNIT_SIZE, originTileY * Core.UNIT_SIZE);
    }

    public override void Start()
    {
        base.Start();
        AddComponent<Lightsource>();
        lightSource = GetComponent<Lightsource>();

        AddComponent<FurnaceComponent>();
        furnaceComponent = GetComponentFast<FurnaceComponent>();

        widthInTiles = 2;
        collider.isTrigger = true;
        isSolid = false;
        collider.isActive = true;
        tileType = "FurnaceTile";
        itemIdsDropAmounts.Add((short)ItemFactory.ItemID.furnace, 1);
        tileId = (short)TileFactory.TileID.furnace;
        TileFactory.RegisterTileType<FurnaceTile>(tileId);
        color = Color.DarkGray;
        renderer.sprite = TextureManager.LoadTexture("Textures/furnace.png");
        transform.position = new Vector2(
            MathF.Floor(transform.position.X / Core.UNIT_SIZE) * Core.UNIT_SIZE,
            MathF.Floor(transform.position.Y / Core.UNIT_SIZE) * Core.UNIT_SIZE
        );
        collider.boxCollider = new Rectangle(transform.position.X, transform.position.Y, Core.UNIT_SIZE * widthInTiles, Core.UNIT_SIZE);
        lightSource.SetParent(this);
        lightSource.light = new Light();
        userInterface = new FurnaceInterface();
        var furnaceUI = (FurnaceInterface)userInterface;
        furnaceComponent.SetupSlots(furnaceUI.fuelSlot, furnaceUI.inputSlot, furnaceUI.resultSlot);
        furnaceUI.ownerTile = this;
    }

    public override void OnInteract()
    {
        ToggleInterface();
    }

    public override void Update()
    {
        base.Update();

        furnaceComponent?.Update();

        if (furnaceComponent != null && furnaceComponent.active && lightSource != null)
        {
            lightSource.light.SetBrightness(Core.MAX_BRIGHTNESS);
        }
    }

    public override void OnHit()
    {
        int rand = Random.Shared.Next(-20, 20) > 0 ? 5 : -5;

        transform.zRotation = rand;
    }
}

public class WorkBench : InteractableTile
{
    WorkBenchComponent? workbenchComponent;
    public override void OnPlaced(int originTileX, int originTileY)
    {
        base.OnPlaced(originTileX, originTileY);
        transform.position = new Vector2(originTileX * Core.UNIT_SIZE, originTileY * Core.UNIT_SIZE);
    }

    public override void Start()
    {
        base.Start();
        widthInTiles = 2;
        heightInTiles = 2;
        collider.isTrigger = true;
        isSolid = false;
        tileType = "CraftingTableTile";
        itemIdsDropAmounts.Add((short)ItemFactory.ItemID.workbench, 1);
        tileId = (short)TileFactory.TileID.craftingTable;
        TileFactory.RegisterTileType<WorkBench>(tileId);
        color = Color.Brown;
        renderer.sprite = TextureManager.LoadTexture("Textures/workbench.png");
        collider.boxCollider = new Rectangle(transform.position.X, transform.position.Y, Core.UNIT_SIZE * widthInTiles, Core.UNIT_SIZE);

        // attach a crafting component and create the UI from it (tier1 as example)
        AddComponent<WorkBenchComponent>();
        workbenchComponent = GetComponent<WorkBenchComponent>();
        workbenchComponent.SetupComponent(CraftingTier.tier1);
        userInterface = new WorkBenchInterface();
        var workbenchUI = (WorkBenchInterface)userInterface;
        workbenchUI.ownerTile = this;
        workbenchUI.component = workbenchComponent;
        workbenchUI.Initialize();
    }
}

public class TreeTile : MultiTile
{
    public TreeTile()
    {
        widthInTiles = 1;   // tree occupies 1 tile for collision / logic
        heightInTiles = 4;  // trunk height
                            // how wide the sprite is visually (in tiles)
        spriteWidthInTiles = 3;
    }

    // visual-only field
    public int spriteWidthInTiles = 3;
    public int age = 0;
    private float ageTimer = 0;

    public override void OnPlaced(int originTileX, int originTileY)
    {
        base.OnPlaced(originTileX, originTileY);
        transform.position = new Vector2(originTileX * Core.UNIT_SIZE, originTileY * Core.UNIT_SIZE);
    }

    public override void Start()
    {
        base.Start();
        collider.isTrigger = true;
        isSolid = false;
        tileType = "TreeTile";

        itemIdsDropAmounts.Add((short)ItemFactory.ItemID.sapling, Random.Shared.Next(1, 3));
        tileId = (short)TileFactory.TileID.tree;
        TileFactory.RegisterTileType<TreeTile>(tileId);
        color = Color.DarkBrown;
        renderer.sprite = TextureManager.LoadTexture("Textures/treestages.png");
        collider.boxCollider = new Rectangle(transform.position.X, transform.position.Y, Core.UNIT_SIZE * widthInTiles, Core.UNIT_SIZE * heightInTiles);
        age = 3;
    }

    public override void OnHit()
    {
        int rand = Random.Shared.Next(-20, 20) > 0 ? 5 : -5;

        transform.zRotation = rand;
    }

    public override void Update()
    {
        base.Update();

        if (age >= 3)
        {
            ageTimer = 0;
            if (!itemIdsDropAmounts.ContainsKey((short)ItemFactory.ItemID.wood))
                itemIdsDropAmounts.Add((short)ItemFactory.ItemID.wood, Random.Shared.Next(5, 8));
        }
        else
        {
            ageTimer += Raylib.GetFrameTime();
        }

        if (ageTimer >= 5 && age < 3)
        {
            ageTimer = 0;
            age++;
        }
    }

    public override void Draw()
    {
        if (renderer != null && renderer.sprite.Width > 0)
        {
            // Draw sprite centered on the collider's horizontal center but with a wider width
            float dstWidth = Core.UNIT_SIZE * spriteWidthInTiles;
            float dstHeight = collider.boxCollider.Height; // keep height equal to collider (tree art matches trunk height)
            float dstX = collider.boxCollider.X + Core.UNIT_SIZE / 2;
            float dstY = collider.boxCollider.Y + dstHeight / 2;

            var dst = new Rectangle(dstX, dstY, dstWidth, dstHeight);
            var origin = new Vector2(dst.Width / 2f, dst.Height / 2);
            var src = new Rectangle(age * (renderer.sprite.Width / 4), 0, renderer.sprite.Width / 4, renderer.sprite.Height);
            Raylib.DrawTexturePro(renderer.sprite, src, dst, origin, transform.zRotation, Color.White);
        }
        else
            Raylib.DrawRectangleRec(collider.boxCollider, color);
    }

    public void ResetAge()
    {
        age = 0;
    }
}
