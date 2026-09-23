using System.Windows.Input;

namespace AniRankApp.Controls;

/// <summary>
/// Centered "nothing here" block for CollectionView.EmptyView: icon, title, explanation and an
/// optional call-to-action button (hidden when <see cref="ActionText"/> is empty).
/// </summary>
public class EmptyState : ContentView
{
    public static readonly BindableProperty GlyphProperty = BindableProperty.Create(
        nameof(Glyph), typeof(string), typeof(EmptyState), IconGlyphs.Inbox, propertyChanged: Refresh);

    public static readonly BindableProperty TitleProperty = BindableProperty.Create(
        nameof(Title), typeof(string), typeof(EmptyState), string.Empty, propertyChanged: Refresh);

    public static readonly BindableProperty MessageProperty = BindableProperty.Create(
        nameof(Message), typeof(string), typeof(EmptyState), string.Empty, propertyChanged: Refresh);

    public static readonly BindableProperty ActionTextProperty = BindableProperty.Create(
        nameof(ActionText), typeof(string), typeof(EmptyState), string.Empty, propertyChanged: Refresh);

    public static readonly BindableProperty ActionCommandProperty = BindableProperty.Create(
        nameof(ActionCommand), typeof(ICommand), typeof(EmptyState), null, propertyChanged: Refresh);

    public string Glyph
    {
        get => (string)GetValue(GlyphProperty);
        set => SetValue(GlyphProperty, value);
    }

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string Message
    {
        get => (string)GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }

    public string ActionText
    {
        get => (string)GetValue(ActionTextProperty);
        set => SetValue(ActionTextProperty, value);
    }

    public ICommand? ActionCommand
    {
        get => (ICommand?)GetValue(ActionCommandProperty);
        set => SetValue(ActionCommandProperty, value);
    }

    private readonly Label _glyph;
    private readonly Label _title;
    private readonly Label _message;
    private readonly Button _action;

    public EmptyState()
    {
        _glyph = new Label { FontSize = 48, HorizontalOptions = LayoutOptions.Center };
        _glyph.SetDynamicResource(StyleProperty, "Icon");
        _glyph.SetDynamicResource(Label.TextColorProperty, "Gray500");

        _title = new Label { HorizontalTextAlignment = TextAlignment.Center, FontSize = 16 };
        _title.SetDynamicResource(StyleProperty, "SectionTitle");

        _message = new Label { HorizontalTextAlignment = TextAlignment.Center };
        _message.SetDynamicResource(StyleProperty, "Caption");

        _action = new Button { HorizontalOptions = LayoutOptions.Center, Margin = new Thickness(0, 8, 0, 0) };
        _action.SetDynamicResource(StyleProperty, "SubtleButton");

        Content = new VerticalStackLayout
        {
            Spacing = 6,
            Padding = new Thickness(32, 48),
            MaximumWidthRequest = 420,
            HorizontalOptions = LayoutOptions.Center,
            Children = { _glyph, _title, _message, _action }
        };

        Update();
    }

    private static void Refresh(BindableObject bindable, object oldValue, object newValue)
        => ((EmptyState)bindable).Update();

    private void Update()
    {
        _glyph.Text = Glyph;
        _title.Text = Title;
        _title.IsVisible = !string.IsNullOrWhiteSpace(Title);
        _message.Text = Message;
        _message.IsVisible = !string.IsNullOrWhiteSpace(Message);
        _action.Text = ActionText;
        _action.Command = ActionCommand;
        _action.IsVisible = !string.IsNullOrWhiteSpace(ActionText) && ActionCommand is not null;
    }
}
