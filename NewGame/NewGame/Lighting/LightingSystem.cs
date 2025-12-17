using Raylib_cs;
using System.Numerics;
using System.Collections.Generic;
using BlendMode = Raylib_cs.BlendMode;
using Camera2D = Raylib_cs.Camera2D;
using TextureFilter = Raylib_cs.TextureFilter;
using Shader = Raylib_cs.Shader;
using ShaderUniformDataType = Raylib_cs.ShaderUniformDataType;
using Image = Raylib_cs.Image;

public class LightingSystem
{
    private static LightingSystem? _instance;
    public static LightingSystem Instance => _instance ??= new LightingSystem();

    private RenderTexture2D lightingTexture;

    // Use a small value struct instead of tuple for dictionary values
    private struct LightValue
    {
        public int r, g, b, total;
        public LightValue(int r, int g, int b, int total) { this.r = r; this.g = g; this.b = b; this.total = total; }
    }

    private struct LightNode
    {
        public int x, y, light;
        public Color color;
        public LightNode(int x, int y, int light, Color color) { this.x = x; this.y = y; this.light = light; this.color = color; }
    }

    private Dictionary<(int x, int y), LightValue> globalLightMap = new Dictionary<(int, int), LightValue>(4096);
    private Dictionary<(int x, int y), Color> computedLightmap = new Dictionary<(int, int), Color>(4096);

    // reusable BFS queue
    private Queue<LightNode> bfsQueue = new Queue<LightNode>(8192);

    private const int MAX_QUEUE_SIZE = 100000;

    Shader kawaseShader;
    RenderTexture2D blurA;
    RenderTexture2D blurB;

    int uResolutionLoc;
    int uOffsetLoc;

    // static direction arrays so we don't allocate them every frame
    private static readonly int[] dx = [-1, 1, 0, 0];
    private static readonly int[] dy = [0, 0, -1, 1];

    RenderTexture2D godrayTexture;

    // occlusion (blocking) render target
    RenderTexture2D occlusionTexture;

    // godray shader & uniform locations
    Shader godrayOcclusionShader;
    int uLightPosLoc, uExposureLoc, uDecayLoc, uDensityLoc, uWeightLoc, uSamplesLoc;
    int uSceneTexLoc, uOccTexLoc;

    private LightingSystem() { }

    public void Initialize()
    {
        var pp = CameraSystem.Instance.pixelPerfectTargetTexture;
        lightingTexture = Raylib.LoadRenderTexture(pp.Texture.Width, pp.Texture.Height);
        Raylib.SetTextureFilter(lightingTexture.Texture, TextureFilter.Point);

        // Load Kawase blur shader
        kawaseShader = Raylib.LoadShader(null, "Shaders/kawase_blur.fs");
        Console.WriteLine("Shader Loaded? " + (kawaseShader.Id != 0));

        uResolutionLoc = Raylib.GetShaderLocation(kawaseShader, "resolution");
        uOffsetLoc = Raylib.GetShaderLocation(kawaseShader, "offset");

        Vector2 res = new(pp.Texture.Width, pp.Texture.Height);
        Raylib.SetShaderValue(kawaseShader, uResolutionLoc, res, ShaderUniformDataType.Vec2);

        // Create two ping-pong blur buffers
        blurA = Raylib.LoadRenderTexture(pp.Texture.Width, pp.Texture.Height);
        blurB = Raylib.LoadRenderTexture(pp.Texture.Width, pp.Texture.Height);

        // Occlusion target
        occlusionTexture = Raylib.LoadRenderTexture(pp.Texture.Width, pp.Texture.Height);
        godrayTexture = Raylib.LoadRenderTexture(pp.Texture.Width, pp.Texture.Height);
        // Godray shader that uses occlusion
        godrayOcclusionShader = Raylib.LoadShader(null, "Shaders/godrays_occlusion.fs");

        // Uniform locations (some backends use names; Raylib returns location ints)
        uSceneTexLoc = Raylib.GetShaderLocation(godrayOcclusionShader, "sceneTex");
        uOccTexLoc = Raylib.GetShaderLocation(godrayOcclusionShader, "occlusionTex");
        uLightPosLoc = Raylib.GetShaderLocation(godrayOcclusionShader, "lightPos");
        uExposureLoc = Raylib.GetShaderLocation(godrayOcclusionShader, "exposure");
        uDecayLoc = Raylib.GetShaderLocation(godrayOcclusionShader, "decay");
        uDensityLoc = Raylib.GetShaderLocation(godrayOcclusionShader, "density");
        uWeightLoc = Raylib.GetShaderLocation(godrayOcclusionShader, "weight");
        uSamplesLoc = Raylib.GetShaderLocation(godrayOcclusionShader, "samples");
    }

