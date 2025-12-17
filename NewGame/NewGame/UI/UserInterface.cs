public abstract class UserInterface : GameObject
{
    public bool isHovering = false;
    protected Rectangle interactionPanel;
    protected bool isOpen = false;
    protected string name;
    public override void Start()
    {
        base.Start();
        tag = "UserInterface";
    }

    public override void Update()
    {
        base.Update();
                                  
        isHovering = Raylib.CheckCollisionPointRec(Raylib.GetMousePosition(), interactionPanel);
    }

    public void Open()
    {
        isOpen = true;
        Console.WriteLine("Open interface");
    }

    public void Close()
    {
        isOpen = false;
        Console.WriteLine("Closed Interface");

    }

    public bool IsOpen()
    {
        return isOpen;
    }
}