public class UIElement : GameObject
{
    protected Rectangle rectangle;
    protected bool hovered => IsHovered();
    public int width = 100;
    public int height = 50;

    Vector2 parentPos = new Vector2(Game.screenWidth, Game.screenHeight);

    public enum PositionOnScreen
    {
        TOP,
        LEFT,
        RIGHT,
        MIDDLE,
        BOTTOM
    }

    public UIElement(int width, int height, PositionOnScreen xAlign, PositionOnScreen yAlign)
    {
        this.width = width;
        this.height = height;
        var pos = GetPositionFromAlignment(xAlign, yAlign);
        rectangle = new Rectangle(pos.X, pos.Y, width, height);
    }

    public override void Update()
    {
        base.Update();
    }

    public void SetUIParent(UIElement parent)
    {
        parentPos = parent.transform.position;
    }

    protected bool IsHovered()
    {
        return Raylib.CheckCollisionPointRec(Raylib.GetMousePosition(), rectangle);
    }

    protected bool IsLeftClicked()
    {
        return IsHovered() && Raylib.IsMouseButtonPressed(Raylib_cs.MouseButton.Left);
    }
    protected bool IsRightClicked()
    {
        return IsHovered() && Raylib.IsMouseButtonPressed(Raylib_cs.MouseButton.Right);
    }

    private Vector2 GetPositionFromAlignment(PositionOnScreen xAlignment, PositionOnScreen yAlignMent)
    {
        var x = 0f;
        var y = 0f;
        switch (xAlignment)
        {
            case PositionOnScreen.LEFT:
                x = parentPos.X - parentPos.X;
                break;
            case PositionOnScreen.MIDDLE:
                x = parentPos.Y / 2 - width / 2;
                break;
            case PositionOnScreen.RIGHT:
                x = parentPos.Y - width;
                break;
        }
        switch (yAlignMent)
        {
            case PositionOnScreen.TOP:
                y = parentPos.Y - parentPos.Y;
                break;
            case PositionOnScreen.MIDDLE:
                y = parentPos.Y / 2 - height;
                break;
            case PositionOnScreen.BOTTOM:
                y = parentPos.Y - height / 2;
                break;
        }

        return new Vector2(x, y);
    }
}

public class UIButton : UIElement
{
    public event Action OnPress;
    public string? content;
    public int fontSize = 10;
    public Color color;
    public UIButton(int width, int height, PositionOnScreen xAlign, PositionOnScreen yAlign, string content, int fontSize, Color color) : base(width, height, xAlign, yAlign)
    {
        this.content = content;
        this.fontSize = fontSize;
        this.color = color;
    }

    public override void Update()
    {
        base.Update();
        if (IsLeftClicked())
            OnPress?.Invoke();
    }

    public override void Draw()
    {
        base.Draw();
        Raylib.DrawRectangleRec(rectangle, Color.Gray);
        Raylib.DrawText($"{content}", (int)transform.position.X, (int)transform.position.Y, fontSize, color);
    }

}