    public void Update()
    {
        // Recompute the globalLightMap from scratch each frame for now.
        // (Later: maintain dirty chunk set and only update changed chunks.)
        globalLightMap.Clear();
        bfsQueue.Clear();

        var visibleChunks = WorldGeneration.Instance.visibleChunks;
        var chunkMap = WorldGeneration.Instance.chunkMap;

        // 1) SKY LIGHT
        float skylightBrightness = (int)(Core.MAX_BRIGHTNESS * DayNightSystem.Instance.GetSkyLightMultiplier());

        foreach (var chunkIndex in visibleChunks)
        {
            if (!chunkMap.TryGetValue(chunkIndex, out var chunk)) continue;

            int chunkTilesWide = Core.CHUNK_SIZE;
            int chunkTileStartX = chunkIndex.Item1 * Core.CHUNK_SIZE;

            for (int lx = 0; lx < chunkTilesWide; lx++)
            {
                int gx = chunkTileStartX + lx;

                if (!WorldGeneration.Instance.GetSurfaceIndexAtWorldX(gx * Core.UNIT_SIZE, out int? surfaceTileAtX) || surfaceTileAtX == null)
                    continue;

                for (int gy = 0; gy <= surfaceTileAtX.Value; gy++)
                {
                    var key = (gx, gy);

                    if (chunk.tileMap.TryGetValue(key, out var t) && t.blocksLight)
                        break;

                    var val = new LightValue(255, 255, 255, (int)skylightBrightness);
                    globalLightMap[key] = val;
                    bfsQueue.Enqueue(new LightNode(gx, gy, (int)skylightBrightness, Color.White));
                }
            }
        }

        var particles = ParticlePool.ActiveParticles;
        for (int i = 0; i < particles.Count; i++)
        {
            var p = particles[i];
            if (p == null || !p.isActive || p.lightSource == null || p.lightSource.light.GetBrightness() <= 0) continue;

            int gx = (int)MathF.Floor(p.transform.position.X / Core.UNIT_SIZE);
            int gy = (int)MathF.Floor(p.transform.position.Y / Core.UNIT_SIZE);
            int brightness = p.lightSource.light.GetBrightness();
            var color = p.lightSource.light.GetColor();

            var key = (gx, gy);
            globalLightMap[key] = new LightValue(color.R, color.G, color.B, brightness);
            bfsQueue.Enqueue(new LightNode(gx, gy, brightness, color));
        }

        while (bfsQueue.Count > 0 && bfsQueue.Count < MAX_QUEUE_SIZE)
        {
            var node = bfsQueue.Dequeue();
            int x = node.x, y = node.y;
            int light = node.light;
            Color color = node.color;

            if (light <= 1) continue;

            for (int d = 0; d < 4; d++)
            {
                int nx = x + dx[d], ny = y + dy[d];
                var nkey = (nx, ny);

                int chunkX = (int)MathF.Floor((float)nx / Core.CHUNK_SIZE);
                int chunkY = (int)MathF.Floor((float)ny / Core.CHUNK_SIZE);
                var neighborIdx = (chunkX, chunkY);

                if (!chunkMap.TryGetValue(neighborIdx, out var neighborChunk))
                    continue;

                bool blocks = false;
                if (neighborChunk.tileMap.TryGetValue(nkey, out var neighborTile) && neighborTile.blocksLight)
                    blocks = true;

                int propagation = blocks ? 10 : 2;
                int newLight = light - propagation;
                if (newLight <= 0) continue;

                if (!globalLightMap.TryGetValue(nkey, out var cur) || newLight > cur.total)
                {
                    float factor = newLight / (float)Core.MAX_BRIGHTNESS;
                    int rr = (int)Math.Clamp(color.R * factor, 0, 255);
                    int gg = (int)Math.Clamp(color.G * factor, 0, 255);
                    int bb = (int)Math.Clamp(color.B * factor, 0, 255);
                    globalLightMap[nkey] = new LightValue(rr, gg, bb, newLight);
                    bfsQueue.Enqueue(new LightNode(nx, ny, newLight, color));
                }
            }
        }
    }

