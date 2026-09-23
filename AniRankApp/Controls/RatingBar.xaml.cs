namespace AniRankApp.Controls;

/// <summary>
/// Custom control (ContentView) that renders a rating as stars + a coloured badge.
/// Exposes BindableProperties: RatingValue, MaxRating, ShowStars.
/// </summary>
public partial class RatingBar : ContentView
{
    public RatingBar()
    {
        InitializeComponent();
        Render();
    }

    public static readonly BindableProperty RatingValueProperty = BindableProperty.Create(
        nameof(RatingValue), typeof(double), typeof(RatingBar), 0.0, propertyChanged: OnAnyChanged);

    public static readonly BindableProperty MaxRatingProperty = BindableProperty.Create(
        nameof(MaxRating), typeof(int), typeof(RatingBar), 10, propertyChanged: OnAnyChanged);

    public static readonly BindableProperty ShowStarsProperty = BindableProperty.Create(
        nameof(ShowStars), typeof(bool), typeof(RatingBar), true, propertyChanged: OnAnyChanged);

    public double RatingValue
    {
        get => (double)GetValue(RatingValueProperty);
        set => SetValue(RatingValueProperty, value);
    }

    public int MaxRating
    {
        get => (int)GetValue(MaxRatingProperty);
        set => SetValue(MaxRatingProperty, value);
    }

    public bool ShowStars
    {
        get => (bool)GetValue(ShowStarsProperty);
        set => SetValue(ShowStarsProperty, value);
    }

    private static void OnAnyChanged(BindableObject bindable, object oldValue, object newValue)
        => ((RatingBar)bindable).Render();

    private void Render()
    {
        if (StarsLayout is null) return;

        var color = TierColor();
        const int starCount = 5;
        var filled = MaxRating > 0
            ? (int)Math.Round(RatingValue / MaxRating * starCount, MidpointRounding.AwayFromZero)
            : 0;
        filled = Math.Clamp(filled, 0, starCount);

        StarsLayout.Children.Clear();
        StarsLayout.IsVisible = ShowStars;

        if (ShowStars)
        {
            for (int i = 1; i <= starCount; i++)
            {
                StarsLayout.Children.Add(new Label
                {
                    Text = i <= filled ? IconGlyphs.Star : IconGlyphs.StarBorder, // Material star / star_border
                    FontFamily = "Icons",
                    FontSize = 16,
                    TextColor = color
                });
            }
        }

        ValueLabel.Text = $"{RatingValue:0.0} / {MaxRating}";
        ValueBadge.BackgroundColor = color;
        ValueLabel.TextColor = ThemeColors.OnScore; // dark text: white on green/yellow is unreadable
        SemanticProperties.SetDescription(this, $"Ocena {RatingValue:0.0} od {MaxRating}");
    }

    private Color TierColor() => ThemeColors.ForRating(RatingValue, MaxRating);
}
