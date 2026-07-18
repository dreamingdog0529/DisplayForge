using System.Globalization;
using System.Windows;
using DisplayForge.Resources;

namespace DisplayForge.Services;

public sealed record LanguageOption(string Code, string DisplayName);

public interface ILocalizationService
{
    string this[string key] { get; }
    string CurrentLanguage { get; }
    IReadOnlyList<LanguageOption> GetLanguageOptions();
    void ApplyLanguage(string languageCode);
    string ResolveLanguageCode(string settingValue);
    bool IsSupportedLanguage(string languageCode);
}

public sealed class LocalizationService : ILocalizationService
{
    /// <summary>
    /// Supported UI cultures (resource satellite names). Display names are native.
    /// </summary>
    public static readonly IReadOnlyList<(string Code, string NativeName)> SupportedLanguages =
    [
        ("en", "English"),
        ("ja", "日本語"),
        ("zh-Hans", "简体中文"),
        ("zh-Hant", "繁體中文"),
        ("ko", "한국어"),
        ("de", "Deutsch"),
        ("fr", "Français"),
        ("es", "Español"),
        ("pt-BR", "Português (Brasil)"),
        ("pt-PT", "Português (Portugal)"),
        ("it", "Italiano"),
        ("nl", "Nederlands"),
        ("pl", "Polski"),
        ("ru", "Русский"),
        ("uk", "Українська"),
        ("tr", "Türkçe"),
        ("cs", "Čeština"),
        ("sv", "Svenska"),
        ("da", "Dansk"),
        ("nb", "Norsk bokmål"),
        ("fi", "Suomi"),
        ("hu", "Magyar"),
        ("ro", "Română"),
        ("el", "Ελληνικά"),
        ("vi", "Tiếng Việt"),
        ("th", "ไทย"),
        ("id", "Bahasa Indonesia"),
        ("ms", "Bahasa Melayu"),
        ("hi", "हिन्दी"),
        ("ar", "العربية"),
        ("he", "עברית"),
    ];

    private static readonly Dictionary<string, string> CodeLookup =
        SupportedLanguages.ToDictionary(x => x.Code, x => x.Code, StringComparer.OrdinalIgnoreCase);

    public string CurrentLanguage { get; private set; } = "en";

    public string this[string key] => Strings.ResourceManager.GetString(key, Strings.Culture) ?? key;

    public IReadOnlyList<LanguageOption> GetLanguageOptions()
    {
        var list = new List<LanguageOption>(SupportedLanguages.Count + 1)
        {
            new("auto", Strings.LanguageAuto)
        };
        foreach (var (code, nativeName) in SupportedLanguages)
            list.Add(new LanguageOption(code, nativeName));
        return list;
    }

    public bool IsSupportedLanguage(string languageCode) =>
        !string.IsNullOrWhiteSpace(languageCode)
        && CodeLookup.ContainsKey(languageCode);

    public string ResolveLanguageCode(string settingValue)
    {
        if (!string.IsNullOrWhiteSpace(settingValue)
            && !settingValue.Equals("auto", StringComparison.OrdinalIgnoreCase)
            && CodeLookup.TryGetValue(settingValue.Trim(), out var exact))
        {
            return exact;
        }

        return MatchSystemCulture(CultureInfo.CurrentUICulture);
    }

    public void ApplyLanguage(string languageCode)
    {
        var code = ResolveLanguageCode(languageCode);
        CurrentLanguage = code;
        var culture = CreateCulture(code);
        CultureInfo.CurrentUICulture = culture;
        CultureInfo.CurrentCulture = culture;
        Strings.Culture = culture;

        var flow = culture.TextInfo.IsRightToLeft
            ? FlowDirection.RightToLeft
            : FlowDirection.LeftToRight;

        if (Application.Current is null)
            return;

        foreach (Window window in Application.Current.Windows)
        {
            window.Language = System.Windows.Markup.XmlLanguage.GetLanguage(culture.IetfLanguageTag);
            window.FlowDirection = flow;
        }
    }

    private static CultureInfo CreateCulture(string code)
    {
        try
        {
            return CultureInfo.GetCultureInfo(code);
        }
        catch (CultureNotFoundException)
        {
            return CultureInfo.GetCultureInfo("en");
        }
    }

    private static string MatchSystemCulture(CultureInfo culture)
    {
        for (var c = culture; c is not null && !Equals(c, CultureInfo.InvariantCulture); c = c.Parent)
        {
            var mapped = MapCultureName(c.Name);
            if (mapped is not null)
                return mapped;

            if (CodeLookup.TryGetValue(c.Name, out var byName))
                return byName;

            if (CodeLookup.TryGetValue(c.TwoLetterISOLanguageName, out var byIso))
                return byIso;
        }

        return "en";
    }

    /// <summary>
    /// Map common Windows culture names to our satellite resource cultures.
    /// </summary>
    private static string? MapCultureName(string name)
    {
        if (string.IsNullOrEmpty(name))
            return null;

        // Chinese variants
        if (name.Equals("zh-Hans", StringComparison.OrdinalIgnoreCase)
            || name.StartsWith("zh-Hans-", StringComparison.OrdinalIgnoreCase)
            || name.Equals("zh-CN", StringComparison.OrdinalIgnoreCase)
            || name.Equals("zh-SG", StringComparison.OrdinalIgnoreCase)
            || name.Equals("zh-CHS", StringComparison.OrdinalIgnoreCase))
            return "zh-Hans";

        if (name.Equals("zh-Hant", StringComparison.OrdinalIgnoreCase)
            || name.StartsWith("zh-Hant-", StringComparison.OrdinalIgnoreCase)
            || name.Equals("zh-TW", StringComparison.OrdinalIgnoreCase)
            || name.Equals("zh-HK", StringComparison.OrdinalIgnoreCase)
            || name.Equals("zh-MO", StringComparison.OrdinalIgnoreCase)
            || name.Equals("zh-CHT", StringComparison.OrdinalIgnoreCase))
            return "zh-Hant";

        // Portuguese
        if (name.Equals("pt-BR", StringComparison.OrdinalIgnoreCase)
            || name.StartsWith("pt-BR-", StringComparison.OrdinalIgnoreCase))
            return "pt-BR";
        if (name.Equals("pt-PT", StringComparison.OrdinalIgnoreCase)
            || name.StartsWith("pt-PT-", StringComparison.OrdinalIgnoreCase))
            return "pt-PT";
        // Bare "pt" and other regional variants (AO, MZ, …) → Brazilian (larger speaker base)
        if (name.Equals("pt", StringComparison.OrdinalIgnoreCase)
            || name.StartsWith("pt-", StringComparison.OrdinalIgnoreCase))
            return "pt-BR";

        // Norwegian: nb / nn / no
        if (name.Equals("nb", StringComparison.OrdinalIgnoreCase)
            || name.StartsWith("nb-", StringComparison.OrdinalIgnoreCase)
            || name.Equals("nn", StringComparison.OrdinalIgnoreCase)
            || name.StartsWith("nn-", StringComparison.OrdinalIgnoreCase)
            || name.Equals("no", StringComparison.OrdinalIgnoreCase)
            || name.StartsWith("no-", StringComparison.OrdinalIgnoreCase))
            return "nb";

        // Hebrew historical iw
        if (name.Equals("iw", StringComparison.OrdinalIgnoreCase)
            || name.StartsWith("iw-", StringComparison.OrdinalIgnoreCase))
            return "he";

        return null;
    }
}