    public void ComputeLighting()
    {
        computedLightmap.Clear();

        var visibleChunks = WorldGeneration.Instance.visibleChunks;
        var chunkMap = WorldGeneration.Instance.chunkMap;

        foreach (var chunkIndex in visibleChunks)
        {
            if (!chunkMap.TryGetValue(chunkIndex, out var chunk)) continue;

            int chunkStartX = chunkIndex.Item1 * Core.CHUNK_SIZE;
            int chunkStartY = chunkIndex.Item2 * Core.CHUNK_SIZE;

            for (int lx = 0; lx < Core.CHUNK_SIZE; lx++)
            {
                for (int ly = 0; ly < Core.CHUNK_SIZE; ly++)
                {
                    int gx = chunkStartX + lx;
                    int gy = chunkStartY + ly;
                    var key = (gx, gy);
                    if (!globalLightMap.TryGetValue(key, out var l))
                        computedLightmap[key] = Color.Black;
                    else
                    {
                        if (l.total <= 0)
                        {
                            computedLightmap[key] = Color.Black;
                        }
                        else
                        {
                            float brightness = l.total / (float)Core.MAX_BRIGHTNESS;
                            computedLightmap[key] = new Color(
                                (byte)Math.Clamp(l.r * brightness, 0, 255),
                                (byte)Math.Clamp(l.g * brightness, 0, 255),
                                (byte)Math.Clamp(l.b * brightness, 0, 255),
                                (byte)255
                            );
                        }
                    }
                }
            }
        }
    }

    public void RenderLightingTexture()
    {
        Raylib.BeginTextureMode(lightingTexture);
        Raylib.ClearBackground(Color.Blank);

        Camera2D camera = CameraSystem.Instance.GetCamera();

        float zoom = camera.Zoom;
        int texW = lightingTexture.Texture.Width;
        int texH = lightingTexture.Texture.Height;

        var visibleChunks = WorldGeneration.Instance.visibleChunks;
        var chunkMap = WorldGeneration.Instance.chunkMap;

        foreach (var chunkIndex in visibleChunks)
        {
            if (!chunkMap.TryGetValue(chunkIndex, out var chunk)) continue;

            int chunkStartX = chunkIndex.Item1 * Core.CHUNK_SIZE;
            int chunkStartY = chunkIndex.Item2 * Core.CHUNK_SIZE;

            for (int lx = 0; lx < Core.CHUNK_SIZE; lx++)
            {
                for (int ly = 0; ly < Core.CHUNK_SIZE; ly++)
                {
                    int gx = chunkStartX + lx;
                    int gy = chunkStartY + ly;
                    var key = (gx, gy);
                    Color col = computedLightmap.TryGetValue(key, out Color c) ? c : Color.Black;

                    float wx = gx * Core.UNIT_SIZE;
                    float wy = gy * Core.UNIT_SIZE;

                    float sx = (wx - camera.Target.X) * zoom + camera.Offset.X;
                    float sy = (wy - camera.Target.Y) * zoom + camera.Offset.Y;

                    float tileScreenW = Core.UNIT_SIZE * zoom;
                    float tileScreenH = Core.UNIT_SIZE * zoom;

                    if (sx + tileScreenW < 0 || sx > texW) continue;
                    if (sy + tileScreenH < 0 || sy > texH) continue;

                    Raylib.DrawRectangle(
                        (int)sx,
                        (int)sy,
                        (int)Math.Ceiling(tileScreenW),
                        (int)Math.Ceiling(tileScreenH),
                        col
                    );
                }
            }
        }

        Raylib.BeginBlendMode(BlendMode.Additive);
        Raylib.BeginMode2D(camera);

        foreach (var p in ParticlePool.ActiveParticles)
        {
            if (p == null || !p.isActive) continue;

            int brightness = p.lightSource?.light.GetBrightness() ?? 0;
            if (brightness <= 0) continue;

            float radius = brightness;

            byte alpha = (byte)Math.Clamp((1 - p.age) * 255, 0, 255);
            Color col = new Color(p.lightColor.R, p.lightColor.G, p.lightColor.B, alpha);

            Raylib.DrawCircleGradient((int)(p.transform.position.X + p.GetSize() / 2), (int)(p.transform.position.Y + p.GetSize() / 2), radius, col, Color.Blank);
        }

        Raylib.EndMode2D();
        Raylib.EndBlendMode();

        Raylib.EndTextureMode();
    }

