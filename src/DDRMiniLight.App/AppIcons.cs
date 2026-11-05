using System.Drawing.Drawing2D;

namespace DDRMiniLight.App;

/// <summary>별도 .ico 파일 없이 런타임에 앱/트레이 아이콘을 그립니다.</summary>
internal static class AppIcons
{
    private static readonly Color ActiveColor = Color.FromArgb(46, 139, 255);
    private static readonly Color InactiveColor = Color.FromArgb(140, 140, 140);

    /// <param name="size">아이콘 한 변의 픽셀 크기.</param>
    /// <param name="active">장치가 연결되어 있으면 파란색, 아니면 회색.</param>
    public static Icon Create(int size, bool active)
    {
        using var bitmap = new Bitmap(size, size);
        using (var g = Graphics.FromImage(bitmap))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);

            float s = size;
            float pad = s * 0.06f;
            float radius = s * 0.22f;
            var rect = new RectangleF(pad, pad, s - pad * 2, s - pad * 2);

            using (var bg = RoundedRect(rect, radius))
            using (var brush = new SolidBrush(active ? ActiveColor : InactiveColor))
                g.FillPath(brush, bg);

            PointF[] arrow =
            [
                new(s * 0.50f, s * 0.16f), new(s * 0.84f, s * 0.50f), new(s * 0.63f, s * 0.50f),
                new(s * 0.63f, s * 0.84f), new(s * 0.37f, s * 0.84f), new(s * 0.37f, s * 0.50f),
                new(s * 0.16f, s * 0.50f),
            ];
            g.FillPolygon(Brushes.White, arrow);
        }

        // GetHicon 핸들은 앱 수명 동안 몇 개만 만들므로 따로 해제하지 않습니다.
        return Icon.FromHandle(bitmap.GetHicon());
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
}
