namespace AniRankApp.Helpers;

/// <summary>Eye button next to a password field: shows / hides the typed characters.</summary>
public static class PasswordVisibility
{
    public static void Toggle(Entry entry, Button toggle)
    {
        entry.IsPassword = !entry.IsPassword;

        var key = entry.IsPassword ? "IconVisibility" : "IconVisibilityOff";
        if (Application.Current?.Resources.TryGetValue(key, out var glyph) == true)
            toggle.Text = (string)glyph;

        SemanticProperties.SetDescription(toggle, entry.IsPassword ? "Prikaži lozinku" : "Sakrij lozinku");
    }
}