    public void PerformKawaseRenderPass()
    {
        KawaseBlurPass(lightingTexture, blurA, 1.5f);
        KawaseBlurPass(blurA, blurB, 1.5f);
    }

    public void Draw()
    {
        Raylib.BeginBlendMode(BlendMode.Multiplied);
        Raylib.DrawTexturePro(
            blurB.Texture,
            new Rectangle(0, 0, blurB.Texture.Width, -blurB.Texture.Height),
            new Rectangle(0, 0, blurB.Texture.Width, blurB.Texture.Height),
            Vector2.Zero,
            0f,
            Color.White
        );

        Raylib.EndBlendMode();

        Raylib.BeginBlendMode(BlendMode.Additive);

        Raylib.DrawTexturePro(
            godrayTexture.Texture,
            new Rectangle(0, 0, godrayTexture.Texture.Width, -godrayTexture.Texture.Height),
            new Rectangle(0, 0, godrayTexture.Texture.Width, godrayTexture.Texture.Height),
            Vector2.Zero,
            0f,
            Color.White
        );

        Raylib.EndBlendMode();
    }

    public void KawaseBlurPass(RenderTexture2D src, RenderTexture2D dst, float offset)
    {
        Vector2 res = new(src.Texture.Width, src.Texture.Height);
        Raylib.SetShaderValue(kawaseShader, uResolutionLoc, res, ShaderUniformDataType.Vec2);
        Raylib.SetShaderValue(kawaseShader, uOffsetLoc, offset, ShaderUniformDataType.Float);

        Raylib.BeginTextureMode(dst);
        Raylib.ClearBackground(Color.Blank);
        Raylib.BeginShaderMode(kawaseShader);

        Raylib.DrawTexturePro(
            src.Texture,
            new Rectangle(0, 0, src.Texture.Width, -src.Texture.Height),
            new Rectangle(0, 0, dst.Texture.Width, dst.Texture.Height),
            Vector2.Zero,
            0f,
            Color.White
        );

        Raylib.EndShaderMode();
        Raylib.EndTextureMode();
    }

