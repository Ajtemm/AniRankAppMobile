using AniRankApp.Helpers;
using AniRankApp.Models;
using AniRankApp.ViewModels;

namespace AniRankApp.Views;

public partial class AdminUserReviewsView : ContentPage
{
    private readonly AdminUserReviewsViewModel _vm;

    public AdminUserReviewsView() : this(ServiceHelper.GetService<AdminUserReviewsViewModel>()) { }

    public AdminUserReviewsView(AdminUserReviewsViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    private void OnDeleteReviewClicked(object? sender, EventArgs e)
    {
        if (sender is Button { BindingContext: Review review })
            _vm.DeleteReviewCommand.Execute(review);
    }
}
