using System.Text;

// Generates installer/DisplayForge.Installer/Loc/*.wxl
// Run: dotnet run --project tools/GenInstallerLocales

static class Program
{
    static int Main()
    {
        var repoRoot = FindRepoRoot();
        var outDir = Path.Combine(repoRoot, "installer", "DisplayForge.Installer", "Loc");
        Directory.CreateDirectory(outDir);

        foreach (var f in Directory.EnumerateFiles(outDir, "*.wxl"))
            File.Delete(f);

        var locales = BuildLocales();
        foreach (var (culture, loc) in locales)
        {
            var sb = new StringBuilder();
            sb.AppendLine("""<?xml version="1.0" encoding="utf-8"?>""");
            sb.AppendLine($"""<WixLocalization xmlns="http://wixtoolset.org/schemas/v4/wxl" Culture="{culture}" Language="{loc.Lcid}">""");
            WriteString(sb, "PackageLanguage", loc.Lcid.ToString());
            WriteString(sb, "DowngradeError", loc.DowngradeError);
            WriteString(sb, "SummaryDescription", loc.SummaryDescription);
            WriteString(sb, "ShortcutDescription", loc.ShortcutDescription);
            WriteString(sb, "OptionsDlgWindowTitle", loc.OptionsDlgWindowTitle);
            WriteString(sb, "OptionsDlgBannerTitle", loc.OptionsDlgBannerTitle);
            WriteString(sb, "OptionsDlgDescription", loc.OptionsDlgDescription);
            WriteString(sb, "OptionsDlgStartupCheck", loc.OptionsDlgStartupCheck);
            WriteString(sb, "OptionsDlgStartupHint", loc.OptionsDlgStartupHint);
            WriteString(sb, "ExitDialogLaunchCheck", ExitDialogLaunchCheck(culture));
            sb.AppendLine("</WixLocalization>");

            var path = Path.Combine(outDir, $"{culture}.wxl");
            File.WriteAllText(path, sb.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            Console.WriteLine($"Wrote {path}");
        }

        Console.WriteLine($"Done. {locales.Count} cultures.");
        return 0;
    }

    static void WriteString(StringBuilder sb, string id, string value) =>
        sb.AppendLine($"""  <String Id="{id}" Value="{XmlEscape(value)}" />""");

    static string XmlEscape(string s) =>
        s.Replace("&", "&amp;", StringComparison.Ordinal)
         .Replace("<", "&lt;", StringComparison.Ordinal)
         .Replace(">", "&gt;", StringComparison.Ordinal)
         .Replace("\"", "&quot;", StringComparison.Ordinal);

    /// <summary>ExitDialog checkbox: launch the app after install (default checked in Package.wxs).</summary>
    static string ExitDialogLaunchCheck(string culture) => culture switch
    {
        "ja-JP" => "DisplayForge を起動",
        "zh-CN" => "启动 DisplayForge",
        "zh-TW" => "啟動 DisplayForge",
        "ko-KR" => "DisplayForge 시작",
        "de-DE" => "DisplayForge starten",
        "fr-FR" => "Lancer DisplayForge",
        "es-ES" => "Iniciar DisplayForge",
        "pt-BR" => "Iniciar o DisplayForge",
        "pt-PT" => "Iniciar o DisplayForge",
        "it-IT" => "Avvia DisplayForge",
        "nl-NL" => "DisplayForge starten",
        "pl-PL" => "Uruchom DisplayForge",
        "ru-RU" => "Запустить DisplayForge",
        "uk-UA" => "Запустити DisplayForge",
        "tr-TR" => "DisplayForge'u başlat",
        "cs-CZ" => "Spustit DisplayForge",
        "sv-SE" => "Starta DisplayForge",
        "da-DK" => "Start DisplayForge",
        "nb-NO" => "Start DisplayForge",
        "fi-FI" => "Käynnistä DisplayForge",
        "hu-HU" => "DisplayForge indítása",
        "ro-RO" => "Pornește DisplayForge",
        "el-GR" => "Εκκίνηση DisplayForge",
        "vi-VN" => "Khởi động DisplayForge",
        "th-TH" => "เริ่ม DisplayForge",
        "id-ID" => "Jalankan DisplayForge",
        "ms-MY" => "Mulakan DisplayForge",
        "hi-IN" => "DisplayForge शुरू करें",
        "ar-SA" => "تشغيل DisplayForge",
        "he-IL" => "הפעל את DisplayForge",
        _ => "Launch DisplayForge",
    };

    static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "DisplayForge.sln")))
                return dir.FullName;
            dir = dir.Parent;
        }
        return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
    }

    sealed record Loc(
        int Lcid,
        string DowngradeError,
        string SummaryDescription,
        string ShortcutDescription,
        string OptionsDlgWindowTitle,
        string OptionsDlgBannerTitle,
        string OptionsDlgDescription,
        string OptionsDlgStartupCheck,
        string OptionsDlgStartupHint);

    static Dictionary<string, Loc> BuildLocales() => new(StringComparer.OrdinalIgnoreCase)
    {
        ["en-US"] = new(
            1033,
            "A newer version of [ProductName] is already installed.",
            "Multi-monitor profile switcher with global hotkeys",
            "Multi-monitor profile switcher",
            "[ProductName] Setup",
            "Installation Options",
            "Choose optional features for DisplayForge.",
            "Start DisplayForge automatically when I sign in to Windows",
            "The main window opens on first launch by default. Enable \"Start minimized to tray\" later in Settings if you prefer."),

        ["ja-JP"] = new(
            1041,
            "新しいバージョンの [ProductName] が既にインストールされています。",
            "グローバルホットキー対応のマルチモニタープロファイル切替",
            "マルチモニタープロファイル切替",
            "[ProductName] セットアップ",
            "インストールオプション",
            "DisplayForge のオプションを選択してください。",
            "Windows にサインインしたときに DisplayForge を自動で起動する",
            "初回起動ではメインウィンドウが表示されます。トレイのみで始めたい場合は、あとから設定の「起動時はトレイに最小化」を有効にできます。"),

        ["zh-CN"] = new(
            2052,
            "已安装更新版本的 [ProductName]。",
            "支持全局快捷键的多显示器配置切换工具",
            "多显示器配置切换",
            "[ProductName] 安装",
            "安装选项",
            "选择 DisplayForge 的可选功能。",
            "登录 Windows 时自动启动 DisplayForge",
            "适合托盘常驻使用。稍后也可在设置中启用“启动时最小化到托盘”。"),

        ["zh-TW"] = new(
            1028,
            "已安裝較新版本的 [ProductName]。",
            "支援全域快速鍵的多螢幕設定檔切換工具",
            "多螢幕設定檔切換",
            "[ProductName] 安裝",
            "安裝選項",
            "選擇 DisplayForge 的選用功能。",
            "登入 Windows 時自動啟動 DisplayForge",
            "適合系統匣常駐使用。之後也可在設定中啟用「啟動時最小化到系統匣」。"),

        ["ko-KR"] = new(
            1042,
            "더 새로운 버전의 [ProductName]이(가) 이미 설치되어 있습니다.",
            "전역 단축키를 지원하는 멀티 모니터 프로필 전환",
            "멀티 모니터 프로필 전환",
            "[ProductName] 설치",
            "설치 옵션",
            "DisplayForge 선택 기능을 선택하세요.",
            "Windows에 로그인할 때 DisplayForge 자동 시작",
            "트레이 상주 사용에 권장됩니다. 나중에 설정에서 \"시작 시 트레이로 최소화\"도 켤 수 있습니다."),

        ["de-DE"] = new(
            1031,
            "Eine neuere Version von [ProductName] ist bereits installiert.",
            "Multi-Monitor-Profilumschalter mit globalen Hotkeys",
            "Multi-Monitor-Profilumschalter",
            "[ProductName]-Setup",
            "Installationsoptionen",
            "Optionale Funktionen für DisplayForge auswählen.",
            "DisplayForge beim Anmelden an Windows automatisch starten",
            "Empfohlen für die Nutzung im Infobereich. \"Beim Start in den Infobereich minimieren\" können Sie später in den Einstellungen aktivieren."),

        ["fr-FR"] = new(
            1036,
            "Une version plus récente de [ProductName] est déjà installée.",
            "Commutateur de profils multi-écrans avec raccourcis globaux",
            "Commutateur de profils multi-écrans",
            "Installation de [ProductName]",
            "Options d'installation",
            "Choisissez les options de DisplayForge.",
            "Démarrer DisplayForge automatiquement à la connexion Windows",
            "Recommandé pour une utilisation dans la zone de notification. Vous pourrez aussi activer « Démarrer réduit dans la zone de notification » dans les paramètres."),

        ["es-ES"] = new(
            3082,
            "Ya hay instalada una versión más reciente de [ProductName].",
            "Cambiador de perfiles multimonitor con teclas de acceso global",
            "Cambiador de perfiles multimonitor",
            "Instalación de [ProductName]",
            "Opciones de instalación",
            "Elija las opciones de DisplayForge.",
            "Iniciar DisplayForge automáticamente al iniciar sesión en Windows",
            "Recomendado para uso en la bandeja. También puede activar «Iniciar minimizado en la bandeja» más tarde en Configuración."),

        ["pt-BR"] = new(
            1046,
            "Já está instalada uma versão mais recente do [ProductName].",
            "Alternador de perfis multi-monitor com atalhos globais",
            "Alternador de perfis multi-monitor",
            "Instalação do [ProductName]",
            "Opções de instalação",
            "Escolha os recursos opcionais do DisplayForge.",
            "Iniciar o DisplayForge automaticamente ao entrar no Windows",
            "Recomendado para uso na bandeja. Você também pode ativar \"Iniciar minimizado na bandeja\" depois em Configurações."),

        ["pt-PT"] = new(
            2070,
            "Já está instalada uma versão mais recente do [ProductName].",
            "Comutador de perfis multi-monitor com atalhos globais",
            "Comutador de perfis multi-monitor",
            "Instalação do [ProductName]",
            "Opções de instalação",
            "Escolha as funcionalidades opcionais do DisplayForge.",
            "Iniciar o DisplayForge automaticamente ao iniciar sessão no Windows",
            "Recomendado para utilização na bandeja. Também pode ativar \"Iniciar minimizado na bandeja\" mais tarde em Definições."),

        ["it-IT"] = new(
            1040,
            "È già installata una versione più recente di [ProductName].",
            "Commutatore di profili multi-monitor con tasti di scelta rapida globali",
            "Commutatore di profili multi-monitor",
            "Installazione di [ProductName]",
            "Opzioni di installazione",
            "Scegli le opzioni di DisplayForge.",
            "Avvia DisplayForge automaticamente all'accesso a Windows",
            "Consigliato per l'uso nell'area di notifica. Puoi anche abilitare \"Avvia ridotto a icona nell'area di notifica\" in Impostazioni."),

        ["nl-NL"] = new(
            1043,
            "Er is al een nieuwere versie van [ProductName] geïnstalleerd.",
            "Multi-monitorprofielwisselaar met globale sneltoetsen",
            "Multi-monitorprofielwisselaar",
            "[ProductName]-installatie",
            "Installatieopties",
            "Kies optionele functies voor DisplayForge.",
            "DisplayForge automatisch starten bij aanmelden bij Windows",
            "Aanbevolen voor gebruik in het systeemvak. U kunt later in Instellingen ook \"Starten geminimaliseerd naar systeemvak\" inschakelen."),

        ["pl-PL"] = new(
            1045,
            "Nowsza wersja [ProductName] jest już zainstalowana.",
            "Przełącznik profili wielu monitorów z globalnymi skrótami",
            "Przełącznik profili wielu monitorów",
            "Instalacja [ProductName]",
            "Opcje instalacji",
            "Wybierz opcjonalne funkcje DisplayForge.",
            "Uruchamiaj DisplayForge automatycznie po zalogowaniu do Windows",
            "Zalecane przy pracy w zasobniku. Później w Ustawieniach możesz też włączyć „Uruchom zminimalizowane do zasobnika”."),

        ["ru-RU"] = new(
            1049,
            "Уже установлена более новая версия [ProductName].",
            "Переключатель профилей нескольких мониторов с глобальными горячими клавишами",
            "Переключатель профилей мониторов",
            "Установка [ProductName]",
            "Параметры установки",
            "Выберите дополнительные параметры DisplayForge.",
            "Запускать DisplayForge автоматически при входе в Windows",
            "Рекомендуется для работы в области уведомлений. Позже в параметрах можно включить «Запускать свёрнутым в область уведомлений»."),

        ["uk-UA"] = new(
            1058,
            "Новішу версію [ProductName] уже встановлено.",
            "Перемикач профілів кількох моніторів із глобальними гарячими клавішами",
            "Перемикач профілів моніторів",
            "Інсталяція [ProductName]",
            "Параметри інсталяції",
            "Виберіть додаткові параметри DisplayForge.",
            "Запускати DisplayForge автоматично під час входу в Windows",
            "Рекомендовано для роботи в області сповіщень. Пізніше в параметрах можна ввімкнути «Запускати згорнутим в область сповіщень»."),

        ["tr-TR"] = new(
            1055,
            "[ProductName] uygulamasının daha yeni bir sürümü zaten yüklü.",
            "Genel kısayol tuşlarıyla çoklu monitör profili değiştirici",
            "Çoklu monitör profili değiştirici",
            "[ProductName] Kurulumu",
            "Kurulum seçenekleri",
            "DisplayForge için isteğe bağlı özellikleri seçin.",
            "Windows oturumu açıldığında DisplayForge'u otomatik başlat",
            "Sistem tepsisinde kullanım için önerilir. Ayarlar'da daha sonra \"Başlangıçta tepsiye küçült\" seçeneğini de açabilirsiniz."),

        ["cs-CZ"] = new(
            1029,
            "Je již nainstalována novější verze [ProductName].",
            "Přepínač profilů více monitorů s globálními klávesovými zkratkami",
            "Přepínač profilů více monitorů",
            "Instalace [ProductName]",
            "Možnosti instalace",
            "Vyberte volitelné funkce DisplayForge.",
            "Spouštět DisplayForge automaticky při přihlášení k Windows",
            "Doporučeno pro běh v oznamovací oblasti. Později v Nastavení můžete zapnout „Spustit minimalizované do oznamovací oblasti“."),

        ["sv-SE"] = new(
            1053,
            "En nyare version av [ProductName] är redan installerad.",
            "Profilväxlare för flera skärmar med globala snabbkommandon",
            "Profilväxlare för flera skärmar",
            "Installation av [ProductName]",
            "Installationsalternativ",
            "Välj tillval för DisplayForge.",
            "Starta DisplayForge automatiskt när jag loggar in i Windows",
            "Rekommenderas för användning i meddelandefältet. Du kan senare aktivera \"Starta minimerad till meddelandefältet\" i Inställningar."),

        ["da-DK"] = new(
            1030,
            "En nyere version af [ProductName] er allerede installeret.",
            "Profilskifter til flere skærme med globale genvejstaster",
            "Profilskifter til flere skærme",
            "Installation af [ProductName]",
            "Installationsindstillinger",
            "Vælg valgfrie funktioner til DisplayForge.",
            "Start DisplayForge automatisk, når jeg logger på Windows",
            "Anbefales til brug i meddelelsesområdet. Du kan senere aktivere \"Start minimeret til meddelelsesområdet\" under Indstillinger."),

        ["nb-NO"] = new(
            1044,
            "En nyere versjon av [ProductName] er allerede installert.",
            "Profilbytter for flere skjermer med globale hurtigtaster",
            "Profilbytter for flere skjermer",
            "Installasjon av [ProductName]",
            "Installasjonsalternativer",
            "Velg valgfrie funksjoner for DisplayForge.",
            "Start DisplayForge automatisk når jeg logger på Windows",
            "Anbefales for bruk i systemstatusfeltet. Du kan senere aktivere \"Start minimert til systemstatusfeltet\" i Innstillinger."),

        ["fi-FI"] = new(
            1035,
            "Uudempi versio tuotteesta [ProductName] on jo asennettu.",
            "Moninäyttöprofiilien vaihtaja globaaleilla pikanäppäimillä",
            "Moninäyttöprofiilien vaihtaja",
            "[ProductName]-asennus",
            "Asennusasetukset",
            "Valitse DisplayForgen valinnaiset ominaisuudet.",
            "Käynnistä DisplayForge automaattisesti Windowsiin kirjautuessa",
            "Suositellaan ilmoitusalueella käytettäväksi. Voit myöhemmin ottaa asetuksissa käyttöön \"Käynnistä pienennettynä ilmoitusalueelle\"."),

        ["hu-HU"] = new(
            1038,
            "A [ProductName] újabb verziója már telepítve van.",
            "Többmonitoros profilváltó globális billentyűparancsokkal",
            "Többmonitoros profilváltó",
            "[ProductName] telepítése",
            "Telepítési beállítások",
            "Válassza ki a DisplayForge választható funkcióit.",
            "DisplayForge automatikus indítása Windows-bejelentkezéskor",
            "Ajánlott tálcán futó használathoz. Később a Beállításokban bekapcsolhatja a „Indítás minimalizálva a tálcára” lehetőséget is."),

        ["ro-RO"] = new(
            1048,
            "Este deja instalată o versiune mai nouă de [ProductName].",
            "Comutator de profiluri multi-monitor cu comenzi rapide globale",
            "Comutator de profiluri multi-monitor",
            "Instalare [ProductName]",
            "Opțiuni de instalare",
            "Alegeți funcțiile opționale pentru DisplayForge.",
            "Pornește DisplayForge automat la conectarea în Windows",
            "Recomandat pentru utilizare în zona de notificare. Puteți activa ulterior „Pornește minimizat în zona de notificare” din Setări."),

        ["el-GR"] = new(
            1032,
            "Είναι ήδη εγκατεστημένη νεότερη έκδοση του [ProductName].",
            "Εναλλαγή προφίλ πολλαπλών οθονών με καθολικές συντομεύσεις",
            "Εναλλαγή προφίλ πολλαπλών οθονών",
            "Εγκατάσταση [ProductName]",
            "Επιλογές εγκατάστασης",
            "Επιλέξτε προαιρετικές δυνατότητες για το DisplayForge.",
            "Αυτόματη εκκίνηση του DisplayForge κατά τη σύνδεση στα Windows",
            "Συνιστάται για χρήση στην περιοχή ειδοποιήσεων. Μπορείτε αργότερα να ενεργοποιήσετε «Εκκίνηση ελαχιστοποιημένο στην περιοχή ειδοποιήσεων» στις Ρυθμίσεις."),

        ["vi-VN"] = new(
            1066,
            "Đã cài đặt phiên bản mới hơn của [ProductName].",
            "Trình chuyển hồ sơ đa màn hình với phím tắt toàn cục",
            "Trình chuyển hồ sơ đa màn hình",
            "Cài đặt [ProductName]",
            "Tùy chọn cài đặt",
            "Chọn các tính năng tùy chọn cho DisplayForge.",
            "Tự động khởi động DisplayForge khi đăng nhập Windows",
            "Khuyến nghị khi dùng trên khay hệ thống. Bạn cũng có thể bật \"Khởi động thu nhỏ xuống khay\" sau trong Cài đặt."),

        ["th-TH"] = new(
            1054,
            "มีเวอร์ชันที่ใหม่กว่าของ [ProductName] ติดตั้งอยู่แล้ว",
            "สลับโปรไฟล์หลายจอพร้อมปุ่มลัดสากล",
            "สลับโปรไฟล์หลายจอ",
            "ติดตั้ง [ProductName]",
            "ตัวเลือกการติดตั้ง",
            "เลือกคุณลักษณะเสริมของ DisplayForge",
            "เริ่ม DisplayForge โดยอัตโนมัติเมื่อลงชื่อเข้าใช้ Windows",
            "แนะนำสำหรับการใช้งานในถาดระบบ คุณสามารถเปิด \"เริ่มแบบย่อในถาดระบบ\" ได้ในภายหลังในการตั้งค่า"),

        ["id-ID"] = new(
            1057,
            "Versi [ProductName] yang lebih baru sudah terpasang.",
            "Pengalih profil multi-monitor dengan pintasan global",
            "Pengalih profil multi-monitor",
            "Penyiapan [ProductName]",
            "Opsi instalasi",
            "Pilih fitur opsional DisplayForge.",
            "Mulai DisplayForge secara otomatis saat masuk ke Windows",
            "Disarankan untuk penggunaan di baki sistem. Anda juga dapat mengaktifkan \"Mulai diminimalkan ke baki\" nanti di Pengaturan."),

        ["ms-MY"] = new(
            1086,
            "Versi [ProductName] yang lebih baharu sudah dipasang.",
            "Penukar profil berbilang monitor dengan pintasan global",
            "Penukar profil berbilang monitor",
            "Persediaan [ProductName]",
            "Pilihan pemasangan",
            "Pilih ciri pilihan DisplayForge.",
            "Mulakan DisplayForge secara automatik apabila log masuk ke Windows",
            "Disyorkan untuk penggunaan dalam dulang sistem. Anda juga boleh dayakan \"Mula diminimumkan ke dulang\" kemudian dalam Tetapan."),

        ["hi-IN"] = new(
            1081,
            "[ProductName] का नया संस्करण पहले से स्थापित है।",
            "वैश्विक हॉटकी के साथ मल्टी-मॉनिटर प्रोफ़ाइल स्विचर",
            "मल्टी-मॉनिटर प्रोफ़ाइल स्विचर",
            "[ProductName] सेटअप",
            "इंस्टॉलेशन विकल्प",
            "DisplayForge के वैकल्पिक फीचर चुनें।",
            "Windows में साइन इन करने पर DisplayForge स्वचालित रूप से शुरू करें",
            "ट्रे में रहने वाले उपयोग के लिए अनुशंसित। बाद में सेटिंग्स में \"ट्रे में न्यूनतम करके शुरू करें\" भी सक्षम कर सकते हैं।"),

        ["ar-SA"] = new(
            1025,
            "هناك إصدار أحدث من [ProductName] مثبت بالفعل.",
            "مبدّل ملفات شخصية لشاشات متعددة مع اختصارات عامة",
            "مبدّل ملفات شخصية لشاشات متعددة",
            "إعداد [ProductName]",
            "خيارات التثبيت",
            "اختر الميزات الاختيارية لـ DisplayForge.",
            "بدء DisplayForge تلقائيًا عند تسجيل الدخول إلى Windows",
            "يُنصح به للاستخدام في علبة النظام. يمكنك أيضًا تمكين «البدء مصغّرًا في علبة النظام» لاحقًا من الإعدادات."),

        ["he-IL"] = new(
            1037,
            "גרסה חדשה יותר של [ProductName] כבר מותקנת.",
            "מחליף פרופילי ריבוי צגים עם קיצורי דרך גלובליים",
            "מחליף פרופילי ריבוי צגים",
            "התקנת [ProductName]",
            "אפשרויות התקנה",
            "בחר תכונות אופציונליות עבור DisplayForge.",
            "הפעל את DisplayForge אוטומטית בכניסה ל-Windows",
            "מומלץ לשימוש במגש המערכת. ניתן גם להפעיל \"הפעלה ממוזערת למגש\" מאוחר יותר בהגדרות."),
    };
}
