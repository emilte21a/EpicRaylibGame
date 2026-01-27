public class Animator : Component
{
    private int _frame;
    private float _timer;
    private int _maxTime = 2;

    public void PlayAnimation(Texture2D spriteSheet, int direction, int maxFrames, Vector2 position, Rectangle dest)
    {
        _timer += Raylib.GetFrameTime() * 10;

        if (_timer >= _maxTime)
        {
            _timer = 0;
            _frame++;
        }
        _frame %= maxFrames;

        var src = new Rectangle(_frame * spriteSheet.Width / maxFrames, 0, spriteSheet.Width / maxFrames * direction, spriteSheet.Height);
        
        Raylib.DrawTexturePro(spriteSheet,src, dest, Vector2.Zero,0,Color.White);
    }
}