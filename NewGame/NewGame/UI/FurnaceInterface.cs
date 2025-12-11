using LibNoise.Renderer;

public class FurnaceInterface : UserInterface, ISlotContainer
{
    public FurnaceSlot fuelSlot = new FurnaceSlot(null);
    public FurnaceSlot inputSlot = new FurnaceSlot(null);
    public ResultSlot resultSlot = new ResultSlot(null);
    public FurnaceTile ownerTile = null;
    public IEnumerable<Slot> Slots => [inputSlot, fuelSlot, resultSlot];

    public FurnaceInterface()
    {
        Console.WriteLine("FurnaceInterface created " + GetHashCode());

        float panelW = 800;
        float panelH = 300;

        // center panel inside the game window
        interactionPanel = new Rectangle(
            (Game.screenWidth - panelW) / 2,
            (Game.screenHeight - panelH) / 2,
            panelW,
            panelH
        );

        // base Y row for slot layout
        float baseY = interactionPanel.Y + panelH / 2 - Core.UI_SLOTSIZE / 2;

        // X positions relative to the panel
        float xFuel = interactionPanel.X + 200;
        float xInput = xFuel + Core.UI_SLOTSIZE + 20;
        float xResult = xInput + (Core.UI_SLOTSIZE + 20) * 2;

        // place slots at centered positions
        fuelSlot.rectangle.Position = new Vector2(xFuel, baseY);
        inputSlot.rectangle.Position = new Vector2(xInput, baseY);
        resultSlot.rectangle.Position = new Vector2(xResult, baseY);

        fuelSlot.inputType = inputType.fuel;
        inputSlot.inputType = inputType.smeltable;

        foreach (var slot in Slots)
        {
            slot.owner = this;
        }

        fuelSlot.SetSlotFrame("Textures/fuelslotframe.png");
        inputSlot.SetSlotFrame("Textures/inputslotframe.png");

        SetupAcceptedSlotInputs();
    }

    public override void Start()
    {
        base.Start();
        tag = "Furnace interface";
    }

    public void SetupAcceptedSlotInputs()
    {
        //FUELSLOT
        fuelSlot.acceptedInputTypes.Add((short)ItemFactory.ItemID.coalore);

        //INPUTSLOT
        inputSlot.acceptedInputTypes.Add((short)ItemFactory.ItemID.copperore);
        inputSlot.acceptedInputTypes.Add((short)ItemFactory.ItemID.silverore);
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
        Raylib.DrawRectangleRec(interactionPanel, Core.UI_INTERACTION_PANEL_COLOR);
        Raylib.DrawRectangleLinesEx(interactionPanel, 1, Color.Black);
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
    }
}

public enum inputType
{
    fuel,
    smeltable
}