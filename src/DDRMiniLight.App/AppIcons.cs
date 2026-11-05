using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using DDRMiniLight.App.Controls;

namespace DDRMiniLight.App;

/// <summary>
/// DDR 패드 모양의 앱/트레이 아이콘을 그립니다.
/// 3×3 칸 중 십자 위치에 화살표 패널(좌우 파랑, 위아래 빨강)을 배치합니다.
/// </summary>
internal static class AppIcons
{
    private static readonly Color FrameColor = Color.FromArgb(52, 56, 64);
    private static readonly Color PlateColor = Color.FromArgb(150, 156, 166);

    // 위쪽을 가리키는 화살표, 중심 (0,0) 기준 [-1, 1] 범위
    private static readonly PointF[] ArrowShape =
    [
        new(0f, -0.95f), new(0.9f, 0.0f), new(0.38f, 0.0f), new(0.38f, 0.9f),
        new(-0.38f, 0.9f), new(-0.38f, 0.0f), new(-0.9f, 0.0f),
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
        // 트레이 크기(16~24px)에서는 회색 판을 빼고 색 패널만 크게 그립니다.
        bool tiny = size <= 24;

        // 바깥 프레임
        float framePad = tiny ? 0f : s * 0.02f;
        var frame = new RectangleF(framePad, framePad, s - framePad * 2, s - framePad * 2);
        using (var path = RoundedRect(frame, s * 0.14f))
        using (var brush = new SolidBrush(FrameColor))
            g.FillPath(brush, path);

        // 3×3 칸
        float inset = s * (tiny ? 0.08f : 0.08f);
        float gap = tiny ? Math.Max(0.5f, s * 0.02f) : Math.Max(1f, s * 0.035f);
        float cell = (s - inset * 2 - gap * 2) / 3f;

        (int Col, int Row, float Angle, Color Color)[] arrows =
        [
            (1, 0, 0f, PadView.UpDownColor),
            (0, 1, 270f, PadView.LeftRightColor),
            (2, 1, 90f, PadView.LeftRightColor),
            (1, 2, 180f, PadView.UpDownColor),
        ];

        if (tiny)
        {
            foreach (var (col, row, angle, color) in arrows)
            {
                var rect = new RectangleF(inset + col * (cell + gap), inset + row * (cell + gap), cell, cell);
                using (var panel = RoundedRect(rect, cell * 0.2f))
                using (var brush = new SolidBrush(active ? color : Grayscale(color)))
                    g.FillPath(brush, panel);

                if (size >= 20)
                {
                    // 작은 크기에서는 화살촉(삼각형)만 흰색으로
                    using var head = new GraphicsPath();
                    head.AddPolygon(new PointF[] { new(0f, -0.7f), new(0.7f, 0.35f), new(-0.7f, 0.35f) });
                    using var m = new Matrix();
                    m.Translate(rect.X + rect.Width / 2f, rect.Y + rect.Height / 2f);
                    m.Rotate(angle);
                    m.Scale(cell * 0.42f, cell * 0.42f);
                    head.Transform(m);
                    g.FillPath(Brushes.White, head);
                }
            }
            return bitmap;
        }

        for (int row = 0; row < 3; row++)
        {
            for (int col = 0; col < 3; col++)
            {
                var rect = new RectangleF(inset + col * (cell + gap), inset + row * (cell + gap), cell, cell);
                using var plate = RoundedRect(rect, cell * 0.18f);
                using var plateBrush = new SolidBrush(PlateColor);
                g.FillPath(plateBrush, plate);
            }
        }

        foreach (var (col, row, angle, color) in arrows)
        {
            var rect = new RectangleF(inset + col * (cell + gap), inset + row * (cell + gap), cell, cell);
            using var path = new GraphicsPath();
            path.AddPolygon(ArrowShape);
            using var matrix = new Matrix();
            matrix.Translate(rect.X + rect.Width / 2f, rect.Y + rect.Height / 2f);
            matrix.Rotate(angle);
            float r = cell * 0.42f;
            matrix.Scale(r, r);
            path.Transform(matrix);

            using var brush = new SolidBrush(active ? color : Grayscale(color));
            g.FillPath(brush, path);
            using var pen = new Pen(Color.FromArgb(200, Color.White), Math.Max(1f, s * 0.015f));
            g.DrawPath(pen, path);
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
