public interface ISlotContainer
{
    IEnumerable<Slot> Slots { get; }
    bool IsOpen();
}