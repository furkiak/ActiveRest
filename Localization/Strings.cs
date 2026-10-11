using System.Collections.Generic;

namespace ActiveRest.Localization
{
    /// <summary>All user-visible text. Keys missing from a language fall back to English.</summary>
    internal static class Strings
    {
        public static readonly IReadOnlyDictionary<string, string> English = new Dictionary<string, string>
        {
            ["unit.h"] = "h", ["unit.m"] = "m", ["unit.s"] = "s",

            ["period.day"] = "Day", ["period.week"] = "Week", ["period.month"] = "Month", ["period.year"] = "Year",
            ["nav.today"] = "Today", ["nav.yesterday"] = "Yesterday",
            ["nav.thisWeek"] = "This week", ["nav.lastWeek"] = "Last week",
            ["nav.thisMonth"] = "This month", ["nav.thisYear"] = "This year",
            ["nav.previous"] = "Previous", ["nav.next"] = "Next", ["nav.pickDate"] = "Pick a date",
            ["nav.backToToday"] = "Back to today",

            ["mode.focus"] = "Focus", ["mode.focus.desc"] = "Active at your computer",
            ["mode.relaxed"] = "Relaxed", ["mode.relaxed.desc"] = "Away, media playing",
            ["mode.rest"] = "Rest", ["mode.rest.desc"] = "Away from the screen",
            ["status.focus"] = "Focusing", ["status.relaxed"] = "Relaxing",
            ["status.rest"] = "On a break", ["status.paused"] = "Tracking paused",

            ["goal.of"] = "of {0} goal", ["goal.reached"] = "Goal reached",
            ["goal.title"] = "Focus goal",
            ["stat.ratio"] = "Focus ratio", ["stat.streak"] = "Day streak", ["stat.streak.value"] = "{0} days",
            ["stat.streak.none"] = "0 days",

            ["chart.day"] = "Focus by hour", ["chart.week"] = "Daily focus",
            ["chart.month"] = "Daily focus", ["chart.year"] = "Monthly focus",
            ["chart.empty"] = "No data for this period yet", ["chart.peak"] = "Peak {0}",

            ["btn.report"] = "Detailed report",
            ["tip.pause"] = "Pause tracking", ["tip.resume"] = "Resume tracking",
            ["tip.pin"] = "Keep window open", ["tip.settings"] = "Settings", ["tip.close"] = "Close",

            ["details.title"] = "Detailed analysis", ["details.subtitle"] = "Where your time went",
            ["details.total"] = "Tracked time",
            ["details.apps"] = "Applications", ["details.apps.empty"] = "No application activity recorded",
            ["details.highlights"] = "Highlights",
            ["insight.peak"] = "Peak hour", ["insight.longest"] = "Longest focus",
            ["insight.top"] = "Top app", ["insight.average"] = "Daily average",
            ["insight.days"] = "Active days", ["insight.none"] = "-", ["insight.tracked"] = "Tracked time",
            ["export.pdf"] = "PDF report", ["export.csv"] = "CSV export",
            ["export.saved"] = "Saved: {0}", ["export.failed"] = "Export failed: {0}",
            ["export.show"] = "Show in folder", ["export.working"] = "Preparing file...",

            ["settings.title"] = "Settings", ["settings.subtitle"] = "Make ActiveRest yours",
            ["settings.appearance"] = "Appearance",
            ["settings.language"] = "Language", ["lang.auto"] = "Auto", ["lang.en"] = "English", ["lang.tr"] = "Türkçe",
            ["settings.theme"] = "Theme", ["theme.system"] = "System", ["theme.light"] = "Light", ["theme.dark"] = "Dark",
            ["settings.eye"] = "Eye care",
            ["eye.enable"] = "20-20-20 reminders", ["eye.enable.desc"] = "A gentle nudge to look away from the screen",
            ["eye.interval"] = "Remind every {0} min",
            ["settings.focus"] = "Focus",
            ["focus.goal"] = "Daily focus goal", ["focus.goal.value"] = "{0}",
            ["focus.idle"] = "Away after", ["focus.idle.value"] = "{0} min of inactivity",
            ["settings.system"] = "System",
            ["startup.enable"] = "Start with Windows", ["startup.desc"] = "Launches quietly in the tray",
            ["settings.data"] = "Data",
            ["data.open"] = "Open data folder", ["data.hint"] = "Everything stays on this device.",
            ["settings.done"] = "Done", ["settings.version"] = "ActiveRest {0}",

            ["toast.title"] = "Rest your eyes",
            ["toast.body"] = "Look at something 20 feet (6 m) away for 20 seconds.",
            ["toast.snooze"] = "Snooze 5 min", ["toast.dismiss"] = "Got it",

            ["tray.open"] = "Open ActiveRest", ["tray.pause"] = "Pause tracking", ["tray.resume"] = "Resume tracking",
            ["tray.settings"] = "Settings", ["tray.exit"] = "Quit",
            ["tray.tooltip"] = "ActiveRest · {0} focus today",
            ["tray.tooltip.paused"] = "ActiveRest · paused",

            ["report.title"] = "Productivity report", ["report.period"] = "Period",
            ["report.generated"] = "Generated {0}",
            ["report.daily"] = "Daily breakdown", ["report.apps"] = "Applications",
            ["report.date"] = "Date", ["report.focus"] = "Focus", ["report.relaxed"] = "Relaxed", ["report.rest"] = "Rest",
            ["report.application"] = "Application", ["report.duration"] = "Duration", ["report.share"] = "Share",
            ["report.footer"] = "ActiveRest report",
            ["report.summary"] = "Summary",
            ["csv.seconds"] = "Seconds",
        };

