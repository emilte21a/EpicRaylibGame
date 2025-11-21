public class FurnaceInterface : UserInterface, ISlotContainer
{
    public FurnaceSlot fuelSlot = new FurnaceSlot(null);
    public FurnaceSlot inputSlot = new FurnaceSlot(null);
    public ResultSlot resultSlot = new ResultSlot(null);
    public FurnaceTile ownerTile = null;
    public IEnumerable<Slot> Slots => new Slot[] { inputSlot, fuelSlot, resultSlot };

    public FurnaceInterface()
    {
        Console.WriteLine("FurnaceInterface created " + GetHashCode());
        interactionPanel = new Raylib_cs.Rectangle(100, 100, 900, 600);
        fuelSlot.rectangle.Position = new Vector2(300, 300);
        inputSlot.rectangle.Position = new Vector2(300 + Core.UI_SLOTSIZE + 10, 300);
        resultSlot.rectangle.Position = new Vector2(300 + (Core.UI_SLOTSIZE + 10) * 3, 300);
        fuelSlot.inputType = inputType.fuel;
        inputSlot.inputType = inputType.smeltable;
        // bind slots to this UI so they only respond when UI is open
        fuelSlot.owner = this;
        inputSlot.owner = this;
        resultSlot.owner = this;

        SetupAcceptedSlotInputs();
    }

    public void SetupAcceptedSlotInputs()
    {
        //FUELSLOT
        fuelSlot.acceptedInputTypes.Add((short)ItemFactory.ItemID.coalore);

        //INPUTSLOT
        inputSlot.acceptedInputTypes.Add((short)ItemFactory.ItemID.copperore);
    }

    public override void Update()
    {
        base.Update();
        foreach (var slot in Slots)
        {
            slot.Update();
        }

        if (Raylib.IsMouseButtonReleased(MouseButton.Left))
        {
            Vector2 mousePos = Raylib.GetMousePosition();
            // if (Raylib.CheckCollisionPointRec(mousePos, fuelSlot.rectangle))
            //     SlotUtils.TryPlaceItemInSlot(fuelSlot);
            // else if (Raylib.CheckCollisionPointRec(mousePos, inputSlot.rectangle))
            //     SlotUtils.TryPlaceItemInSlot(inputSlot);
        }
    }

    public override void Draw()
    {
        base.Draw();
        Raylib.DrawRectangleRec(interactionPanel, new Color(0, 100, 150, 120));
        foreach (var slot in Slots)
        {
            slot.Draw();
        }
        if (SlotUtils.hoveredSlot != null && SlotUtils.hoveredSlot.itemInSlot != null && !string.IsNullOrEmpty(SlotUtils.hoveredSlot.itemInSlot.description))
        {
            Raylib.DrawText(SlotUtils.hoveredSlot.itemInSlot.description, (int)SlotUtils.hoveredSlot.rectangle.X, (int)SlotUtils.hoveredSlot.rectangle.Y - 20, 20, Color.White);
        }

        if (ownerTile != null)
        {
            Vector2 progressBarPosition = new Vector2(inputSlot.rectangle.X + inputSlot.rectangle.Width + 10, inputSlot.rectangle.Y + Core.UI_SLOTSIZE / 2);
            Vector2 progressBarSize = new Vector2(Core.UI_SLOTSIZE, 10);

            Raylib.DrawRectangleV(progressBarPosition, progressBarSize, Color.DarkGray);

            Raylib.DrawRectangle(
                (int)progressBarPosition.X,
                (int)progressBarPosition.Y,
                (int)(progressBarSize.X * ownerTile.GetComponent<FurnaceComponent>().GetSmeltProgressNormalized()),
                (int)progressBarSize.Y,
                Color.White
            );

            Vector2 fuelBarPosition = new Vector2(fuelSlot.rectangle.X - 20, fuelSlot.rectangle.Y);
            Vector2 fuelBarSize = new Vector2(10, Core.UI_SLOTSIZE);

            Raylib.DrawRectangleV(fuelBarPosition, fuelBarSize, Color.Orange);
            Raylib.DrawRectangle(
               (int)fuelBarPosition.X,
               (int)fuelBarPosition.Y,
               (int)fuelBarSize.X,
               (int)(fuelBarSize.Y * (1 - ownerTile.GetComponent<FurnaceComponent>().GetFuelProgressNormalized())),
               Color.DarkGray
           );
        }

        Raylib.DrawRectangleLinesEx(fuelSlot.rectangle, 2, Color.Red);
        Raylib.DrawRectangleLinesEx(inputSlot.rectangle, 2, Color.Green);
    }
}



public enum inputType
{
    fuel,
    smeltable
}