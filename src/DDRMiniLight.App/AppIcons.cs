using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using DDRMiniLight.App.Controls;

namespace DDRMiniLight.App;

/// <summary>
/// DDR Classic Mini 패드 모양의 앱/트레이 아이콘을 그립니다.
/// 은색 판 위 십자 위치에 검은 패널과 화살표(위아래 분홍, 좌우 하늘색)를 배치합니다.
/// </summary>
internal static class AppIcons
{
    private static readonly Color PlateTop = Color.FromArgb(214, 217, 222);
    private static readonly Color PlateBottom = Color.FromArgb(160, 164, 170);
    private static readonly Color PlateEdge = Color.FromArgb(90, 94, 100);
    private static readonly Color PanelBlack = Color.FromArgb(24, 24, 28);

    // 위쪽을 가리키는 화살표, 중심 (0,0) 기준 [-1, 1] 범위
    private static readonly PointF[] ArrowShape =
    [
        new(0f, -0.95f), new(0.9f, 0.0f), new(0.4f, 0.0f), new(0.4f, 0.9f),
        new(-0.4f, 0.9f), new(-0.4f, 0.0f), new(-0.9f, 0.0f),
    ];

    /// <param name="size">아이콘 한 변의 픽셀 크기.</param>
    /// <param name="active">장치가 연결되어 있으면 컬러, 아니면 회색조.</param>
    public static Icon Create(int size, bool active)
    {
        using var bitmap = Render(size, active);
        // GetHicon 핸들은 앱 수명 동안 몇 개만 만들므로 따로 해제하지 않습니다.
        return Icon.FromHandle(bitmap.GetHicon());
    }

    /// <summary>아이콘 이미지를 그립니다. 빌드용 .ico 생성에도 쓰입니다.</summary>
    public static Bitmap Render(int size, bool active)
    {
        var bitmap = new Bitmap(size, size, PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(bitmap);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        g.Clear(Color.Transparent);

        float s = size;
        // 트레이 크기(16~24px)에서는 화살표 대신 색 패널만 크게 그립니다.
        bool tiny = size <= 24;

        // 은색 판
        var plate = new RectangleF(0.5f, 0.5f, s - 1f, s - 1f);
        using (var path = RoundedRect(plate, s * 0.14f))
        {
            using (var brush = new LinearGradientBrush(plate, PlateTop, PlateBottom, LinearGradientMode.Vertical))
                g.FillPath(brush, path);
            using (var pen = new Pen(PlateEdge, Math.Max(1f, s * 0.03f)))
                g.DrawPath(pen, path);
        }

        float inset = s * (tiny ? 0.1f : 0.09f);
        float gap = tiny ? Math.Max(0.5f, s * 0.03f) : s * 0.025f;
        float cell = (s - inset * 2 - gap * 2) / 3f;

        (int Col, int Row, float Angle, Color Color)[] arrows =
        [
            (1, 0, 0f, PadView.UpDownColor),
            (0, 1, 270f, PadView.LeftRightColor),
            (2, 1, 90f, PadView.LeftRightColor),
            (1, 2, 180f, PadView.UpDownColor),
        ];

        foreach (var (col, row, angle, color) in arrows)
        {
            var rect = new RectangleF(inset + col * (cell + gap), inset + row * (cell + gap), cell, cell);
            var c = active ? color : Grayscale(color);

            if (tiny)
            {
                using (var panel = RoundedRect(rect, cell * 0.18f))
                using (var brush = new SolidBrush(c))
                    g.FillPath(brush, panel);

                if (size >= 20)
                {
                    using var head = new GraphicsPath();
                    head.AddPolygon(new PointF[] { new(0f, -0.7f), new(0.7f, 0.35f), new(-0.7f, 0.35f) });
                    using var m = new Matrix();
                    m.Translate(rect.X + rect.Width / 2f, rect.Y + rect.Height / 2f);
                    m.Rotate(angle);
                    m.Scale(cell * 0.42f, cell * 0.42f);
                    head.Transform(m);
                    g.FillPath(Brushes.White, head);
                }
                continue;
            }

            // 검은 패널 + 흰 테두리 화살표
            using (var panel = RoundedRect(rect, cell * 0.08f))
            using (var black = new SolidBrush(PanelBlack))
                g.FillPath(black, panel);

            using var arrow = new GraphicsPath();
            arrow.AddPolygon(ArrowShape);
            using var matrix = new Matrix();
            matrix.Translate(rect.X + rect.Width / 2f, rect.Y + rect.Height / 2f);
            matrix.Rotate(angle);
            float r = cell * 0.4f;
            matrix.Scale(r, r);
            arrow.Transform(matrix);

            using (var brush = new SolidBrush(c))
                g.FillPath(brush, arrow);
            using (var pen = new Pen(Color.White, Math.Max(1f, s * 0.018f)) { LineJoin = LineJoin.Round })
                g.DrawPath(pen, arrow);
        }

        return bitmap;
    }

    private static Color Grayscale(Color c)
    {
        int v = (int)(c.R * 0.3 + c.G * 0.59 + c.B * 0.11);
        return Color.FromArgb(v, v, v);
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
}
