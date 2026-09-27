using ActiveRest.Models;
using Avalonia;
using Avalonia.Styling;
using System;

namespace ActiveRest.Services;

public static class AppearanceService
{
    public static void Apply(AppSettings settings)
    {
        if (settings.AppearanceTheme is not ("Light" or "Dark") || settings.TextScalePercent is not (100 or 150 or 200))
            throw new ArgumentException("Unsupported appearance preference.", nameof(settings));
        if (Application.Current is not { } application) return;
        var scale = settings.TextScalePercent / 100d;
        application.Resources["BodyFontSize"] = 14d * scale;
        application.Resources["CaptionFontSize"] = 12d * scale;
        application.Resources["SectionFontSize"] = 17d * scale;
        application.Resources["HeadingFontSize"] = 22d * scale;
        application.Resources["TitleFontSize"] = 26d * scale;
        application.Resources["DatePickerThemeMinWidth"] = 296d * scale;
        application.Resources["DatePickerThemeMaxWidth"] = 456d * scale;
        application.Resources["DatePickerFlyoutWidth"] = 296d * scale;
        application.Resources["DatePickerFlyoutPresenterItemHeight"] = 40d * scale;
        application.Resources["DatePickerFlyoutPresenterHighlightHeight"] = 40d * scale;
        application.Resources["DatePickerFlyoutPresenterAcceptDismissHostGridHeight"] = 41d * scale;
        application.RequestedThemeVariant = settings.AppearanceTheme == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light;
    }
}