    public void RenderOccluders()
    {
        Raylib.BeginTextureMode(occlusionTexture);
        Raylib.ClearBackground(Color.Black);

        Camera2D cam = CameraSystem.Instance.GetCamera();
        Raylib.BeginMode2D(cam);

        foreach (var chunkIndex in WorldGeneration.Instance.visibleChunks)
        {
            if (!WorldGeneration.Instance.chunkMap.TryGetValue(chunkIndex, out var chunk)) continue;
            int chunkStartX = chunkIndex.Item1 * Core.CHUNK_SIZE;
            int chunkStartY = chunkIndex.Item2 * Core.CHUNK_SIZE;

            for (int lx = 0; lx < Core.CHUNK_SIZE; lx++)
                for (int ly = 0; ly < Core.CHUNK_SIZE; ly++)
                {
                    int gx = chunkStartX + lx;
                    int gy = chunkStartY + ly;
                    var key = (gx, gy);
                    if (chunk.tileMap.TryGetValue(key, out var tile) && tile.blocksLight)
                    {
                        float wx = gx * Core.UNIT_SIZE;
                        float wy = gy * Core.UNIT_SIZE;
                        Raylib.DrawRectangle((int)wx, (int)wy, Core.UNIT_SIZE, Core.UNIT_SIZE, Color.White);
                    }
                    if (chunk.backgroundTileMap.TryGetValue(key, out var bgTile))
                    {
                        float wx = gx * Core.UNIT_SIZE;
                        float wy = gy * Core.UNIT_SIZE;

                        Raylib.DrawRectangle((int)wx, (int)wy, Core.UNIT_SIZE, Core.UNIT_SIZE, Color.White);
                    }
                }
        }

        ParallaxHandler.DrawOcclusionMask();

        Raylib.EndMode2D();

        Raylib.EndTextureMode();
    }

    public void RenderGodRaysOcclusion(Vector2 lightScreenPosition)
    {
        Raylib.BeginTextureMode(godrayTexture);
        Raylib.ClearBackground(Color.Black);

        Raylib.BeginShaderMode(godrayOcclusionShader);

        // Normalized light pos [0..1]
        Vector2 light01 = new(lightScreenPosition.X / godrayTexture.Texture.Width,
                              lightScreenPosition.Y / godrayTexture.Texture.Height);

        Raylib.SetShaderValue(godrayOcclusionShader, uLightPosLoc, light01, ShaderUniformDataType.Vec2);

        // Control params
        Raylib.SetShaderValue(godrayOcclusionShader, uExposureLoc, 0.6f, ShaderUniformDataType.Float);
        Raylib.SetShaderValue(godrayOcclusionShader, uDecayLoc, 0.96f, ShaderUniformDataType.Float);
        Raylib.SetShaderValue(godrayOcclusionShader, uDensityLoc, 0.9f, ShaderUniformDataType.Float);
        Raylib.SetShaderValue(godrayOcclusionShader, uWeightLoc, 0.8f, ShaderUniformDataType.Float);
        int sampleCount = 64;
        Raylib.SetShaderValue(godrayOcclusionShader, uSamplesLoc, sampleCount, ShaderUniformDataType.Int);

        // Bind input textures
        Raylib.SetShaderValueTexture(godrayOcclusionShader, uSceneTexLoc, lightingTexture.Texture);
        Raylib.SetShaderValueTexture(godrayOcclusionShader, uOccTexLoc, occlusionTexture.Texture);

        // Draw scene in shader
        Raylib.DrawTexturePro(
            lightingTexture.Texture,
            new Rectangle(0, 0, lightingTexture.Texture.Width, -lightingTexture.Texture.Height),
            new Rectangle(0, 0, godrayTexture.Texture.Width, godrayTexture.Texture.Height),
            Vector2.Zero,
            0f,
            Color.White
        );

        Raylib.EndShaderMode();
        Raylib.EndTextureMode();
    }

    public void RenderAll()
    {
        ComputeLighting();
        RenderLightingTexture();
        RenderOccluders();
        Vector2 sunScreenPos = DayNightSystem.Instance.GetSunScreenPosition(); // implement to return pixel coord
        Vector2 sunForShader = new(sunScreenPos.X, DayNightSystem.Instance.sunRenderTexture.Texture.Height - sunScreenPos.Y);
        RenderGodRaysOcclusion(sunForShader);
        DayNightSystem.Instance.RenderSun();

        PerformKawaseRenderPass();
    }
}
