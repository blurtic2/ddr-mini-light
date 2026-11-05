using System.ComponentModel;
using System.Drawing.Drawing2D;
using DDRMiniLight.Core;

namespace DDRMiniLight.App.Controls;

/// <summary>
/// 컨트롤러 화살표 4개를 십자 배치로 그립니다.
/// 눌린 버튼은 색으로 채우고, LED가 켜진 버튼은 바깥에 노란 테두리를 그립니다.
/// </summary>
internal sealed class PadView : Control
{
    // 실물 패널 색: 좌우는 파랑, 위아래는 빨강
    internal static readonly Color LeftRightColor = Color.FromArgb(46, 139, 255);
    internal static readonly Color UpDownColor = Color.FromArgb(232, 64, 96);
    private static readonly Color LightColor = Color.FromArgb(255, 200, 40);

    // 위쪽을 가리키는 화살표, 중심 (0,0) 기준 [-1, 1] 범위
    private static readonly PointF[] ArrowShape =
    [
        new(0f, -0.9f), new(0.85f, -0.05f), new(0.35f, -0.05f), new(0.35f, 0.85f),
        new(-0.35f, 0.85f), new(-0.35f, -0.05f), new(-0.85f, -0.05f),
    ];

    private static readonly (PadButtons Button, int Column, int Row, float Angle)[] ArrowLayout =
    [
        (PadButtons.Up, 1, 0, 0f),
        (PadButtons.Left, 0, 1, 270f),
        (PadButtons.Right, 2, 1, 90f),
        (PadButtons.Down, 1, 2, 180f),
    ];

    private PadButtons _buttons;
    private PadButtons _lights;

    public PadView()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer
                 | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
        Size = new Size(168, 168);
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public PadButtons Buttons
    {
        get => _buttons;
        set
        {
            if (_buttons == value) return;
            _buttons = value;
            Invalidate();
        }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public PadButtons Lights
    {
        get => _lights;
        set
        {
            if (_lights == value) return;
            _lights = value;
            Invalidate();
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(BackColor);

        float cell = Math.Min(ClientSize.Width, ClientSize.Height) / 3f;
        float offsetX = (ClientSize.Width - cell * 3) / 2f;
        float offsetY = (ClientSize.Height - cell * 3) / 2f;

        // 가운데 칸
        using (var centerBrush = new SolidBrush(ControlPaint.Light(BackColor, 0.3f)))
        using (var centerPath = RoundedRect(CellRect(1, 1).Inflated(-cell * 0.12f), cell * 0.12f))
            g.FillPath(centerBrush, centerPath);

        foreach (var (button, column, row, angle) in ArrowLayout)
        {
            var rect = CellRect(column, row);
            bool pressed = (_buttons & button) != 0;
            bool lit = (_lights & button) != 0;
            var color = button is PadButtons.Left or PadButtons.Right ? LeftRightColor : UpDownColor;

            using var tile = RoundedRect(rect.Inflated(-cell * 0.06f), cell * 0.16f);
            using (var tileBrush = new SolidBrush(pressed ? Blend(color, BackColor, 0.75f) : ControlPaint.Light(BackColor, 0.5f)))
                g.FillPath(tileBrush, tile);

            if (lit)
            {
                using var glow = new Pen(LightColor, Math.Max(2f, cell * 0.06f));
                g.DrawPath(glow, tile);
            }

            using var arrow = ArrowPath(rect, angle, cell * 0.36f);
            using (var arrowBrush = new SolidBrush(pressed ? color : Color.FromArgb(70, ForeColor)))
                g.FillPath(arrowBrush, arrow);
            using (var outline = new Pen(pressed ? ControlPaint.Dark(color, 0.1f) : Color.FromArgb(110, ForeColor), 1.5f))
                g.DrawPath(outline, arrow);
        }

        RectangleF CellRect(int column, int row) =>
            new(offsetX + column * cell, offsetY + row * cell, cell, cell);
    }

    private static GraphicsPath ArrowPath(RectangleF cell, float angle, float radius)
    {
        var path = new GraphicsPath();
        path.AddPolygon(ArrowShape);
        using var matrix = new Matrix();
        matrix.Translate(cell.X + cell.Width / 2f, cell.Y + cell.Height / 2f);
        matrix.Rotate(angle);
        matrix.Scale(radius, radius);
        path.Transform(matrix);
        return path;
    }

    private static GraphicsPath RoundedRect(RectangleF r, float radius)
    {
        float d = radius * 2;
        var path = new GraphicsPath();
        path.AddArc(r.X, r.Y, d, d, 180, 90);
        path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }

    private static Color Blend(Color a, Color b, float amountOfB) => Color.FromArgb(
        (int)(a.R + (b.R - a.R) * amountOfB),
        (int)(a.G + (b.G - a.G) * amountOfB),
        (int)(a.B + (b.B - a.B) * amountOfB));
}

internal static class RectangleFExtensions
{
    public static RectangleF Inflated(this RectangleF rect, float amount)
    {
        rect.Inflate(amount, amount);
        return rect;
    }
}
