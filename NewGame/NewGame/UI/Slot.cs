public abstract class Slot
{
    public Item? itemInSlot;
    public Rectangle rectangle;
    public int slotSize = Core.UI_SLOTSIZE;
    public Color color = Color.Black;
    public bool isHovered = false;
    public int amount;
    private Texture2D slotFrame;

    // owner UI (set when the UI creates the slot)
    public UserInterface? owner;

    public Slot(Item? itemInSlot)
    {
        this.itemInSlot = itemInSlot;
        rectangle = new Rectangle(0, 0, slotSize, slotSize);
        slotFrame = TextureManager.LoadTexture("Textures/slotframe.png");
    }

    public void Update()
    {
        // if slot is owned by a UI that isn't open, skip behaviour
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

            if (Raylib.IsMouseButtonPressed(MouseButton.Left) && itemInSlot != null && !UIDragContext.isDragging)
            {
                UIDragContext.originSlot = this;
                UIDragContext.draggedItem = itemInSlot;
                UIDragContext.draggedCount = amount;
                UIDragContext.isDragging = true;
                itemInSlot = null;
                amount = 0;
            }
        }
        else
        {
            color = Color.Black;
            isHovered = false;
        }
    }

    public void Draw()
    {
        // do not draw if owner exists but is closed
        if (owner != null && !owner.IsOpen())
            return;

        Raylib.DrawRectangle((int)rectangle.X, (int)rectangle.Y, (int)rectangle.Width, (int)rectangle.Height, color);
        Raylib.DrawTexturePro(slotFrame, new Raylib_cs.Rectangle(0, 0, slotFrame.Width, slotFrame.Height), new Raylib_cs.Rectangle(rectangle.X, rectangle.Y, slotSize, slotSize), Vector2.Zero, 2, Color.White);
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
}

public class InventorySlot(Item? itemInSlot) : Slot(itemInSlot)
{
    public int index;
}

public class FurnaceSlot(Item? itemInSlot) : Slot(itemInSlot)
{
    public inputType inputType;
    public List<short> acceptedInputTypes = new List<short>();
}

public class ResultSlot(Item? itemInSlot) : Slot(itemInSlot)
{

}
