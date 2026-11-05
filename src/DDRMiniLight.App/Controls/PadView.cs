using System.ComponentModel;
using System.Drawing.Drawing2D;
using DDRMiniLight.Core;

namespace DDRMiniLight.App.Controls;

/// <summary>
/// DDR Classic Mini 패드 실물을 본뜬 그림.
/// 은색 판 위 3×3 칸에 화살표 패널(위아래 분홍, 좌우 하늘색)이 있고,
/// 아래쪽 흰 띠에 메뉴 좌(◀), 취소(빨강), 선택(초록), 메뉴 우(▶) 버튼이 있습니다.
/// 1P 패드는 왼쪽 아래, 2P 패드는 오른쪽 아래 모서리가 비스듬히 잘려 있습니다.
/// </summary>
/// <remarks>
/// 상태 표시: LED가 켜진 화살표는 밝게, 꺼진 화살표는 어둡게 그립니다.
/// 눌린 버튼은 흰 테두리와 함께 살짝 눌린 모양으로 그립니다.
/// </remarks>
internal sealed class PadView : Control
{
    // 실물 패널 색: 위아래 분홍, 좌우 하늘색
    internal static readonly Color UpDownColor = Color.FromArgb(232, 52, 142);
    internal static readonly Color LeftRightColor = Color.FromArgb(38, 160, 230);

    private static readonly Color PlateTop = Color.FromArgb(196, 199, 204);
    private static readonly Color PlateBottom = Color.FromArgb(160, 164, 170);
    private static readonly Color PlateEdge = Color.FromArgb(120, 124, 130);
    private static readonly Color GridLine = Color.FromArgb(140, 144, 150);
    private static readonly Color PanelBlack = Color.FromArgb(24, 24, 28);
    private static readonly Color MeshColor = Color.FromArgb(60, 60, 66);
    private static readonly Color PressRing = Color.White;
    private static readonly Color StripColor = Color.FromArgb(248, 248, 246);
    private static readonly Color MenuArrowColor = Color.FromArgb(232, 140, 30);
    private static readonly Color CancelColor = Color.FromArgb(214, 40, 40);
    private static readonly Color SelectColor = Color.FromArgb(30, 170, 80);

