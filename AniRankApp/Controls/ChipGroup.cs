using System.Collections;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Layouts;

namespace AniRankApp.Controls;

/// <summary>
/// Single-choice chips that wrap onto several lines (e.g. watch status on the review sheet).
/// One tap selects - faster than opening a Picker dialog. <see cref="SelectedItem"/> binds two-way.
/// </summary>
public class ChipGroup : ContentView
{
    public static readonly BindableProperty ItemsSourceProperty = BindableProperty.Create(
        nameof(ItemsSource), typeof(IEnumerable), typeof(ChipGroup), null,
        propertyChanged: (b, _, _) => ((ChipGroup)b).Build());

    public static readonly BindableProperty SelectedItemProperty = BindableProperty.Create(
        nameof(SelectedItem), typeof(string), typeof(ChipGroup), null, BindingMode.TwoWay,
        propertyChanged: (b, _, _) => ((ChipGroup)b).Render());

    public IEnumerable? ItemsSource
    {
        get => (IEnumerable?)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public string? SelectedItem
    {
        get => (string?)GetValue(SelectedItemProperty);
        set => SetValue(SelectedItemProperty, value);
    }

    private readonly FlexLayout _flex = new() { Wrap = FlexWrap.Wrap, AlignItems = FlexAlignItems.Start };
    private readonly List<(string Value, Border Chip, Label Text)> _chips = new();

    public ChipGroup()
    {
        Content = _flex;
    }

    private void Build()
    {
        _flex.Children.Clear();
        _chips.Clear();

        if (ItemsSource is null) return;

        foreach (var item in ItemsSource)
        {
            var value = item?.ToString() ?? string.Empty;

            var text = new Label { Text = value };
            text.SetDynamicResource(StyleProperty, "ChipLabelStyle");

            var chip = new Border
            {
                Padding = new Thickness(14, 8),
                Margin = new Thickness(0, 0, 8, 8),
                StrokeThickness = 1,
                StrokeShape = new RoundRectangle { CornerRadius = 18 },
                Content = text
            };
            chip.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(() => SelectedItem = value) });
            SemanticProperties.SetDescription(chip, value);

            _flex.Children.Add(chip);
            _chips.Add((value, chip, text));
        }

        Render();
    }

    private void Render()
    {
        var primary = ThemeColors.Get("Primary", "#5B3AE0");
        var card = ThemeColors.Get("CardBackground", "#1C1C24");
        var stroke = ThemeColors.Get("Gray300", "#34343E");
        var secondary = ThemeColors.Get("TextSecondary", "#A0A0AC");

        foreach (var (value, chip, text) in _chips)
        {
            var selected = value == SelectedItem;
            chip.BackgroundColor = selected ? primary : card;
            chip.Stroke = selected ? primary : stroke;
            text.TextColor = selected ? Colors.White : secondary;
        }
    }
}
