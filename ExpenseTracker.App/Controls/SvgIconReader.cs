namespace ExpenseTracker.App.Controls;

using System.Globalization;
using System.Text;
using System.Xml.Linq;

/// <summary>
/// Turns an SVG file into path data for <see cref="IconView"/>. Meant for simple line icons
/// (24 x 24 grid) made of paths, circles, rectangles, lines and polylines.
/// The result goes through <see cref="SvgPathNormalizer"/>.
/// </summary>
public static class SvgIconReader
{
    private const int MaxLength = 20_000;

    /// <summary>Returns the path data, or null when the file holds no usable shapes.</summary>
    public static string? ToPathData(string svg)
    {
        XElement root;

        try
        {
            var settings = new System.Xml.XmlReaderSettings { DtdProcessing = System.Xml.DtdProcessing.Prohibit };
            using var reader = System.Xml.XmlReader.Create(new StringReader(svg), settings);
            root = XDocument.Load(reader).Root ?? throw new FormatException();
        }
        catch
        {
            return null;
        }

        if (!Is24Grid(root)) return null;

        var data = new StringBuilder();

        foreach (var element in root.Descendants())
        {
            // Each shape is normalized on its own, so a relative first move cannot depend on the shape before it.
            if (Shape(element) is { Length: > 0 } shape)
            {
                if (SvgPathNormalizer.Normalize(shape) is not { } normalized) return null;

                data.Append(normalized);
            }
        }

        var result = data.ToString().Trim();

        return result.Length is > 0 and <= MaxLength ? result : null;
    }

    /// <summary>Only a 24 x 24 grid (or none given) is supported: the path data is used as it is.</summary>
    private static bool Is24Grid(XElement root)
    {
        var box = root.Attribute("viewBox")?.Value;

        if (box is null) return true;

        var parts = box.Split([' ', ','], StringSplitOptions.RemoveEmptyEntries);

        return parts.Length == 4
               && double.TryParse(parts[2], CultureInfo.InvariantCulture, out var width)
               && Math.Abs(width - 24) < 0.01;
    }

    private static double Num(XElement e, string name, double fallback = 0) =>
        double.TryParse(e.Attribute(name)?.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v : fallback;

    private static string F(double v) => v.ToString("0.###", CultureInfo.InvariantCulture);

    private static string Shape(XElement e)
    {
        switch (e.Name.LocalName)
        {
            case "path":
                return e.Attribute("d")?.Value ?? "";

            case "circle":
            {
                double cx = Num(e, "cx"), cy = Num(e, "cy"), r = Num(e, "r");
                return $"M{F(cx - r)} {F(cy)}a{F(r)} {F(r)} 0 1 0 {F(2 * r)} 0a{F(r)} {F(r)} 0 1 0 {F(-2 * r)} 0";
            }

            case "ellipse":
            {
                double cx = Num(e, "cx"), cy = Num(e, "cy"), rx = Num(e, "rx"), ry = Num(e, "ry");
                return $"M{F(cx - rx)} {F(cy)}a{F(rx)} {F(ry)} 0 1 0 {F(2 * rx)} 0a{F(rx)} {F(ry)} 0 1 0 {F(-2 * rx)} 0";
            }

            case "line":
                return $"M{F(Num(e, "x1"))} {F(Num(e, "y1"))}L{F(Num(e, "x2"))} {F(Num(e, "y2"))}";

            case "polyline":
            case "polygon":
            {
                var numbers = (e.Attribute("points")?.Value ?? "")
                    .Split([' ', ',', '\t', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => double.TryParse(x, NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v : double.NaN)
                    .ToArray();

                if (numbers.Length < 4 || numbers.Any(double.IsNaN)) return "";

                var sb = new StringBuilder($"M{F(numbers[0])} {F(numbers[1])}");
                for (var i = 2; i + 1 < numbers.Length; i += 2) sb.Append($"L{F(numbers[i])} {F(numbers[i + 1])}");
                if (e.Name.LocalName == "polygon") sb.Append('Z');
                return sb.ToString();
            }

            case "rect":
            {
                double x = Num(e, "x"), y = Num(e, "y"), w = Num(e, "width"), h = Num(e, "height");
                var r = Math.Min(Math.Min(Num(e, "rx", Num(e, "ry")), w / 2), h / 2);

                if (r <= 0) return $"M{F(x)} {F(y)}h{F(w)}v{F(h)}h{F(-w)}z";

                return $"M{F(x + r)} {F(y)}h{F(w - 2 * r)}a{F(r)} {F(r)} 0 0 1 {F(r)} {F(r)}v{F(h - 2 * r)}a{F(r)} {F(r)} 0 0 1 {F(-r)} {F(r)}"
                       + $"h{F(-(w - 2 * r))}a{F(r)} {F(r)} 0 0 1 {F(-r)} {F(-r)}v{F(-(h - 2 * r))}a{F(r)} {F(r)} 0 0 1 {F(r)} {F(-r)}z";
            }

            default:
                return "";
        }
    }
}
