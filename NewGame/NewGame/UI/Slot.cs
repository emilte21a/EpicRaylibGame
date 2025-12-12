public abstract class Slot
{
    public Item? itemInSlot;
    public Rectangle rectangle;
    public int slotSize = Core.UI_SLOTSIZE;
    public Color color = Color.Black;
    public bool isHovered = false;
    public int amount;
    private Texture2D slotFrame;
    public UserInterface? owner;

    public Slot(Item? itemInSlot)
    {
        this.itemInSlot = itemInSlot;
        rectangle = new Rectangle(0, 0, slotSize, slotSize);
        slotFrame = TextureManager.LoadTexture("Textures/slotframe.png");
    }

    public virtual void Update()
    {
        if (owner != null && (!owner.IsOpen() || !owner.isHovering))
        {
            isHovered = false;
            color = Color.Black;
            return;
        }

        Vector2 mousePos = Raylib.GetMousePosition();
        if (Raylib.CheckCollisionPointRec(mousePos, rectangle))
        {
            isHovered = true;
            color = new Color(0, 0, 0, 200);
        }
        else
        {
            color = Color.Black;
            isHovered = false;
        }
    }

    public void Draw()
    {
        if (owner != null && !owner.IsOpen())
            return;

        Raylib.DrawRectangle((int)rectangle.X, (int)rectangle.Y, (int)rectangle.Width, (int)rectangle.Height, color);
        DrawFrame();
        if (itemInSlot != null)
        {
            Raylib.DrawTexturePro(itemInSlot.texture,
            new Rectangle(0, 0, itemInSlot.texture.Width, itemInSlot.texture.Height),
            new Rectangle(rectangle.X + slotSize / 4, rectangle.Y + slotSize / 4,
            slotSize / 2, slotSize / 2)
            , Vector2.Zero, 0, Color.White);
            if (amount > 0)
                Raylib.DrawText($"{amount}", (int)(rectangle.X + 10), (int)(rectangle.Y + 10), 30, Color.White);
        }

    }

    void DrawFrame()
    {
        Raylib.DrawTexturePro(slotFrame, new Raylib_cs.Rectangle(0, 0, slotFrame.Width, slotFrame.Height), new Raylib_cs.Rectangle(rectangle.X, rectangle.Y, slotSize, slotSize), Vector2.Zero, 0, Color.White);
    }

    public void SetSlotFrame(string newTexturePath)
    {
        slotFrame = TextureManager.LoadTexture(newTexturePath);
    }
}

public class InventorySlot : Slot
{
    public int index;

    public InventorySlot(Item? itemInSlot) : base(itemInSlot)
    {
    }
}

public class FurnaceSlot : Slot
{
    public inputType inputType;
    public List<short> acceptedInputTypes = new List<short>();

    public FurnaceSlot(Item? itemInSlot) : base(itemInSlot)
    {
    }
}

public class ResultSlot : Slot
{
    public ResultSlot(Item? itemInSlot) : base(itemInSlot)
    {
    }
}

public class CraftingSlot : Slot
{
    public CraftingRecipe craftingRecipe;

    public CraftingSlot(CraftingRecipe craftingRecipe, Item? itemInSlot = null) : base(itemInSlot)
    {
        this.craftingRecipe = craftingRecipe;
    }

    public override void Update()
    {
        if (owner != null && !owner.IsOpen())
        {
            isHovered = false;
            color = Color.Black;
            return;
        }
        Vector2 mousePos = Raylib.GetMousePosition();
        if (Raylib.CheckCollisionPointRec(mousePos, rectangle))
        {
            isHovered = true;
            color = new Color(0, 0, 0, 200);
        }
        else
        {
            color = Color.Black;
            isHovered = false;
        }
    }
}
