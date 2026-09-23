using Microsoft.Maui.Controls.Shapes;

namespace AniRankApp.Controls;

/// <summary>
/// Pulsing placeholder cards shown while a list loads for the first time, in the shape of the
/// real result cards (poster + text lines). Feels faster than a lone spinner in the middle.
/// </summary>
public class SkeletonList : ContentView
{
    private const string PulseAnimation = "SkeletonPulse";

    public static readonly BindableProperty IsActiveProperty = BindableProperty.Create(
        nameof(IsActive), typeof(bool), typeof(SkeletonList), false,
        propertyChanged: (b, _, _) => ((SkeletonList)b).UpdateActive());

    public static readonly BindableProperty CountProperty = BindableProperty.Create(
        nameof(Count), typeof(int), typeof(SkeletonList), 5,
        propertyChanged: (b, _, _) => ((SkeletonList)b).Build());

    public static readonly BindableProperty PosterWidthProperty = BindableProperty.Create(
        nameof(PosterWidth), typeof(double), typeof(SkeletonList), 88.0,
        propertyChanged: (b, _, _) => ((SkeletonList)b).Build());

    public static readonly BindableProperty ItemHeightProperty = BindableProperty.Create(
        nameof(ItemHeight), typeof(double), typeof(SkeletonList), 140.0,
        propertyChanged: (b, _, _) => ((SkeletonList)b).Build());

    public bool IsActive
    {
        get => (bool)GetValue(IsActiveProperty);
        set => SetValue(IsActiveProperty, value);
    }

    public int Count
    {
        get => (int)GetValue(CountProperty);
        set => SetValue(CountProperty, value);
    }

    /// <summary>0 hides the poster block (e.g. user rows).</summary>
    public double PosterWidth
    {
        get => (double)GetValue(PosterWidthProperty);
        set => SetValue(PosterWidthProperty, value);
    }

    public double ItemHeight
    {
        get => (double)GetValue(ItemHeightProperty);
        set => SetValue(ItemHeightProperty, value);
    }

    public SkeletonList()
    {
        InputTransparent = true;
        IsVisible = false;
        SemanticProperties.SetDescription(this, "Učitavanje");
        Build();
    }

    private void Build()
    {
        var stack = new VerticalStackLayout { Spacing = 10 };
        for (var i = 0; i < Count; i++)
            stack.Add(BuildCard());

        Content = stack;
    }

    private View BuildCard()
    {
        var grid = new Grid { ColumnSpacing = 12 };

        if (PosterWidth > 0)
        {
            grid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(PosterWidth)));
            grid.Add(Block(double.NaN, double.NaN, 10), 0);
        }

        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));

        var lines = new VerticalStackLayout
        {
            Spacing = 10,
            VerticalOptions = LayoutOptions.Center,
            Children =
            {
                Block(180, 16, 6),
                Block(120, 12, 6),
                Block(150, 12, 6),
                Block(60, 22, 8)
            }
        };
        grid.Add(lines, grid.ColumnDefinitions.Count - 1);

        var card = new Border
        {
            HeightRequest = ItemHeight,
            Padding = 10,
            StrokeThickness = 1,
            StrokeShape = new RoundRectangle { CornerRadius = 16 },
            Content = grid
        };
        card.SetDynamicResource(BackgroundColorProperty, "CardBackground");
        card.SetDynamicResource(Border.StrokeProperty, "Gray300");
        return card;
    }

    private static BoxView Block(double width, double height, double radius)
    {
        var box = new BoxView
        {
            CornerRadius = radius,
            HorizontalOptions = double.IsNaN(width) ? LayoutOptions.Fill : LayoutOptions.Start
        };
        if (!double.IsNaN(width)) box.WidthRequest = width;
        if (!double.IsNaN(height)) box.HeightRequest = height;
        box.SetDynamicResource(BoxView.ColorProperty, "Skeleton");
        return box;
    }

    private void UpdateActive()
    {
        IsVisible = IsActive;
        this.AbortAnimation(PulseAnimation);

        if (!IsActive)
        {
            Opacity = 1;
            return;
        }

        var pulse = new Animation
        {
            { 0, 0.5, new Animation(v => Opacity = v, 1, 0.45) },
            { 0.5, 1, new Animation(v => Opacity = v, 0.45, 1) }
        };
        pulse.Commit(this, PulseAnimation, length: 1100, easing: Easing.SinInOut, repeat: () => IsActive);
    }
}
