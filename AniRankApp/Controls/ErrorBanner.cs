namespace AniRankApp.Controls;

/// <summary>
/// Dismissable message banner that floats over the bottom of a page instead of pushing the
/// layout around. Bind <see cref="Message"/> to a view model's ErrorMessage (two-way, so the
/// close button clears it). <see cref="IsWarning"/> switches to the amber "offline" look.
/// </summary>
public class ErrorBanner : ContentView
{
    public static readonly BindableProperty MessageProperty = BindableProperty.Create(
        nameof(Message), typeof(string), typeof(ErrorBanner), null, BindingMode.TwoWay,
        propertyChanged: (b, _, _) => ((ErrorBanner)b).OnMessageChanged());

    public static readonly BindableProperty IsWarningProperty = BindableProperty.Create(
        nameof(IsWarning), typeof(bool), typeof(ErrorBanner), false,
        propertyChanged: (b, _, _) => ((ErrorBanner)b).ApplyKind());

    public string? Message
    {
        get => (string?)GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }

    public bool IsWarning
    {
        get => (bool)GetValue(IsWarningProperty);
        set => SetValue(IsWarningProperty, value);
    }

    private readonly Border _card;
    private readonly Label _icon;
    private readonly Label _text;

    public ErrorBanner()
    {
        VerticalOptions = LayoutOptions.End;
        Margin = new Thickness(12, 0, 12, 12);
        MaximumWidthRequest = 640;
        IsVisible = false;
        ZIndex = 10;

        _icon = new Label { FontSize = 20 };
        _icon.SetDynamicResource(StyleProperty, "Icon");

        _text = new Label { FontSize = 14, VerticalOptions = LayoutOptions.Center };
        _text.SetDynamicResource(Label.TextColorProperty, "TextPrimary");

        var close = new Label { Text = IconGlyphs.Close, FontSize = 20, Padding = new Thickness(6) };
        close.SetDynamicResource(StyleProperty, "Icon");
        SemanticProperties.SetDescription(close, "Zatvori poruku");
        close.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(() => Message = null) });

        var grid = new Grid
        {
            ColumnDefinitions = { new(GridLength.Auto), new(GridLength.Star), new(GridLength.Auto) },
            ColumnSpacing = 10
        };
        grid.Add(_icon, 0);
        grid.Add(_text, 1);
        grid.Add(close, 2);

        _card = new Border
        {
            Padding = new Thickness(14, 10, 8, 10),
            StrokeThickness = 1,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 12 },
            Shadow = new Shadow { Brush = Brush.Black, Opacity = 0.4f, Radius = 12, Offset = new Point(0, 4) },
            Content = grid
        };

        Content = _card;
        ApplyKind();
    }

    private void ApplyKind()
    {
        _icon.Text = IsWarning ? IconGlyphs.Offline : IconGlyphs.Error;
        _icon.TextColor = ThemeColors.Get(IsWarning ? "ScoreMid" : "DangerText", IsWarning ? "#E3B341" : "#FF7B72");
        _card.BackgroundColor = ThemeColors.Get(IsWarning ? "WarningSurface" : "ErrorSurface", "#3A1E22");
        _card.Stroke = _icon.TextColor;
    }

    private async void OnMessageChanged()
    {
        var text = Message;
        if (string.IsNullOrWhiteSpace(text))
        {
            IsVisible = false;
            return;
        }

        _text.Text = text;
        if (IsVisible) return;

        IsVisible = true;
        Opacity = 0;
        TranslationY = 16;
        SemanticScreenReader.Announce(text);

        await Task.WhenAll(this.FadeToAsync(1, 180), this.TranslateToAsync(0, 0, 180, Easing.CubicOut));
    }
}
