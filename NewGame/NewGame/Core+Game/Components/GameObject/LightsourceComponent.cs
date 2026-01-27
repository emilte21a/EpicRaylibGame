using SharpNoise.Modules;

public class Lightsource : Component
{
    public Light light;

    private int brightness = Core.MAX_BRIGHTNESS;
    public Color color = Color.White;

    public override void Start()
    {
        light = new Light(parent.transform.position, brightness, color);
    }

    public override void Update()
    {
        base.Update();
        light.SetColor(color);
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        light.SetBrightness(0);
    }

    
}