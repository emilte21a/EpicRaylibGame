public abstract class UserInterface : GameObject
{
    public bool isHovering = false;
    protected Rectangle interactionPanel;
    protected bool shouldOpen = false;
    

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
        System.Console.WriteLine("Open interface");
    }

    public void Close()
    {
        shouldOpen = false;
        System.Console.WriteLine("Closed Interface");
        
    }

    public bool IsOpen()
    {
        return shouldOpen;
    }
}