    // 위쪽을 가리키는 화살표, 중심 (0,0) 기준 [-1, 1] 범위
    private static readonly PointF[] ArrowShape =
    [
        new(0f, -0.9f), new(0.85f, -0.05f), new(0.38f, -0.05f), new(0.38f, 0.85f),
        new(-0.38f, 0.85f), new(-0.38f, -0.05f), new(-0.85f, -0.05f),
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
    private PadSide _side;

    public PadView()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer
                 | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
        Size = new Size(168, 200);
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

    /// <summary>1P/2P에 따라 잘린 모서리 위치가 달라집니다.</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public PadSide Side
    {
        get => _side;
        set
        {
            if (_side == value) return;
            _side = value;
            Invalidate();
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        g.Clear(BackColor);

        // 판 비율: 가로 3칸, 세로 3칸 + 아래쪽 버튼 띠
        const float GridUnits = 3f, StripUnits = 0.95f, Margin = 0.12f;
        float unit = Math.Min(
            ClientSize.Width / (GridUnits + Margin * 2),
            ClientSize.Height / (GridUnits + StripUnits + Margin * 2));
        float plateW = unit * (GridUnits + Margin * 2);
        float plateH = unit * (GridUnits + StripUnits + Margin * 2);
        var plate = new RectangleF((ClientSize.Width - plateW) / 2f, (ClientSize.Height - plateH) / 2f, plateW, plateH);

        DrawPlate(g, plate, unit);

        float gridX = plate.X + unit * Margin;
        float gridY = plate.Y + unit * Margin;
        DrawGrid(g, gridX, gridY, unit);

        foreach (var (button, column, row, angle) in ArrowLayout)
        {
            var cell = new RectangleF(gridX + column * unit, gridY + row * unit, unit, unit);
            DrawArrowPanel(g, cell, button, angle);
        }

        var strip = new RectangleF(
            plate.X + plate.Width * 0.12f,
            gridY + unit * 3f + unit * 0.22f,
            plate.Width * 0.76f,
            unit * 0.5f);
        DrawMenuStrip(g, strip);
    }

    private void DrawPlate(Graphics g, RectangleF r, float unit)
    {
        float radius = unit * 0.06f;
        float cut = unit * 0.32f;
        using var path = new GraphicsPath();

        // 시계 방향: 왼쪽 위 → 오른쪽 위 → 오른쪽 아래 → 왼쪽 아래
        path.AddArc(r.X, r.Y, radius * 2, radius * 2, 180, 90);
        path.AddArc(r.Right - radius * 2, r.Y, radius * 2, radius * 2, 270, 90);
        if (_side == PadSide.P2)
        {
            path.AddLine(r.Right, r.Y + radius, r.Right, r.Bottom - cut);
            path.AddLine(r.Right, r.Bottom - cut, r.Right - cut, r.Bottom);
        }
        else
        {
            path.AddArc(r.Right - radius * 2, r.Bottom - radius * 2, radius * 2, radius * 2, 0, 90);
        }
        if (_side == PadSide.P1)
        {
            path.AddLine(r.X + cut, r.Bottom, r.X, r.Bottom - cut);
        }
        else
        {
            path.AddArc(r.X, r.Bottom - radius * 2, radius * 2, radius * 2, 90, 90);
        }
        path.CloseFigure();

        using (var brush = new LinearGradientBrush(r, PlateTop, PlateBottom, LinearGradientMode.Vertical))
            g.FillPath(brush, path);
        using (var pen = new Pen(PlateEdge, Math.Max(1f, unit * 0.012f)))
            g.DrawPath(pen, path);
    }

    private static void DrawGrid(Graphics g, float x, float y, float unit)
    {
        using var pen = new Pen(GridLine, Math.Max(1f, unit * 0.008f));
        for (int i = 0; i <= 3; i++)
        {
            g.DrawLine(pen, x + i * unit, y, x + i * unit, y + unit * 3);
            g.DrawLine(pen, x, y + i * unit, x + unit * 3, y + i * unit);
        }
    }

    private void DrawArrowPanel(Graphics g, RectangleF cell, PadButtons button, float angle)
    {
        bool pressed = (_buttons & button) != 0;
        bool lit = (_lights & button) != 0;
        var color = button is PadButtons.Left or PadButtons.Right ? LeftRightColor : UpDownColor;

        // 눌리면 패널을 살짝 작게 (눌린 느낌)
        var panel = Inset(cell, cell.Width * (pressed ? 0.075f : 0.04f));

        using (var panelPath = RoundedRect(panel, panel.Width * 0.05f))
        {
            using (var black = new SolidBrush(PanelBlack))
                g.FillPath(black, panelPath);

            // 가운데 쪽 반원 무늬 (스피커 망처럼 보이는 부분)
            var state = g.Save();
            g.SetClip(panelPath);
            g.TranslateTransform(panel.X + panel.Width / 2f, panel.Y + panel.Height / 2f);
            g.RotateTransform(angle);
            float meshR = panel.Width * 0.42f;
            using (var mesh = new SolidBrush(MeshColor))
                g.FillEllipse(mesh, -meshR, panel.Height / 2f - meshR * 0.55f, meshR * 2, meshR * 2);
            g.Restore(state);

            if (pressed)
            {
                using var ring = new Pen(PressRing, Math.Max(2f, cell.Width * 0.05f));
                g.DrawPath(ring, panelPath);
            }
        }

        // 화살표: LED가 켜지면 밝게, 꺼지면 어둡게
        var arrowColor = lit ? color : Blend(color, PanelBlack, 0.55f);
        using var arrow = ArrowPath(panel, angle, panel.Width * 0.36f);
        if (lit)
        {
            using var glow = new Pen(Color.FromArgb(110, color), Math.Max(3f, panel.Width * 0.1f)) { LineJoin = LineJoin.Round };
            g.DrawPath(glow, arrow);
        }
        using (var brush = new SolidBrush(arrowColor))
            g.FillPath(brush, arrow);
        using (var outline = new Pen(lit ? Color.White : Color.FromArgb(150, 210, 210, 210), Math.Max(1f, panel.Width * 0.035f)) { LineJoin = LineJoin.Round })
            g.DrawPath(outline, arrow);
    }

    private void DrawMenuStrip(Graphics g, RectangleF strip)
    {
        using (var path = RoundedRect(strip, strip.Height / 2f))
        {
            using (var brush = new SolidBrush(StripColor))
                g.FillPath(brush, path);
            using (var pen = new Pen(Color.FromArgb(150, 150, 150), Math.Max(1f, strip.Height * 0.03f)))
                g.DrawPath(pen, path);
        }

        // 실물 배치: ◀ (주황)  ■ 취소 (빨강)  ■ 선택 (초록)  ▶ (주황)
        float size = strip.Height * 0.48f;
        float cy = strip.Y + strip.Height / 2f;
        float cx = strip.X + strip.Width / 2f;
        float step = strip.Height * 0.78f;

        DrawMenuItem(g, PadButtons.MenuLeft, new PointF(cx - step * 1.65f, cy), size, MenuArrowColor, shape: -1);
        DrawMenuItem(g, PadButtons.Cancel, new PointF(cx - step * 0.5f, cy), size, CancelColor, shape: 0);
        DrawMenuItem(g, PadButtons.Select, new PointF(cx + step * 0.5f, cy), size, SelectColor, shape: 0);
        DrawMenuItem(g, PadButtons.MenuRight, new PointF(cx + step * 1.65f, cy), size, MenuArrowColor, shape: 1);
    }

    /// <param name="shape">-1: 왼쪽 삼각형, 0: 사각형, 1: 오른쪽 삼각형</param>
    private void DrawMenuItem(Graphics g, PadButtons button, PointF center, float size, Color color, int shape)
    {
        bool pressed = (_buttons & button) != 0;
        float h = size / 2f;

        using var path = new GraphicsPath();
        if (shape == 0)
        {
            path.AddRectangle(new RectangleF(center.X - h * 1.1f, center.Y - h * 0.8f, h * 2.2f, h * 1.6f));
        }
        else
        {
            float dir = shape;
            path.AddPolygon(new[]
            {
                new PointF(center.X + dir * h, center.Y),
                new PointF(center.X - dir * h * 0.8f, center.Y - h),
                new PointF(center.X - dir * h * 0.8f, center.Y + h),
            });
        }

        if (pressed)
        {
            // 눌린 버튼: 바깥에 어두운 원형 표시
            float r = size * 0.95f;
            using var halo = new SolidBrush(Color.FromArgb(70, color));
            g.FillEllipse(halo, center.X - r, center.Y - r, r * 2, r * 2);
        }

        using (var brush = new SolidBrush(pressed ? color : Blend(color, StripColor, 0.25f)))
            g.FillPath(brush, path);
        if (pressed)
        {
            using var pen = new Pen(ControlPaint.Dark(color, 0.3f), Math.Max(1.5f, size * 0.12f));
            g.DrawPath(pen, path);
        }
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
        float d = Math.Max(0.5f, radius * 2);
        var path = new GraphicsPath();
        path.AddArc(r.X, r.Y, d, d, 180, 90);
        path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }

    private static RectangleF Inset(RectangleF r, float amount)
    {
        r.Inflate(-amount, -amount);
        return r;
    }

    private static Color Blend(Color a, Color b, float amountOfB) => Color.FromArgb(
        (int)(a.R + (b.R - a.R) * amountOfB),
        (int)(a.G + (b.G - a.G) * amountOfB),
        (int)(a.B + (b.B - a.B) * amountOfB));
}