        public static readonly IReadOnlyDictionary<string, string> Turkish = new Dictionary<string, string>
        {
            ["unit.h"] = "sa", ["unit.m"] = "dk", ["unit.s"] = "sn",

            ["period.day"] = "Gün", ["period.week"] = "Hafta", ["period.month"] = "Ay", ["period.year"] = "Yıl",
            ["nav.today"] = "Bugün", ["nav.yesterday"] = "Dün",
            ["nav.thisWeek"] = "Bu hafta", ["nav.lastWeek"] = "Geçen hafta",
            ["nav.thisMonth"] = "Bu ay", ["nav.thisYear"] = "Bu yıl",
            ["nav.previous"] = "Önceki", ["nav.next"] = "Sonraki", ["nav.pickDate"] = "Tarih seç",
            ["nav.backToToday"] = "Bugüne dön",

            ["mode.focus"] = "Odak", ["mode.focus.desc"] = "Bilgisayar başında aktif",
            ["mode.relaxed"] = "Rahat", ["mode.relaxed.desc"] = "Uzakta, medya oynuyor",
            ["mode.rest"] = "Mola", ["mode.rest.desc"] = "Ekrandan uzakta",
            ["status.focus"] = "Odaktasın", ["status.relaxed"] = "Rahat modda",
            ["status.rest"] = "Moladasın", ["status.paused"] = "Takip duraklatıldı",

            ["goal.of"] = "{0} hedefin", ["goal.reached"] = "Hedef tamam",
            ["goal.title"] = "Odak hedefi",
            ["stat.ratio"] = "Odak oranı", ["stat.streak"] = "Seri", ["stat.streak.value"] = "{0} gün",
            ["stat.streak.none"] = "0 gün",

            ["chart.day"] = "Saatlik odak", ["chart.week"] = "Günlük odak",
            ["chart.month"] = "Günlük odak", ["chart.year"] = "Aylık odak",
            ["chart.empty"] = "Bu dönem için henüz veri yok", ["chart.peak"] = "En yüksek {0}",

            ["btn.report"] = "Ayrıntılı rapor",
            ["tip.pause"] = "Takibi duraklat", ["tip.resume"] = "Takibi sürdür",
            ["tip.pin"] = "Pencereyi açık tut", ["tip.settings"] = "Ayarlar", ["tip.close"] = "Kapat",

            ["details.title"] = "Ayrıntılı analiz", ["details.subtitle"] = "Zamanın nereye gitti",
            ["details.total"] = "İzlenen süre",
            ["details.apps"] = "Uygulamalar", ["details.apps.empty"] = "Kayıtlı uygulama etkinliği yok",
            ["details.highlights"] = "Öne çıkanlar",
            ["insight.peak"] = "En verimli saat", ["insight.longest"] = "En uzun odak",
            ["insight.top"] = "En çok kullanılan", ["insight.average"] = "Günlük ortalama",
            ["insight.days"] = "Aktif gün", ["insight.none"] = "-", ["insight.tracked"] = "İzlenen süre",
            ["export.pdf"] = "PDF rapor", ["export.csv"] = "CSV aktar",
            ["export.saved"] = "Kaydedildi: {0}", ["export.failed"] = "Dışa aktarma başarısız: {0}",
            ["export.show"] = "Klasörde göster", ["export.working"] = "Dosya hazırlanıyor...",

            ["settings.title"] = "Ayarlar", ["settings.subtitle"] = "ActiveRest'i kendine göre ayarla",
            ["settings.appearance"] = "Görünüm",
            ["settings.language"] = "Dil", ["lang.auto"] = "Otomatik", ["lang.en"] = "English", ["lang.tr"] = "Türkçe",
            ["settings.theme"] = "Tema", ["theme.system"] = "Sistem", ["theme.light"] = "Açık", ["theme.dark"] = "Koyu",
            ["settings.eye"] = "Göz sağlığı",
            ["eye.enable"] = "20-20-20 hatırlatıcısı", ["eye.enable.desc"] = "Ekrandan uzağa bakman için nazik bir uyarı",
            ["eye.interval"] = "Her {0} dakikada bir hatırlat",
            ["settings.focus"] = "Odak",
            ["focus.goal"] = "Günlük odak hedefi", ["focus.goal.value"] = "{0}",
            ["focus.idle"] = "Uzakta sayılma süresi", ["focus.idle.value"] = "{0} dk hareketsizlik",
            ["settings.system"] = "Sistem",
            ["startup.enable"] = "Windows ile başlat", ["startup.desc"] = "Sistem tepsisinde sessizce açılır",
            ["settings.data"] = "Veri",
            ["data.open"] = "Veri klasörünü aç", ["data.hint"] = "Tüm veriler yalnızca bu cihazda kalır.",
            ["settings.done"] = "Tamam", ["settings.version"] = "ActiveRest {0}",

            ["toast.title"] = "Gözlerini dinlendir",
            ["toast.body"] = "20 saniye boyunca 6 metre uzaktaki bir şeye bak.",
            ["toast.snooze"] = "5 dk ertele", ["toast.dismiss"] = "Tamam",

            ["tray.open"] = "ActiveRest'i aç", ["tray.pause"] = "Takibi duraklat", ["tray.resume"] = "Takibi sürdür",
            ["tray.settings"] = "Ayarlar", ["tray.exit"] = "Çık",
            ["tray.tooltip"] = "ActiveRest · bugün {0} odak",
            ["tray.tooltip.paused"] = "ActiveRest · duraklatıldı",

            ["report.title"] = "Verimlilik raporu", ["report.period"] = "Dönem",
            ["report.generated"] = "Oluşturulma: {0}",
            ["report.daily"] = "Günlük döküm", ["report.apps"] = "Uygulamalar",
            ["report.date"] = "Tarih", ["report.focus"] = "Odak", ["report.relaxed"] = "Rahat", ["report.rest"] = "Mola",
            ["report.application"] = "Uygulama", ["report.duration"] = "Süre", ["report.share"] = "Pay",
            ["report.footer"] = "ActiveRest raporu",
            ["report.summary"] = "Özet",
            ["csv.seconds"] = "Saniye",
        };
    }
}
