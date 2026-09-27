# Appearance preferences and native acceptance

Settings → **Appearance** offers Light/Dark and 100%/150%/200% text size. **Save Changes** commits preferences to the selected profile, updates dynamic resources across windows, then closes Settings. Changes are not previewed before Save, so closing without saving leaves the appearance unchanged. Each launch reads that profile's saved preferences. Legacy files use Light/100%; invalid values/main/backup files stay preserved and show recovery guidance.

Font resources are multiplied by 1, 1.5 or 2. This is real FontSize scaling, not a DPI change or RenderTransform. Main, Settings and Details use vertical scrolling, auto content heights and wrapping actions. The three-part DatePicker has horizontal scrolling when needed; its supported Fluent width/item-height resources are also scaled to keep larger text readable. Its popup retains the existing date-selection behavior. No data category or clock rule changes with appearance. The PDF remains a separate, fixed report layout.

Opaque Window/Card/Soft/Hover surfaces define the hierarchy. No Acrylic/Mica/background blur is used, and no blur interaction cost has been measured. Application-owned transitions and pressed scaling are removed; this is not a claim to change OS-wide animation preferences. Light/Dark are explicit app choices rather than automatic high-contrast mode handling.

## Source palette measurements

sRGB relative luminance, (lighter + .05)/(darker + .05). Values below are the minimum across the Window/Card/Soft/Hover surfaces, except category text which appears on CardSurface. Application brushes are fully opaque, so there is no unknown desktop background in these calculations.

| Token/check | Light | Dark |
| --- | ---: | ---: |
| Primary text | 11.01:1 | 8.31:1 |
| Secondary text | 5.81:1 | 5.68:1 |
| Accent text | 6.51:1 | 5.67:1 |
| Warning text | 5.16:1 | 5.81:1 |
| Control boundary | 3.26:1 | 3.50:1 |
| Keyboard focus outline | 6.51:1 | 6.73:1 |
| Focus/audio/rest text, minimum on card | 5.75:1 | 8.03:1 |
| Adjacent measured-time bar segments, minimum | 3.17:1 | 3.26:1 |

These are application-token calculations, not an audit of rendered native popup/disabled/host states. The 2px keyboard FocusAdorner uses the focus token; pointer focus does not substitute for a keyboard focus check. Normal widget keyboard behavior is retained.

## Evidence and pending native checks

The 38 behavior tests include the earlier 26 tracking/profile checks, legacy settings upgrade, settings/backup preservation, restart persistence, invalid appearance save refusal and reminder reload while tracking remains active. They do not instantiate a native window or prove a layout screenshot. No tests merely assert XAML strings/style structure.

Use the new package and a disposable **absolute** `--data-directory`, never the normal data folder. Record commit/package hash, Windows version and display scaling. Do not change OS display settings for this test.

1. Start with tracking off. Open Settings; ensure startup is disabled and the supplied folder is visible. Select Dark/200%, Save; reopen Settings and confirm both choices. Close/restart with the same profile and confirm persistence. Repeat Light/150% and Light/100%.
2. In both themes, test 200% at the default window size and after narrowing to the allowed minimum. Scroll Main, Settings and Details to reach every control. Period actions/data actions must wrap; labels, durations and reminder interval must stay readable. The date field may scroll horizontally; its popup must show full day/month/year values and accept/cancel correctly.
3. Use Tab/Shift+Tab and keyboard activation to reach Settings, each period, Start/Pause, the date field, Details, appearance selectors, interval controls and Save. Focus must remain visible with the strong outline and scroll into view; no keyboard trap. Open the theme/font lists with keyboard and inspect their popup text/selected states.
4. While tracking in the disposable profile, change appearance only. Totals should continue without reset; Pause should still freeze totals. Check a selected week before/after and export the report. Font preference does not enlarge the PDF.
5. Cancel the history-deletion dialog at 200%; all prompt/buttons must be readable and reachable. Verify native reminder sizing when due. Finish with normal history/settings/Run-entry hashes unchanged. Record any popup clipping, unexpected widget animation or low-contrast state rather than treating source palette results as complete native acceptance.

The appearance UI acceptance, clean-machine installation and public release remain pending until native evidence is recorded. Historical screenshots and native tracking checks apply to the earlier layout.

## Türkçe

Açık/koyu tema ile %100/%150/%200 yazı tercihi yalnız seçilen profilin ayarına kaydedilir. Kaydetmeden pencereyi kapatmak görünümü değiştirmez. Fontlar büyür; DPI/Windows ekran ölçeği veya kişisel başlangıç ayarı değiştirilmez. Yüzeyler opaktır, blur yoktur. Kaynak renk hesapları ve 38 davranış testi, yeni görünümün native klavye ve %200 kabulü yerine geçmez. Yukarıdaki adımları yalnız ayrı deneme profilinde uygulayıp sonuçları kaydet.

The DatePicker sizing overrides follow the pinned [Avalonia 11.2.7 Fluent DatePicker source](https://github.com/AvaloniaUI/Avalonia/blob/11.2.7/src/Avalonia.Themes.Fluent/Controls/DatePicker.xaml); recheck these resource keys when upgrading Avalonia.
