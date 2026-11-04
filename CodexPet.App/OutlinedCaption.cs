using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CodexPet.App;

public sealed class OutlinedCaption : Control
{
    public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
        nameof(Text), typeof(string), typeof(OutlinedCaption),
        new FrameworkPropertyMetadata(string.Empty,
            FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var text = new FormattedText(Text, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight,
            new Typeface(FontFamily, FontStyle, FontWeight, FontStretch), FontSize,
            Brushes.White, VisualTreeHelper.GetDpi(this).PixelsPerDip);
        return new Size(text.WidthIncludingTrailingWhitespace + 10, text.Height + 10);
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        var text = new FormattedText(Text, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight,
            new Typeface(FontFamily, FontStyle, FontWeight, FontStretch), FontSize,
            Brushes.White, VisualTreeHelper.GetDpi(this).PixelsPerDip);
        var geometry = text.BuildGeometry(new Point(5, 5));
        if (geometry.Bounds.IsEmpty) return;

        double outlineWidth = geometry.Bounds.Height / 15;
        var outline = new Pen(Brushes.Black, outlineWidth * 2) { LineJoin = PenLineJoin.Round };
        drawingContext.DrawGeometry(Brushes.Black, outline, geometry);
        drawingContext.DrawGeometry(Brushes.White, null, geometry);
    }
}
