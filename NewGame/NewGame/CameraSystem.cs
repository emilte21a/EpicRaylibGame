using Camera2D = Raylib_cs.Camera2D;

public class CameraSystem
{
    private Camera2D camera;
    private Vector2 position;
    private Vector2 target;

    public const int virtualScreenWidth = 640 / 2;
    public const int virtualScreenHeight = 360 / 2;

    private static float virtualRatio;

    public Camera2D pixelPerfectCamera;
    public RenderTexture2D pixelPerfectTargetTexture = Raylib.LoadRenderTexture(virtualScreenWidth, virtualScreenHeight);

    public Rectangle sourceRec;
    public Rectangle destRec;

    public enum CameraMode
    {
        still,
        followX,
        followY,
        followXY
    }

    private CameraMode cameraMode = CameraMode.followXY;

    private static CameraSystem? _instance;
    public static CameraSystem Instance => _instance ??= new CameraSystem();

    public CameraSystem()
    {
        camera = new Camera2D
        {
            Rotation = 0,
            Target = new Vector2(0, 0),
            Zoom = 0.7f
        };

        pixelPerfectCamera = new();
    }

    public void Update()
    {
        virtualRatio = (float)Game.windowWidth / virtualScreenWidth;

        switch (cameraMode)
        {
            case CameraMode.still:
                camera.Target = Vector2.Zero;
                break;

            case CameraMode.followXY:
                camera.Target = target;
                camera.Offset = new Vector2(
                    virtualScreenWidth / 2f,
                    virtualScreenHeight / 2f
                );
                break;
        }

        sourceRec = new Rectangle(
            0,
            0,
            pixelPerfectTargetTexture.Texture.Width,
            -pixelPerfectTargetTexture.Texture.Height
        );

        destRec = new Rectangle(
            0,
            0,
            Game.windowWidth,
            Game.windowHeight
        );

        pixelPerfectCamera.Target = camera.Target;
        pixelPerfectCamera.Offset = camera.Offset;
        pixelPerfectCamera.Zoom = camera.Zoom;
    }

    public void ChangeCameraModeTo(CameraMode newCameraMode)
    {
        cameraMode = newCameraMode;
    }

    public void SetCameraPosition(Vector2 newPosition)
    {
        position = newPosition;
    }

    public void SetTarget(Vector2 newTarget)
    {
        target = newTarget;
    }

    public void SetZoom(float newZoom)
    {
        camera.Zoom = newZoom;
    }

    public Vector2 GetTarget()
    {
        return camera.Target;
    }

    public Camera2D GetCamera()
    {
        return camera;
    }

    public Camera2D GetPixelPerfectCamera()
    {
        return pixelPerfectCamera;
    }

    public Vector2 WorldToScreen(Vector2 worldPos)
    {
        return Raylib.GetWorldToScreen2D(worldPos, camera);
    }

    public Vector2 GetMouseWorldPosition()
    {
        Vector2 mouseScreen = Raylib.GetMousePosition();

        float scaleX = (float)virtualScreenWidth / Game.windowWidth;
        float scaleY = (float)virtualScreenHeight / Game.windowHeight;

        Vector2 mouseVirtual = new Vector2(
            mouseScreen.X * scaleX,
            mouseScreen.Y * scaleY
        );

        return Raylib.GetScreenToWorld2D(mouseVirtual, camera);
    }
}