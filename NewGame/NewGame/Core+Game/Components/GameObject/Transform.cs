public class Transform : Component
{

    private Vector2 _position;

    public Vector2 position
    {
        get => _position;
        set
        {
            _position = value;
            x = value.X;
            y = value.Y;
        }
    }

    public float x
    {
        get => _position.X;
        set => _position.X = value;
    }

    public float y
    {
        get => _position.Y;
        set => _position.Y = value;
    }

    public int z = 0;

    public int zRotation = 0;

    public void Translate(Vector2 delta)
    {
        x += delta.X;
        y += delta.Y;
    }

    public void SetPositionX(float newX)
    {
        x = newX;
    }

    public void SetPositionY(float newY)
    {
        y = newY;
    }

    public void SetZ(int newZ)
    {
        z = newZ;
    }
}