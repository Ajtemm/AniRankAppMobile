using AniRankApp.Helpers;
using AniRankApp.Models;
using AniRankApp.ViewModels;

namespace AniRankApp.Views;

public partial class MyReviewsView : ContentPage
{
    private readonly MyReviewsViewModel _vm;

    public MyReviewsView() : this(ServiceHelper.GetService<MyReviewsViewModel>()) { }

    public MyReviewsView(MyReviewsViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.LoadAsync();
    }

    private void OnDeleteClicked(object? sender, EventArgs e)
    {
        if (sender is Button { BindingContext: Review review })
            _vm.DeleteReviewCommand.Execute(review);
    }
}
