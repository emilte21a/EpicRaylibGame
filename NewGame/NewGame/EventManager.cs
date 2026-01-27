
public class EventManager
{
    private static EventManager? _instance;
    public static EventManager Instance => _instance ??= new EventManager();
    private List<Action> actions = [];


    public void Update()
    {
        if (actions.Count > 0)
        {
            actions.ForEach(a => a.Invoke());
            actions.Clear();
        }
    }

    public void AddAction(Action action)
    {
        if (!actions.Contains(action))
            actions.Add(action);
    }
}