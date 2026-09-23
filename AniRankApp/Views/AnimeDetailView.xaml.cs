using System.ComponentModel;
using AniRankApp.Helpers;
using AniRankApp.Models;
using AniRankApp.ViewModels;

namespace AniRankApp.Views;

public partial class AnimeDetailView : ContentPage
{
    private const uint SheetAnimationMs = 220;

    private readonly AnimeDetailViewModel _vm;

    public AnimeDetailView() : this(ServiceHelper.GetService<AnimeDetailViewModel>()) { }

    public AnimeDetailView(AnimeDetailViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
        _vm.PropertyChanged += OnViewModelPropertyChanged;
    }

    /// <summary>Hardware / gesture back closes the review sheet before leaving the page.</summary>
    protected override bool OnBackButtonPressed()
    {
        if (_vm.IsEditorOpen)
        {
            _vm.CloseEditorCommand.Execute(null);
            return true;
        }

        return base.OnBackButtonPressed();
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(AnimeDetailViewModel.IsEditorOpen))
            _ = _vm.IsEditorOpen ? ShowSheetAsync() : HideSheetAsync();
    }

    /// <summary>Slides the sheet up from below the screen edge while the scrim fades in.</summary>
    private async Task ShowSheetAsync()
    {
        SheetOverlay.IsVisible = true;
        Sheet.TranslationY = Math.Max(Height, 600);

        await Task.WhenAll(
            SheetScrim.FadeToAsync(1, SheetAnimationMs),
            Sheet.TranslateToAsync(0, 0, SheetAnimationMs, Easing.CubicOut));
    }

    private async Task HideSheetAsync()
    {
        await Task.WhenAll(
            SheetScrim.FadeToAsync(0, SheetAnimationMs),
            Sheet.TranslateToAsync(0, Math.Max(Sheet.Height, 400), SheetAnimationMs, Easing.CubicIn));

        // The user may have reopened it while the close animation ran.
        if (!_vm.IsEditorOpen)
            SheetOverlay.IsVisible = false;
    }

    private void OnLikeClicked(object? sender, EventArgs e)
    {
        if (sender is Button { BindingContext: Review review })
            _vm.ToggleLikeCommand.Execute(review);
    }
}
