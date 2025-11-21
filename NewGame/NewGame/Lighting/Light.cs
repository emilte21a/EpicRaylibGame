public struct Light
{
    private Vector2 position = Vector2.Zero;
    private int brightness;
    private Color color;

    public Light(Vector2 position, int brightness, Color color)
    {
        this.position = position;
        this.brightness = brightness;
        this.color = color;

        this.brightness = (int)Raymath.Clamp(this.brightness, 0, Core.MAX_BRIGHTNESS);
    }

    public Vector2 GetPosition()
    {
        return position;
    }

    public int GetBrightness()
    {
        return brightness;
    }

    public void SetBrightness(int newBrightness)
    {
        brightness = newBrightness;
    }

    public Color GetColor()
    {
        return color;
    }

    public void SetColor(Color newColor)
    {
        color = newColor;
    }
}