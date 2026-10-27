using UnityEngine;
using UnityEngine.UIElements;

namespace Relicbound.Game.UI
{
    /// <summary>Escudo heráldico dibujado con Painter2D: nítido a cualquier escala.</summary>
    public sealed class ShieldElement : VisualElement
    {
        private BannerStyle _style;

        public ShieldElement(BannerStyle banner)
        {
            _style = banner;
            pickingMode = PickingMode.Ignore;
            style.position = Position.Absolute;
            style.left = 0;
            style.top = 0;
            style.right = 0;
            style.bottom = 0;
            generateVisualContent += Draw;
        }

        public void SetStyle(BannerStyle banner)
        {
            _style = banner;
            MarkDirtyRepaint();
        }

        private static void ShieldPath(Painter2D painter, Rect r, float inset)
        {
            var left = r.xMin + inset;
            var right = r.xMax - inset;
            var top = r.yMin + inset;
            var bottom = r.yMax - inset;
            var midX = r.center.x;
            var shoulder = top + (bottom - top) * 0.52f;
            painter.BeginPath();
            painter.MoveTo(new Vector2(left, top));
            painter.LineTo(new Vector2(right, top));
            painter.LineTo(new Vector2(right, shoulder));
            painter.BezierCurveTo(new Vector2(right, shoulder + (bottom - shoulder) * 0.55f),
                new Vector2(midX + (right - midX) * 0.45f, bottom - (bottom - shoulder) * 0.12f), new Vector2(midX, bottom));
            painter.BezierCurveTo(new Vector2(midX - (midX - left) * 0.45f, bottom - (bottom - shoulder) * 0.12f),
                new Vector2(left, shoulder + (bottom - shoulder) * 0.55f), new Vector2(left, shoulder));
            painter.ClosePath();
        }

        private void Draw(MeshGenerationContext context)
        {
            var r = contentRect;
            if (r.width < 4f || r.height < 4f) return;
            var painter = context.painter2D;
            var stroke = Mathf.Max(2f, r.width * 0.05f);

            // Sombra, borde metálico y campo.
            painter.fillColor = new Color(0f, 0f, 0f, 0.35f);
            ShieldPath(painter, new Rect(r.x + 1f, r.y + 3f, r.width, r.height), 0f);
            painter.Fill();
            painter.fillColor = _style.Metal * 0.75f + Color.black * 0.25f;
            ShieldPath(painter, r, 0f);
            painter.Fill();
            painter.fillColor = _style.Field;
            ShieldPath(painter, r, stroke);
            painter.Fill();

            // Luz en el cuarto superior izquierdo.
            painter.fillColor = new Color(1f, 1f, 1f, 0.10f);
            painter.BeginPath();
            painter.MoveTo(new Vector2(r.xMin + stroke, r.yMin + stroke));
            painter.LineTo(new Vector2(r.center.x, r.yMin + stroke));
            painter.LineTo(new Vector2(r.xMin + stroke, r.center.y));
            painter.ClosePath();
            painter.Fill();

            painter.fillColor = _style.Metal;
            var cx = r.center.x;
            var w = r.width;
            var h = r.height;
            switch (_style.Charge)
            {
                case BannerCharge.Chevron:
                    painter.BeginPath();
                    painter.MoveTo(new Vector2(r.xMin + w * 0.18f, r.yMin + h * 0.66f));
                    painter.LineTo(new Vector2(cx, r.yMin + h * 0.32f));
                    painter.LineTo(new Vector2(r.xMax - w * 0.18f, r.yMin + h * 0.66f));
                    painter.LineTo(new Vector2(r.xMax - w * 0.30f, r.yMin + h * 0.72f));
                    painter.LineTo(new Vector2(cx, r.yMin + h * 0.48f));
                    painter.LineTo(new Vector2(r.xMin + w * 0.30f, r.yMin + h * 0.72f));
                    painter.ClosePath();
                    painter.Fill();
                    break;
                case BannerCharge.Cross:
                    painter.BeginPath();
                    var arm = w * 0.09f;
                    painter.MoveTo(new Vector2(cx - arm, r.yMin + h * 0.18f));
                    painter.LineTo(new Vector2(cx + arm, r.yMin + h * 0.18f));
                    painter.LineTo(new Vector2(cx + arm, r.yMin + h * 0.38f));
                    painter.LineTo(new Vector2(r.xMax - w * 0.2f, r.yMin + h * 0.38f));
                    painter.LineTo(new Vector2(r.xMax - w * 0.2f, r.yMin + h * 0.5f));
                    painter.LineTo(new Vector2(cx + arm, r.yMin + h * 0.5f));
                    painter.LineTo(new Vector2(cx + arm, r.yMin + h * 0.8f));
                    painter.LineTo(new Vector2(cx - arm, r.yMin + h * 0.8f));
                    painter.LineTo(new Vector2(cx - arm, r.yMin + h * 0.5f));
                    painter.LineTo(new Vector2(r.xMin + w * 0.2f, r.yMin + h * 0.5f));
                    painter.LineTo(new Vector2(r.xMin + w * 0.2f, r.yMin + h * 0.38f));
                    painter.LineTo(new Vector2(cx - arm, r.yMin + h * 0.38f));
                    painter.ClosePath();
                    painter.Fill();
                    break;
                default:
                    // Torre almenada.
                    var tw = w * 0.36f;
                    var left = cx - tw / 2f;
                    var top = r.yMin + h * 0.3f;
                    var bottom = r.yMin + h * 0.74f;
                    var merlon = tw / 5f;
                    painter.BeginPath();
                    painter.MoveTo(new Vector2(left, bottom));
                    painter.LineTo(new Vector2(left, top));
                    for (var i = 0; i < 5; i++)
                    {
                        var x0 = left + merlon * i;
                        var high = i % 2 == 0;
                        painter.LineTo(new Vector2(x0, high ? top - merlon : top));
                        painter.LineTo(new Vector2(x0 + merlon, high ? top - merlon : top));
                    }
                    painter.LineTo(new Vector2(left + tw, bottom));
                    painter.ClosePath();
                    painter.Fill();
                    painter.fillColor = _style.Field;
                    painter.BeginPath();
                    painter.MoveTo(new Vector2(cx - merlon * 0.6f, bottom));
                    painter.LineTo(new Vector2(cx - merlon * 0.6f, bottom - h * 0.12f));
                    painter.ArcTo(new Vector2(cx - merlon * 0.6f, bottom - h * 0.12f - merlon * 0.6f),
                        new Vector2(cx, bottom - h * 0.12f - merlon * 0.6f), merlon * 0.6f);
                    painter.ArcTo(new Vector2(cx + merlon * 0.6f, bottom - h * 0.12f - merlon * 0.6f),
                        new Vector2(cx + merlon * 0.6f, bottom - h * 0.12f), merlon * 0.6f);
                    painter.LineTo(new Vector2(cx + merlon * 0.6f, bottom));
                    painter.ClosePath();
                    painter.Fill();
                    break;
            }
        }
    }

