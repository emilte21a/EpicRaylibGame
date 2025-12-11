public abstract class UserInterface : GameObject
{
    public bool isHovering = false;
    protected Rectangle interactionPanel;
    protected bool shouldOpen = false;
    public override void Start()
    {
        base.Start();
        tag = "UserInterface";
    }

    public override void Update()
    {
        base.Update();
        if (Raylib.CheckCollisionPointRec(Raylib.GetMousePosition(), interactionPanel))
        {
            isHovering = true;
        }
    }

    public void Open()
    {
        shouldOpen = true;
        Console.WriteLine("Open interface");
    }

    public void Close()
    {
        shouldOpen = false;
        Console.WriteLine("Closed Interface");

    }

    public bool IsOpen()
    {
        return shouldOpen;
    }
}