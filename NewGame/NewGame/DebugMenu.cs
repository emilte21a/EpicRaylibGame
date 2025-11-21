public class DebugMenu
{
    private static DebugMenu? _instance;
    public static DebugMenu Instance => _instance ??= new DebugMenu();

    public Player? playerRef;


    KeyboardKey keyToOpenDebug = KeyboardKey.F3;
    private bool isOpen = false;

    public void UpdateDebug()
    {
        if (Raylib.IsKeyPressed(keyToOpenDebug))
            isOpen = !isOpen;

        if (isOpen)
        {
            //Lägg till saker som ska kunna ändras eller visas här
            if (playerRef != null)
                Raylib.DrawText($"{playerRef.transform.position}", 10, 300, 20, Raylib_cs.Color.Black);
        }
    }


}