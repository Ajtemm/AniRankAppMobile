using Microsoft.Maui.Controls.Shapes;

namespace AniRankApp.Controls;

/// <summary>
/// Tap-to-rate input: ten numbered cells, 1-10. Every cell up to the chosen value is filled
/// with the score colour, so the choice reads like a bar. <see cref="Value"/> binds two-way.
/// </summary>
public class RatingPicker : ContentView
{
    private const int Max = 10;

    public static readonly BindableProperty ValueProperty = BindableProperty.Create(
        nameof(Value), typeof(double), typeof(RatingPicker), 0.0, BindingMode.TwoWay,
        propertyChanged: (b, _, _) => ((RatingPicker)b).Render());

    public double Value
    {
        get => (double)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    private readonly List<(Border Cell, Label Text)> _cells = new();

    public RatingPicker()
    {
        var grid = new Grid { ColumnSpacing = 4 };

        for (var i = 1; i <= Max; i++)
        {
            grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));

            var text = new Label
            {
                Text = i.ToString(),
                FontFamily = "OpenSansSemibold",
                FontSize = 14,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center
            };

            var cell = new Border
            {
                HeightRequest = 42,
                StrokeThickness = 1,
                StrokeShape = new RoundRectangle { CornerRadius = 10 },
                Content = text
            };

            var score = i;
            cell.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(() => Value = score) });
            SemanticProperties.SetDescription(cell, $"Ocena {score} od {Max}");

            grid.Add(cell, i - 1);
            _cells.Add((cell, text));
        }

        Content = grid;
        Render();
    }

    private void Render()
    {
        var chosen = (int)Math.Round(Value, MidpointRounding.AwayFromZero);
        var fill = ThemeColors.ForRating(Value, Max);
        var idle = ThemeColors.Get("CardBackgroundAlt", "#262631");
        var idleStroke = ThemeColors.Get("Gray300", "#34343E");
        var idleText = ThemeColors.Get("TextSecondary", "#A0A0AC");

        for (var i = 0; i < _cells.Count; i++)
        {
            var (cell, text) = _cells[i];
            var on = i < chosen;

            cell.BackgroundColor = on ? fill : idle;
            cell.Stroke = on ? fill : idleStroke;
            text.TextColor = on ? ThemeColors.OnScore : idleText;
        }
    }
}
