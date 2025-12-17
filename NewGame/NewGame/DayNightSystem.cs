public class DayNightSystem
{
    public RenderTexture2D sunRenderTexture = Raylib.LoadRenderTexture(
        CameraSystem.Instance.pixelPerfectTargetTexture.Texture.Width,
        CameraSystem.Instance.pixelPerfectTargetTexture.Texture.Height
    );

    public RenderTexture2D sunBloomTextureA = Raylib.LoadRenderTexture(
        CameraSystem.Instance.pixelPerfectTargetTexture.Texture.Width,
        CameraSystem.Instance.pixelPerfectTargetTexture.Texture.Height
    );
    public RenderTexture2D sunBloomTextureB = Raylib.LoadRenderTexture(
        CameraSystem.Instance.pixelPerfectTargetTexture.Texture.Width,
        CameraSystem.Instance.pixelPerfectTargetTexture.Texture.Height
    );

    public static float TimeScale = 60f;
    private float timeOfDay = 12;
    public float timePerDay = 24 * TimeScale;

    private Vector2 sunPos;
    private static DayNightSystem? _instance;
    public static DayNightSystem Instance => _instance ??= new DayNightSystem();

    public void Update()
    {
        timePerDay = 24 * TimeScale;
        timeOfDay += Raylib.GetFrameTime() * (24f / timePerDay);

        if (timeOfDay >= timePerDay)
            timeOfDay -= timePerDay;
    }

    public void RenderSun()
    {
        Raylib.BeginTextureMode(sunRenderTexture);
        Raylib.ClearBackground(Color.Blank);
        Raylib.BeginBlendMode(Raylib_cs.BlendMode.Additive);

        // Draw sun if daytime
        if (timeOfDay >= 6f && timeOfDay <= 19f)
        {
            sunPos = GetSunScreenPosition();
            Raylib.DrawCircleV(sunPos, 10, Color.Orange);
        }

        // Draw moon if nighttime
        if (timeOfDay < 6f || timeOfDay > 19f)
        {
            Vector2 moonPos = GetMoonScreenPosition();
            Raylib.DrawCircleV(moonPos, 10, Color.White);
        }
        Raylib.EndBlendMode();
        Raylib.EndTextureMode();

        LightingSystem.Instance.KawaseBlurPass(sunRenderTexture, sunBloomTextureA, 2f);
        LightingSystem.Instance.KawaseBlurPass(sunBloomTextureB, sunBloomTextureB, 2f);
    }

    public void DrawSkyBackground()
    {
        var tex = CameraSystem.Instance.pixelPerfectTargetTexture.Texture;
        Color skyColor;
        if (timeOfDay >= 6 && timeOfDay <= 19)
            skyColor = Color.SkyBlue; //LerpColor(Color.Black, Color.SkyBlue, (timeOfDay - 6) / 6);

        else
        {
            float t;
            if (timeOfDay >= 19) t = (timeOfDay - 19) / 5;
            else t = (timeOfDay - 6) / 6;
            skyColor = Color.Black; //LerpColor(Color.Black, Color.SkyBlue, t);
        }

        Raylib.ClearBackground(skyColor);
    }

    public Vector2 GetSunScreenPosition()
    {
        var tex = CameraSystem.Instance.pixelPerfectTargetTexture.Texture;
        Vector2 center = new(tex.Width / 2f, tex.Height / 2f);
        float radius = tex.Height * 0.5f;

        // Map sun only from 6:00 → 19:00 → angle 0 → π
        float t = (timeOfDay - 6f) / (19f - 6f); // 0 → 1
        t = Math.Clamp(t, 0f, 1f);
        float angle = (1 - t) * MathF.PI; // 0 = sunrise, π = sunset

        return new Vector2(
            center.X + MathF.Cos(angle) * radius,
            center.Y - MathF.Sin(angle) * radius
        );
    }

    public Vector2 GetMoonScreenPosition()
    {
        var tex = CameraSystem.Instance.pixelPerfectTargetTexture.Texture;
        Vector2 center = new(tex.Width / 2f, tex.Height / 2f);
        float radius = tex.Height * 0.5f;

        // Moon visible from 19:00 → 6:00 (wrap around)
        float t;
        if (timeOfDay > 19f)
            t = (timeOfDay - 19f) / (24f - 19f + 6f); // 19..24 → 0..1
        else
            t = (timeOfDay + 5f) / (24f - 19f + 6f);  // 0..6 → 1..0

        t = Math.Clamp(t, 0f, 1f);
        float angle = (1 - t) * MathF.PI; // 0 = moonrise, π = moonset

        return new Vector2(
            center.X + MathF.Cos(angle) * radius,
            center.Y - MathF.Sin(angle) * radius
        );
    }

    public float GetSkyLightMultiplier()
    {
        if (timeOfDay >= 6f && timeOfDay <= 19f)
            return 1;
        else
            return 0.25f;
    }

    public void Draw()
    {
        Raylib.DrawTexturePro(sunRenderTexture.Texture,
        new Rectangle(0, 0, sunRenderTexture.Texture.Width, -sunRenderTexture.Texture.Height),
        new Rectangle(0, 0, sunRenderTexture.Texture.Width, sunRenderTexture.Texture.Height), Vector2.Zero, 0, Color.White);
        Raylib.BeginBlendMode(Raylib_cs.BlendMode.Additive);
        Raylib.DrawTexturePro(sunBloomTextureB.Texture,
        new Rectangle(0, 0, sunBloomTextureB.Texture.Width, -sunBloomTextureB.Texture.Height),
        new Rectangle(0, 0, sunBloomTextureB.Texture.Width, sunBloomTextureB.Texture.Height), Vector2.Zero, 0, new Color(255,255,255,120));
        Raylib.EndBlendMode();
    }

    public float GetCurrentTime()
    {
        return timeOfDay;
    }

    public Color LerpColor(Color c1, Color c2, float t)
    {
        var col = new Color(Raymath.Lerp(c1.R, c2.R, t),
                            Raymath.Lerp(c1.G, c2.G, t),
                            Raymath.Lerp(c1.B, c2.B, t));
        return col;
    }
}