    public enum GlyphKind
    {
        Gear,
        Close,
    }

    /// <summary>Iconos de interfaz vectoriales (sin emojis ni texturas pixeladas).</summary>
    public sealed class GlyphElement : VisualElement
    {
        private readonly GlyphKind _kind;

        public GlyphElement(GlyphKind kind)
        {
            _kind = kind;
            pickingMode = PickingMode.Ignore;
            style.flexGrow = 1;
            generateVisualContent += Draw;
        }

        public Color Color { get; set; } = new Color(0.91f, 0.80f, 0.47f);

        private void Draw(MeshGenerationContext context)
        {
            var r = contentRect;
            var painter = context.painter2D;
            var c = r.center;
            var size = Mathf.Min(r.width, r.height);
            painter.strokeColor = Color;
            painter.fillColor = Color;
            painter.lineCap = LineCap.Round;
            painter.lineJoin = LineJoin.Round;
            switch (_kind)
            {
                case GlyphKind.Close:
                    painter.lineWidth = size * 0.14f;
                    var d = size * 0.3f;
                    painter.BeginPath();
                    painter.MoveTo(c + new Vector2(-d, -d));
                    painter.LineTo(c + new Vector2(d, d));
                    painter.MoveTo(c + new Vector2(d, -d));
                    painter.LineTo(c + new Vector2(-d, d));
                    painter.Stroke();
                    break;
                default:
                    var outer = size * 0.48f;
                    var inner = size * 0.36f;
                    const int teeth = 8;
                    painter.BeginPath();
                    for (var i = 0; i < teeth * 4; i++)
                    {
                        var angle = i / (float)(teeth * 4) * Mathf.PI * 2f;
                        var radius = (i % 4 == 1 || i % 4 == 2) ? outer : inner;
                        var point = c + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                        if (i == 0) painter.MoveTo(point);
                        else painter.LineTo(point);
                    }
                    painter.ClosePath();
                    painter.Fill();
                    painter.fillColor = new Color(0.08f, 0.13f, 0.24f);
                    painter.BeginPath();
                    painter.Arc(c, size * 0.15f, Angle.Degrees(0f), Angle.Degrees(360f));
                    painter.Fill();
                    break;
            }
        }
    }
}
