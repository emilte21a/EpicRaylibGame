public static class UIDragContext
{
    public static bool isDragging = false;
    public static Item? draggedItem = null;
    public static int draggedCount = 0;
    public static Slot? originSlot = null;


    public static void Reset()
    {
        isDragging = false;
        draggedItem = null;
        draggedCount = 0;
        originSlot = null;
    }
}