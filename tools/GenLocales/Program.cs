using System.Text;

// Generates Strings.<culture>.resx under src/DisplayForge/Resources
// Run: dotnet run --project tools/GenLocales
// Sync from resx: pwsh -File tools/GenLocales/sync-from-resx.ps1

var repoRoot = FindRepoRoot();
var outDir = Path.Combine(repoRoot, "src", "DisplayForge", "Resources");
Directory.CreateDirectory(outDir);

string[] keys =
[
    "AppName","AppTagline","Profiles","SelectedProfileSection","CurrentMonitors","ProfileMonitors",
    "ProfileMonitorsHint","NewProfile","NewProfileHint","SaveToProfile","SaveToProfileHint","Apply",
    "ApplyHint","Duplicate","Delete","Rename","RenamePrompt","Hotkey",
    "ClearHotkey","Settings","ShowMainWindow","Exit","Language","LanguageAuto",
    "GeneralSection","StartMinimized","ShowNotifications","HotkeysEnabled","Save","Cancel",
    "StatusReady","StatusApplied","StatusFailed","StatusSaved","StatusDeleted","ConfirmDelete",
    "ConfirmDeleteTitle","HotkeyConflict","HotkeyRegisterFailed","HotkeyHint","Name","Primary",
    "Enabled","Resolution","Width","Height","Refresh","RefreshMonitors",
    "Position","PosX","PosY","Orientation","OrientationLandscape","OrientationPortrait",
    "OrientationLandscapeFlipped","OrientationPortraitFlipped","NoProfileSelected","EmptyStateNoProfiles","AppliedBadge","TrayTooltip",
    "AlreadyRunning","NewProfileName","MissingMonitors","CloseToTray","ConfirmKeepSettingsTitle","ConfirmKeepSettingsMessage",
    "ConfirmKeepSettingsCountdown","KeepChanges","RevertChanges","StatusReverted","StatusRevertFailed","ConfirmApplySection",
    "ConfirmApplyHint","ConfirmApplyFromUi","ConfirmApplyFromHotkey","ConfirmApplyTimeoutSeconds","LayoutEditor","LayoutEditorHint",
    "LayoutEditorEmpty","IdentifyMonitors","IdentifyMonitorsHint","StatusIdentifyShown"
];

var locales = BuildLocales();
foreach (var (culture, map) in locales)
{
    foreach (var k in keys)
    {
        if (!map.ContainsKey(k))
            throw new InvalidOperationException($"Missing key '{k}' for {culture}");
    }

    var sb = new StringBuilder();
    sb.AppendLine("""<?xml version="1.0" encoding="utf-8"?>""");
    sb.AppendLine("<root>");
    sb.AppendLine("""  <resheader name="resmimetype"><value>text/microsoft-resx</value></resheader>""");
    sb.AppendLine("""  <resheader name="version"><value>2.0</value></resheader>""");
    sb.AppendLine("""  <resheader name="reader"><value>System.Resources.ResXResourceReader, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089</value></resheader>""");
    sb.AppendLine("""  <resheader name="writer"><value>System.Resources.ResXResourceWriter, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089</value></resheader>""");
    sb.AppendLine();
    foreach (var k in keys)
    {
        var v = XmlEscape(map[k]);
        sb.AppendLine($"""  <data name="{k}" xml:space="preserve"><value>{v}</value></data>""");
    }
    sb.AppendLine("</root>");

    var path = Path.Combine(outDir, $"Strings.{culture}.resx");
    File.WriteAllText(path, sb.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    Console.WriteLine($"Wrote {path}");
}

Console.WriteLine($"Done. Generated {locales.Count} locale files.");
return;

static string FindRepoRoot()
{
    var dir = new DirectoryInfo(AppContext.BaseDirectory);
    while (dir is not null)
    {
        if (File.Exists(Path.Combine(dir.FullName, "DisplayForge.sln")))
            return dir.FullName;
        dir = dir.Parent;
    }
    // tools/GenLocales -> repo root
    return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
}

static Dictionary<string, Dictionary<string, string>> BuildLocales()
{
    var d = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

    d["ja"] = L(
        "DisplayForge", "モニターの配置・解像度を保存して、ワンタッチで切り替えできます。", "保存したプロファイル", "選択中のプロファイル",
        "現在接続されているモニター", "このプロファイルの詳細設定", "セルをクリックすると数値を直接編集できます。", "＋ 現在の構成を保存",
        "現在のモニター構成を、新しいプロファイルとして保存します。", "現在の構成で上書き保存", "選択中のプロファイルの内容を、現在のモニター構成で置き換えます。", "このプロファイルに切り替え",
        "保存した構成どおりに、モニターを切り替えます。", "複製", "削除", "名前を変更",
        "新しい名前を入力してください", "切り替えショートカット", "解除", "設定",
        "DisplayForge を開く", "終了", "言語", "システムの言語に合わせる（自動）",
        "基本", "起動時はウィンドウを開かず、トレイ（通知領域）に常駐する", "プロファイル切り替え時に通知を表示する", "ショートカットキーでの切り替えを有効にする",
        "保存", "キャンセル", "準備完了", "切り替えました: {0}",
        "失敗しました: {0}", "保存しました: {0}", "プロファイルを削除しました", "プロファイル「{0}」を削除しますか？\nこの操作は元に戻せません。",
        "削除の確認", "ショートカット {0} は、別のプロファイルで既に使われています。", "ショートカット {0} を登録できませんでした: {1}", "枠をクリックしてから、割り当てたいキーを押してください（例: Ctrl+Alt+1）。Esc キーで解除できます。",
        "名前", "メイン", "使用", "解像度",
        "幅", "高さ", "Hz", "最新の状態に更新",
        "位置", "位置X", "位置Y", "向き",
        "横向き", "縦向き", "横向き（反転）", "縦向き（反転）",
        "プロファイルが選択されていません。", "まだプロファイルがありません。\n\n左上の「＋ 現在の構成を保存」を押すと、\n現在のモニター構成が保存され、いつでも呼び出せるようになります。", "✓ 適用中", "DisplayForge — モニター構成をワンタッチで切り替え",
        "DisplayForge は既に起動しています。", "プロファイル {0}", "（未接続のためスキップ: {0}）", "ウィンドウを閉じても、アプリはトレイ（通知領域）で動き続けます。終了するにはトレイアイコンを右クリックして「終了」を選んでください。",
        "ディスプレイ設定の確認", "このディスプレイ設定のままでよいですか？", "操作がない場合、{0} 秒後に自動で元の設定に戻ります", "この設定のままにする",
        "元に戻す", "ディスプレイ設定を元に戻しました。", "元に戻せませんでした: {0}", "切り替え後の確認",
        "切り替え後に確認画面を表示し、時間内に操作がなければ自動で元の構成に戻します。画面が映らなくなったときの保険になります。", "ボタンやトレイから切り替えたとき、確認画面を表示する", "ショートカットキーで切り替えたとき、確認画面を表示する", "自動で元に戻すまでの時間（秒）",
        "モニターの配置", "四角をドラッグすると、モニターの並びを変更できます。", "左の一覧からプロファイルを選ぶと、ここに配置が表示されます。", "モニターに番号を表示",
        "どのモニターが何番かを、実際の画面に大きく表示します。", "モニターに番号を表示しています…");

    d["ar"] = L(
        "DisplayForge", "احفظ تخطيطات الشاشات وبدّل بينها بلمسة واحدة.", "الملفات الشخصية", "الملف الشخصي المحدد",
        "الشاشات الحالية", "شاشات الملف الشخصي", "انقر على خلية لتحرير القيم مباشرة.", "جديد من الإعداد الحالي",
        "حفظ إعداد الشاشات الحالي كملف شخصي جديد.", "استبدال بالإعداد الحالي", "استبدال هذا الملف بإعداد الشاشات الحالي.", "تطبيق",
        "تبديل الشاشات إلى هذا التخطيط المحفوظ.", "تكرار", "حذف", "إعادة تسمية",
        "أدخل اسمًا جديدًا", "اختصار", "مسح", "الإعدادات",
        "فتح DisplayForge", "خروج", "اللغة", "افتراضي النظام",
        "عام", "البدء مصغّرًا في علبة النظام", "إظهار إشعار عند التبديل", "تمكين الاختصارات العامة",
        "حفظ", "إلغاء", "جاهز", "تم تطبيق الملف: {0}",
        "فشل: {0}", "تم حفظ الملف: {0}", "تم حذف الملف", "حذف الملف الشخصي \"{0}\"؟",
        "تأكيد الحذف", "الاختصار {0} مستخدم بالفعل من ملف آخر.", "تعذّر تسجيل الاختصار {0}: {1}", "انقر ثم اضغط اختصارًا (Ctrl/Alt/Shift/Win + مفتاح). Esc للمسح.",
        "الاسم", "أساسي", "تشغيل", "الدقة",
        "العرض", "الارتفاع", "هرتز", "تحديث",
        "الموضع", "X", "Y", "الاتجاه",
        "أفقي", "عمودي", "أفقي (مقلوب)", "عمودي (مقلوب)",
        "حدد ملفًا شخصيًا أو أنشئه.", "لا توجد ملفات شخصية بعد.\n\nاستخدم الزر أعلى اليسار لحفظ إعداد الشاشات الحالي كملف شخصي.", "✓ قيد الاستخدام", "DisplayForge — ملفات شاشات متعددة",
        "DisplayForge يعمل بالفعل.", "ملف {0}", "مفقود: {0}", "الإغلاق يخفي إلى علبة النظام. استخدم خروج من قائمة العلبة.",
        "إعدادات العرض", "الاحتفاظ بإعدادات العرض هذه؟", "التراجع خلال {0} ث…", "الاحتفاظ",
        "تراجع", "تم التراجع عن إعدادات العرض.", "تعذّر التراجع: {0}", "تأكيد تغييرات العرض",
        "يعرض تأكيدًا بعد التبديل؛ إن لم تستجب في الوقت المحدد يُستعاد التخطيط السابق تلقائيًا (حماية إذا بقي الشاشة سوداء).", "تأكيد بعد التطبيق (زر / علبة النظام)", "تأكيد بعد التطبيق (اختصار)", "المهلة (ثوانٍ)",
        "الترتيب", "اسحب المستطيلات لنقل الشاشات. تلتصق الحواف بالجارات.", "حدد ملفًا شخصيًا لترتيب الشاشات.", "تعرّف",
        "عرض أرقام كبيرة على كل شاشة فعلية.", "جارٍ عرض أرقام الشاشات…");

    d["cs"] = L(
        "DisplayForge", "Ukládejte rozložení monitorů a přepínejte jedním stiskem.", "Profily", "Vybraný profil",
        "Aktuální monitory", "Monitory profilu", "Kliknutím na buňku můžete hodnoty přímo upravit.", "Nový z aktuální konfigurace",
        "Uložit aktuální konfiguraci monitorů jako nový profil.", "Přepsat aktuální", "Nahradit tento profil aktuální konfigurací monitorů.", "Použít",
        "Přepnout monitory na toto uložené rozložení.", "Duplikovat", "Smazat", "Přejmenovat",
        "Zadejte nový název", "Klávesová zkratka", "Vymazat", "Nastavení",
        "Otevřít DisplayForge", "Ukončit", "Jazyk", "Systémové výchozí",
        "Obecné", "Spustit minimalizované v oznamovací oblasti", "Oznámení při přepnutí", "Povolit globální zkratky",
        "Uložit", "Zrušit", "Připraveno", "Profil použit: {0}",
        "Selhalo: {0}", "Profil uložen: {0}", "Profil smazán", "Smazat profil „{0}“?",
        "Potvrdit smazání", "Zkratku {0} už používá jiný profil.", "Nelze zaregistrovat zkratku {0}: {1}", "Klikněte a stiskněte zkratku (Ctrl/Alt/Shift/Win + klávesa). Esc vymaže.",
        "Název", "Hlavní", "Zap.", "Rozlišení",
        "Šířka", "Výška", "Hz", "Obnovit",
        "Pozice", "X", "Y", "Orientace",
        "Na šířku", "Na výšku", "Na šířku (převrácené)", "Na výšku (převrácené)",
        "Vyberte nebo vytvořte profil.", "Zatím žádné profily.\n\nPomocí tlačítka vlevo nahoře uložte aktuální konfiguraci monitorů jako profil.", "✓ Aktivní", "DisplayForge — profily více monitorů",
        "DisplayForge již běží.", "Profil {0}", "Chybí: {0}", "Zavření skryje do oznamovací oblasti. Ukončete z nabídky v oznamovací oblasti.",
        "Nastavení zobrazení", "Ponechat tato nastavení zobrazení?", "Vrácení za {0} s…", "Ponechat",
        "Vrátit", "Nastavení zobrazení vráceno.", "Nepodařilo se vrátit: {0}", "Potvrdit změny zobrazení",
        "Po přepnutí zobrazí potvrzení; bez odezvy v limitu se předchozí rozložení automaticky obnoví (pojistka při černé obrazovce).", "Potvrdit po použití (tlačítko / oznamovací oblast)", "Potvrdit po použití (zkratka)", "Časový limit (sekundy)",
        "Rozložení", "Přetažením obdélníků přesunete monitory. Hrany se přichytí k sousedům.", "Vyberte profil pro uspořádání monitorů.", "Identifikovat",
        "Zobrazit velká čísla na každém fyzickém monitoru.", "Zobrazení čísel monitorů…");

    d["da"] = L(
        "DisplayForge", "Gem skærmlayouts og skift med ét tryk.", "Profiler", "Valgt profil",
        "Aktuelle skærme", "Profilskærme", "Klik på en celle for at redigere værdier direkte.", "Ny fra aktuel konfiguration",
        "Gem den aktuelle skærmkonfiguration som en ny profil.", "Overskriv med aktuel", "Erstat denne profil med den aktuelle skærmkonfiguration.", "Anvend",
        "Skift skærme til dette gemte layout.", "Dupliker", "Slet", "Omdøb",
        "Indtast et nyt navn", "Genvej", "Ryd", "Indstillinger",
        "Åbn DisplayForge", "Afslut", "Sprog", "Systemstandard",
        "Generelt", "Start minimeret i systembakken", "Vis meddelelse ved skift", "Aktivér globale genveje",
        "Gem", "Annuller", "Klar", "Profil anvendt: {0}",
        "Mislykkedes: {0}", "Profil gemt: {0}", "Profil slettet", "Slet profilen \"{0}\"?",
        "Bekræft sletning", "Genvejen {0} bruges allerede af en anden profil.", "Kunne ikke registrere {0}: {1}", "Klik, og tryk på en genvej (Ctrl/Alt/Shift/Win + tast). Esc rydder.",
        "Navn", "Primær", "Til", "Opløsning",
        "Bredde", "Højde", "Hz", "Opdater",
        "Position", "X", "Y", "Retning",
        "Liggende", "Stående", "Liggende (vendt)", "Stående (vendt)",
        "Vælg eller opret en profil.", "Ingen profiler endnu.\n\nBrug knappen øverst til venstre for at gemme den aktuelle skærmkonfiguration som profil.", "✓ Aktiv", "DisplayForge — multi-skærmsprofiler",
        "DisplayForge kører allerede.", "Profil {0}", "Mangler: {0}", "Luk skjuler til systembakken. Afslut via bakkemenuen.",
        "Skærmindstillinger", "Behold disse skærmindstillinger?", "Fortryder om {0} s…", "Behold",
        "Fortryd", "Skærmindstillinger fortrudt.", "Kunne ikke fortryde: {0}", "Bekræft skærmændringer",
        "Viser en bekræftelse efter skift; uden svar i tide gendannes det forrige layout automatisk (sikkerhed ved sort skærm).", "Bekræft efter anvendelse (knap / systembakke)", "Bekræft efter anvendelse (genvej)", "Timeout (sekunder)",
        "Arrangement", "Træk rektangler for at flytte skærme. Kanter fastgøres til naboer.", "Vælg en profil for at arrangere skærme.", "Identificer",
        "Vis store numre på hver fysisk skærm.", "Viser skærmnumre…");

    d["de"] = L(
        "DisplayForge", "Monitoranordnungen speichern und mit einem Tastendruck umschalten.", "Profile", "Ausgewähltes Profil",
        "Aktuelle Monitore", "Profilmonitore", "Zelle anklicken, um Werte direkt zu bearbeiten.", "Neu aus aktueller Konfiguration",
        "Aktuelle Monitorkonfiguration als neues Profil speichern.", "Mit aktueller überschreiben", "Ausgewähltes Profil durch die aktuelle Monitorkonfiguration ersetzen.", "Anwenden",
        "Monitore auf dieses gespeicherte Layout umschalten.", "Duplizieren", "Löschen", "Umbenennen",
        "Neuen Namen eingeben", "Tastenkürzel", "Löschen", "Einstellungen",
        "DisplayForge öffnen", "Beenden", "Sprache", "Systemstandard",
        "Allgemein", "Minimiert im Infobereich starten", "Benachrichtigung beim Wechsel", "Globale Tastenkürzel aktivieren",
        "Speichern", "Abbrechen", "Bereit", "Profil angewendet: {0}",
        "Fehler: {0}", "Profil gespeichert: {0}", "Profil gelöscht", "Profil „{0}“ löschen?",
        "Löschen bestätigen", "Tastenkürzel {0} wird bereits von einem anderen Profil verwendet.", "Tastenkürzel {0} konnte nicht registriert werden: {1}", "Klicken und Tastenkürzel drücken (Strg/Alt/Umschalt/Win + Taste). Esc leert.",
        "Name", "Primär", "An", "Auflösung",
        "Breite", "Höhe", "Hz", "Aktualisieren",
        "Position", "X", "Y", "Ausrichtung",
        "Querformat", "Hochformat", "Querformat (gespiegelt)", "Hochformat (gespiegelt)",
        "Profil auswählen oder erstellen.", "Noch keine Profile.\n\nNutzen Sie die Schaltfläche oben links, um die aktuelle Monitorkonfiguration als Profil zu speichern.", "✓ Aktiv", "DisplayForge — Multimonitor-Profile",
        "DisplayForge läuft bereits.", "Profil {0}", "Fehlend: {0}", "Schließen minimiert in den Infobereich. Beenden über das Infobereich-Menü.",
        "Anzeigeeinstellungen", "Diese Anzeigeeinstellungen beibehalten?", "Rückgängig in {0} Sekunden…", "Beibehalten",
        "Rückgängig", "Anzeigeeinstellungen zurückgesetzt.", "Zurücksetzen fehlgeschlagen: {0}", "Anzeigeänderungen bestätigen",
        "Zeigt nach dem Umschalten eine Bestätigung; ohne Reaktion innerhalb der Frist wird das vorherige Layout automatisch wiederhergestellt (Schutz bei schwarzem Bildschirm).", "Nach Anwenden bestätigen (Schaltfläche / Infobereich)", "Nach Anwenden bestätigen (Tastenkürzel)", "Zeitlimit (Sekunden)",
        "Anordnung", "Rechtecke ziehen, um Monitore zu verschieben. Kanten rasten an Nachbarn ein.", "Profil auswählen, um Monitore anzuordnen.", "Identifizieren",
        "Große Nummern auf jedem physischen Monitor anzeigen.", "Monitornummern werden angezeigt…");

    d["el"] = L(
        "DisplayForge", "Αποθηκεύστε διατάξεις οθονών και αλλάξτε με ένα πάτημα.", "Προφίλ", "Επιλεγμένο προφίλ",
        "Τρέχουσες οθόνες", "Οθόνες προφίλ", "Κάντε κλικ σε ένα κελί για άμεση επεξεργασία τιμών.", "Νέο από την τρέχουσα διαμόρφωση",
        "Αποθήκευση της τρέχουσας διαμόρφωσης ως νέο προφίλ.", "Αντικατάσταση με την τρέχουσα", "Αντικατάσταση αυτού του προφίλ με την τρέχουσα διαμόρφωση.", "Εφαρμογή",
        "Εναλλαγή οθονών σε αυτή την αποθηκευμένη διάταξη.", "Αντιγραφή", "Διαγραφή", "Μετονομασία",
        "Εισαγάγετε νέο όνομα", "Συντόμευση", "Διαγραφή", "Ρυθμίσεις",
        "Άνοιγμα DisplayForge", "Έξοδος", "Γλώσσα", "Προεπιλογή συστήματος",
        "Γενικά", "Έναρξη ελαχιστοποιημένο στη γραμμή ειδοποιήσεων", "Ειδοποίηση κατά την εναλλαγή", "Ενεργοποίηση καθολικών συντομεύσεων",
        "Αποθήκευση", "Ακύρωση", "Έτοιμο", "Εφαρμόστηκε το προφίλ: {0}",
        "Αποτυχία: {0}", "Αποθηκεύτηκε το προφίλ: {0}", "Το προφίλ διαγράφηκε", "Διαγραφή του προφίλ «{0}»;",
        "Επιβεβαίωση διαγραφής", "Η συντόμευση {0} χρησιμοποιείται ήδη από άλλο προφίλ.", "Αδυναμία καταχώρισης {0}: {1}", "Κάντε κλικ και πατήστε συντόμευση (Ctrl/Alt/Shift/Win + πλήκτρο). Esc διαγράφει.",
        "Όνομα", "Κύρια", "Ναι", "Ανάλυση",
        "Πλάτος", "Ύψος", "Hz", "Ανανέωση",
        "Θέση", "X", "Y", "Προσανατολισμός",
        "Οριζόντιος", "Κατακόρυφος", "Οριζόντιος (ανεστραμμένος)", "Κατακόρυφος (ανεστραμμένος)",
        "Επιλέξτε ή δημιουργήστε προφίλ.", "Δεν υπάρχουν ακόμη προφίλ.\n\nΧρησιμοποιήστε το κουμπί πάνω αριστερά για να αποθηκεύσετε την τρέχουσα διαμόρφωση ως προφίλ.", "✓ Σε χρήση", "DisplayForge — προφίλ πολλαπλών οθονών",
        "Το DisplayForge εκτελείται ήδη.", "Προφίλ {0}", "Λείπουν: {0}", "Το κλείσιμο αποκρύπτει στη γραμμή ειδοποιήσεων. Έξοδος από το μενού εκεί.",
        "Ρυθμίσεις οθόνης", "Διατήρηση αυτών των ρυθμίσεων οθόνης;", "Επαναφορά σε {0} δ…", "Διατήρηση",
        "Επαναφορά", "Οι ρυθμίσεις οθόνης επαναφέρθηκαν.", "Αποτυχία επαναφοράς: {0}", "Επιβεβαίωση αλλαγών οθόνης",
        "Εμφανίζει επιβεβαίωση μετά την αλλαγή· αν δεν απαντήσετε εγκαίρως, η προηγούμενη διάταξη επανέρχεται αυτόματα (ασφάλεια αν η οθόνη μείνει μαύρη).", "Επιβεβαίωση μετά την εφαρμογή (κουμπί / γραμμή ειδοποιήσεων)", "Επιβεβαίωση μετά την εφαρμογή (συντόμευση)", "Χρονικό όριο (δευτερόλεπτα)",
        "Διάταξη", "Σύρετε τα ορθογώνια για μετακίνηση οθονών. Οι άκρες κουμπώνουν στους γείτονες.", "Επιλέξτε προφίλ για διάταξη οθονών.", "Αναγνώριση",
        "Εμφάνιση μεγάλων αριθμών σε κάθε φυσική οθόνη.", "Εμφάνιση αριθμών οθόνης…");

    d["es"] = L(
        "DisplayForge", "Guarda la disposición de monitores y cambia con un toque.", "Perfiles", "Perfil seleccionado",
        "Monitores actuales", "Monitores del perfil", "Haz clic en una celda para editar los valores.", "Nuevo desde la config. actual",
        "Guardar la configuración actual como un perfil nuevo.", "Sobrescribir con la actual", "Sustituir este perfil por la configuración actual.", "Aplicar",
        "Cambiar los monitores a esta disposición guardada.", "Duplicar", "Eliminar", "Renombrar",
        "Introduce un nombre nuevo", "Atajo", "Borrar", "Configuración",
        "Abrir DisplayForge", "Salir", "Idioma", "Predeterminado del sistema",
        "General", "Iniciar minimizado en la bandeja", "Mostrar notificación al cambiar", "Habilitar atajos globales",
        "Guardar", "Cancelar", "Listo", "Perfil aplicado: {0}",
        "Error: {0}", "Perfil guardado: {0}", "Perfil eliminado", "¿Eliminar el perfil «{0}»?",
        "Confirmar eliminación", "El atajo {0} ya lo usa otro perfil.", "No se pudo registrar el atajo {0}: {1}", "Haz clic y pulsa un atajo (Ctrl/Alt/Mayús/Win + tecla). Esc borra.",
        "Nombre", "Principal", "Sí", "Resolución",
        "Ancho", "Alto", "Hz", "Actualizar",
        "Posición", "X", "Y", "Orientación",
        "Horizontal", "Vertical", "Horizontal (invertido)", "Vertical (invertido)",
        "Selecciona o crea un perfil.", "Aún no hay perfiles.\n\nUsa el botón superior izquierdo para guardar la configuración actual como perfil.", "✓ En uso", "DisplayForge — perfiles multipantalla",
        "DisplayForge ya se está ejecutando.", "Perfil {0}", "Faltan: {0}", "Al cerrar se oculta en la bandeja. Usa Salir del menú de la bandeja.",
        "Configuración de pantalla", "¿Conservar esta configuración de pantalla?", "Revirtiendo en {0} s…", "Conservar",
        "Revertir", "Configuración de pantalla revertida.", "No se pudo revertir: {0}", "Confirmar cambios de pantalla",
        "Muestra una confirmación tras el cambio; si no respondes a tiempo, se restaura automáticamente la disposición anterior (por si la pantalla queda en negro).", "Confirmar tras aplicar (botón / bandeja)", "Confirmar tras aplicar (atajo)", "Tiempo de espera (segundos)",
        "Disposición", "Arrastra los rectángulos para mover monitores. Los bordes se ajustan a los vecinos.", "Selecciona un perfil para disponer monitores.", "Identificar",
        "Mostrar números grandes en cada monitor físico.", "Mostrando números de monitor…");

    d["fi"] = L(
        "DisplayForge", "Tallenna näyttöasettelut ja vaihda yhdellä painalluksella.", "Profiilit", "Valittu profiili",
        "Nykyiset näytöt", "Profiilin näytöt", "Napsauta solua muokataksesi arvoja suoraan.", "Uusi nykyisestä kokoonpanosta",
        "Tallenna nykyinen näyttöasetus uudeksi profiiliksi.", "Korvaa nykyisellä", "Korvaa tämä profiili nykyisellä näyttöasetuksella.", "Käytä",
        "Vaihda näytöt tähän tallennettuun asetteluun.", "Monista", "Poista", "Nimeä uudelleen",
        "Anna uusi nimi", "Pikanäppäin", "Tyhjennä", "Asetukset",
        "Avaa DisplayForge", "Lopeta", "Kieli", "Järjestelmän oletus",
        "Yleiset", "Käynnistä pienennettynä ilmoitusalueelle", "Näytä ilmoitus vaihdettaessa", "Ota yleiset pikanäppäimet käyttöön",
        "Tallenna", "Peruuta", "Valmis", "Profiili käytössä: {0}",
        "Epäonnistui: {0}", "Profiili tallennettu: {0}", "Profiili poistettu", "Poistetaanko profiili \"{0}\"?",
        "Vahvista poisto", "Pikanäppäin {0} on jo toisen profiilin käytössä.", "Pikanäppäintä {0} ei voitu rekisteröidä: {1}", "Napsauta ja paina pikanäppäintä (Ctrl/Alt/Shift/Win + näppäin). Esc tyhjentää.",
        "Nimi", "Ensisijainen", "Päällä", "Tarkkuus",
        "Leveys", "Korkeus", "Hz", "Päivitä",
        "Sijainti", "X", "Y", "Suunta",
        "Vaaka", "Pysty", "Vaaka (käännetty)", "Pysty (käännetty)",
        "Valitse tai luo profiili.", "Ei vielä profiileja.\n\nTallenna nykyinen näyttöasetus profiiliksi vasemman yläkulman painikkeella.", "✓ Käytössä", "DisplayForge — moninäyttöprofiilit",
        "DisplayForge on jo käynnissä.", "Profiili {0}", "Puuttuu: {0}", "Sulkeminen piilottaa ilmoitusalueelle. Lopeta ilmoitusalueen valikosta.",
        "Näyttöasetukset", "Säilytetäänkö nämä näyttöasetukset?", "Palautetaan {0} s kuluttua…", "Säilytä",
        "Palauta", "Näyttöasetukset palautettu.", "Palautus epäonnistui: {0}", "Vahvista näyttömuutokset",
        "Näyttää vahvistuksen vaihdon jälkeen; jos et vastaa ajoissa, edellinen asettelu palautetaan automaattisesti (turva mustan näytön varalta).", "Vahvista käytön jälkeen (painike / ilmoitusalue)", "Vahvista käytön jälkeen (pikanäppäin)", "Aikakatkaisu (sekuntia)",
        "Sijoittelu", "Siirrä näyttöjä vetämällä suorakulmioita. Reunat tarttuvat naapureihin.", "Valitse profiili näyttöjen sijoitteluun.", "Tunnista",
        "Näytä suuret numerot jokaisella fyysisellä näytöllä.", "Näytetään näyttönumeroita…");

    d["fr"] = L(
        "DisplayForge", "Enregistrez les dispositions d'écrans et basculez d'un simple appui.", "Profils", "Profil sélectionné",
        "Moniteurs actuels", "Moniteurs du profil", "Cliquez sur une cellule pour modifier les valeurs.", "Nouveau depuis la config. actuelle",
        "Enregistrer la configuration actuelle comme nouveau profil.", "Écraser avec l’actuelle", "Remplacer ce profil par la configuration actuelle.", "Appliquer",
        "Basculer les moniteurs vers cette disposition enregistrée.", "Dupliquer", "Supprimer", "Renommer",
        "Entrez un nouveau nom", "Raccourci", "Effacer", "Paramètres",
        "Ouvrir DisplayForge", "Quitter", "Langue", "Paramètre système",
        "Général", "Démarrer réduit dans la zone de notification", "Notification lors du changement", "Activer les raccourcis globaux",
        "Enregistrer", "Annuler", "Prêt", "Profil appliqué : {0}",
        "Échec : {0}", "Profil enregistré : {0}", "Profil supprimé", "Supprimer le profil « {0} » ?",
        "Confirmer la suppression", "Le raccourci {0} est déjà utilisé par un autre profil.", "Impossible d’enregistrer le raccourci {0} : {1}", "Cliquez puis appuyez sur un raccourci (Ctrl/Alt/Maj/Win + touche). Échap efface.",
        "Nom", "Principal", "Oui", "Résolution",
        "Largeur", "Hauteur", "Hz", "Actualiser",
        "Position", "X", "Y", "Orientation",
        "Paysage", "Portrait", "Paysage (inversé)", "Portrait (inversé)",
        "Sélectionnez ou créez un profil.", "Aucun profil pour l'instant.\n\nUtilisez le bouton en haut à gauche pour enregistrer la configuration actuelle comme profil.", "✓ En cours", "DisplayForge — profils multi-moniteurs",
        "DisplayForge est déjà en cours d’exécution.", "Profil {0}", "Manquants : {0}", "Fermer masque dans la zone de notification. Quittez via le menu de la zone de notification.",
        "Paramètres d’affichage", "Conserver ces paramètres d’affichage ?", "Rétablissement dans {0} s…", "Conserver",
        "Rétablir", "Paramètres d’affichage rétablis.", "Échec du rétablissement : {0}", "Confirmer les changements d’affichage",
        "Affiche une confirmation après bascule ; sans réponse dans le délai, la disposition précédente est rétablie automatiquement (sécurité si l'écran reste noir).", "Confirmer après application (bouton / zone de notification)", "Confirmer après application (raccourci)", "Délai (secondes)",
        "Disposition", "Faites glisser les rectangles pour déplacer les moniteurs. Les bords s’alignent sur les voisins.", "Sélectionnez un profil pour disposer les moniteurs.", "Identifier",
        "Afficher de grands numéros sur chaque moniteur physique.", "Affichage des numéros de moniteur…");

    d["he"] = L(
        "DisplayForge", "שמרו פריסות צגים והחליפו בנגיעה אחת.", "פרופילים", "הפרופיל שנבחר",
        "צגים נוכחיים", "צגי הפרופיל", "לחצו על תא כדי לערוך ערכים ישירות.", "חדש מהתצורה הנוכחית",
        "שמירת תצורת הצגים הנוכחית כפרופיל חדש.", "החלף בנוכחית", "החלפת פרופיל זה בתצורת הצגים הנוכחית.", "החל",
        "החלפת הצגים לפריסה השמורה הזו.", "שכפל", "מחק", "שנה שם",
        "הזינו שם חדש", "קיצור דרך", "נקה", "הגדרות",
        "פתח את DisplayForge", "יציאה", "שפה", "ברירת מחדל של המערכת",
        "כללי", "הפעלה ממוזערת למגש", "הצג התראה בעת מעבר", "הפעל קיצורי דרך גלובליים",
        "שמור", "ביטול", "מוכן", "הפרופיל הוחל: {0}",
        "נכשל: {0}", "הפרופיל נשמר: {0}", "הפרופיל נמחק", "למחוק את הפרופיל \"{0}\"?",
        "אישור מחיקה", "קיצור הדרך {0} כבר בשימוש בפרופיל אחר.", "לא ניתן לרשום את {0}: {1}", "לחץ והקש קיצור (Ctrl/Alt/Shift/Win + מקש). Esc מנקה.",
        "שם", "ראשי", "פעיל", "רזולוציה",
        "רוחב", "גובה", "Hz", "רענן",
        "מיקום", "X", "Y", "כיוון",
        "לרוחב", "לאורך", "לרוחב (הפוך)", "לאורך (הפוך)",
        "בחר או צור פרופיל.", "עדיין אין פרופילים.\n\nהשתמשו בכפתור בפינה השמאלית העליונה כדי לשמור את תצורת הצגים הנוכחית כפרופיל.", "✓ בשימוש", "DisplayForge — פרופילי ריבוי צגים",
        "DisplayForge כבר פועל.", "פרופיל {0}", "חסרים: {0}", "סגירה מסתירה למגש. השתמש ביציאה מתפריט המגש.",
        "הגדרות תצוגה", "לשמור את הגדרות התצוגה האלה?", "שחזור בעוד {0} שנ‘…", "שמור שינויים",
        "שחזר", "הגדרות התצוגה שוחזרו.", "השחזור נכשל: {0}", "אישור שינויי תצוגה",
        "מציג אישור לאחר מעבר; אם אין תגובה בזמן, הפריסה הקודמת משוחזרת אוטומטית (הגנה אם המסך נשאר שחור).", "אשר לאחר החלה (כפתור / מגש)", "אשר לאחר החלה (קיצור דרך)", "פסק זמן (שניות)",
        "סידור", "גרור מלבנים כדי להזיז צגים. קצוות נצמדים לשכנים.", "בחר פרופיל כדי לסדר צגים.", "זיהוי",
        "הצג מספרים גדולים על כל צג פיזי.", "מציג מספרי צגים…");

    d["hi"] = L(
        "DisplayForge", "मॉनिटर लेआउट सहेजें और एक स्पर्श से बदलें।", "प्रोफ़ाइल", "चयनित प्रोफ़ाइल",
        "वर्तमान मॉनिटर", "प्रोफ़ाइल मॉनिटर", "मान सीधे संपादित करने के लिए सेल पर क्लिक करें।", "वर्तमान कॉन्फ़िग से नई",
        "वर्तमान मॉनिटर सेटअप को नई प्रोफ़ाइल के रूप में सहेजें।", "वर्तमान से अधिलेखित करें", "इस प्रोफ़ाइल को वर्तमान मॉनिटर सेटअप से बदलें।", "लागू करें",
        "मॉनिटर को इस सहेजे गए लेआउट पर स्विच करें।", "डुप्लिकेट", "हटाएँ", "नाम बदलें",
        "नया नाम दर्ज करें", "हॉटकी", "साफ़ करें", "सेटिंग्स",
        "DisplayForge खोलें", "बाहर निकलें", "भाषा", "सिस्टम डिफ़ॉल्ट",
        "सामान्य", "ट्रे में न्यूनतम करके शुरू करें", "स्विच पर सूचना दिखाएँ", "वैश्विक हॉटकी सक्षम करें",
        "सहेजें", "रद्द करें", "तैयार", "प्रोफ़ाइल लागू: {0}",
        "विफल: {0}", "प्रोफ़ाइल सहेजी: {0}", "प्रोफ़ाइल हटाई गई", "प्रोफ़ाइल \"{0}\" हटाएँ?",
        "हटाने की पुष्टि", "हॉटकी {0} पहले से किसी अन्य प्रोफ़ाइल द्वारा उपयोग में है।", "हॉटकी {0} पंजीकृत नहीं हो सकी: {1}", "क्लिक करें और शॉर्टकट दबाएँ (Ctrl/Alt/Shift/Win + कुंजी)। Esc साफ़ करता है।",
        "नाम", "प्राथमिक", "चालू", "रिज़ॉल्यूशन",
        "चौड़ाई", "ऊँचाई", "Hz", "रीफ़्रेश",
        "स्थिति", "X", "Y", "अभिमुखता",
        "लैंडस्केप", "पोर्ट्रेट", "लैंडस्केप (उल्टा)", "पोर्ट्रेट (उल्टा)",
        "प्रोफ़ाइल चुनें या बनाएँ।", "अभी कोई प्रोफ़ाइल नहीं।\n\nवर्तमान मॉनिटर सेटअप को प्रोफ़ाइल के रूप में सहेजने के लिए ऊपर बाएँ बटन का उपयोग करें।", "✓ उपयोग में", "DisplayForge — मल्टी-मॉनिटर प्रोफ़ाइल",
        "DisplayForge पहले से चल रहा है।", "प्रोफ़ाइल {0}", "अनुपस्थित: {0}", "बंद करने पर ट्रे में छिपता है। बाहर निकलने के लिए ट्रे मेनू का उपयोग करें।",
        "डिस्प्ले सेटिंग्स", "ये डिस्प्ले सेटिंग्स रखें?", "{0} सेकंड में वापस…", "परिवर्तन रखें",
        "वापस लें", "डिस्प्ले सेटिंग्स वापस की गईं।", "वापस नहीं कर सके: {0}", "डिस्प्ले परिवर्तन की पुष्टि",
        "स्विच के बाद पुष्टि दिखाता है; समय पर जवाब न देने पर पिछला लेआउट स्वतः बहाल होता है (स्क्रीन काले पड़ने पर सुरक्षा)।", "लागू करने के बाद पुष्टि (बटन / ट्रे)", "लागू करने के बाद पुष्टि (हॉटकी)", "समय सीमा (सेकंड)",
        "व्यवस्था", "मॉनिटर स्थानांतरित करने के लिए आयत खींचें। किनारे पड़ोसियों से चिपकते हैं।", "मॉनिटर व्यवस्थित करने के लिए प्रोफ़ाइल चुनें।", "पहचानें",
        "प्रत्येक भौतिक मॉनिटर पर बड़े नंबर दिखाएँ।", "मॉनिटर नंबर दिखाए जा रहे हैं…");

    d["hu"] = L(
        "DisplayForge", "Mentse a monitor-elrendezéseket, és váltson egy gombnyomással.", "Profilok", "Kiválasztott profil",
        "Jelenlegi monitorok", "Profil monitorai", "Kattintson egy cellára az értékek közvetlen szerkesztéséhez.", "Új a jelenlegi konfigurációból",
        "Az aktuális monitorbeállítás mentése új profilként.", "Felülírás a jelenlegivel", "A profil felülírása az aktuális monitorbeállítással.", "Alkalmaz",
        "Monitorok váltása erre a mentett elrendezésre.", "Másolat", "Törlés", "Átnevezés",
        "Adjon meg új nevet", "Gyorsbillentyű", "Törlés", "Beállítások",
        "DisplayForge megnyitása", "Kilépés", "Nyelv", "Rendszer alapértelmezett",
        "Általános", "Indítás a tálcára kicsinyítve", "Értesítés váltáskor", "Globális gyorsbillentyűk engedélyezése",
        "Mentés", "Mégse", "Kész", "Profil alkalmazva: {0}",
        "Sikertelen: {0}", "Profil mentve: {0}", "Profil törölve", "Törli a(z) „{0}” profilt?",
        "Törlés megerősítése", "A(z) {0} gyorsbillentyűt már más profil használja.", "Nem regisztrálható a(z) {0}: {1}", "Kattintson, majd nyomjon gyorsbillentyűt (Ctrl/Alt/Shift/Win + billentyű). Esc töröl.",
        "Név", "Elsődleges", "Be", "Felbontás",
        "Szélesség", "Magasság", "Hz", "Frissítés",
        "Pozíció", "X", "Y", "Tájolás",
        "Fekvő", "Álló", "Fekvő (tükrözött)", "Álló (tükrözött)",
        "Válasszon vagy hozzon létre profilt.", "Még nincsenek profilok.\n\nA bal felső gombbal mentheti az aktuális monitorbeállítást profilként.", "✓ Aktív", "DisplayForge — többmonitoros profilok",
        "A DisplayForge már fut.", "Profil {0}", "Hiányzik: {0}", "A bezárás a tálcára rejti. Kilépés a tálca menüjéből.",
        "Kijelzőbeállítások", "Megtartja ezeket a kijelzőbeállításokat?", "Visszaállítás {0} mp múlva…", "Megtartás",
        "Visszaállítás", "Kijelzőbeállítások visszaállítva.", "Visszaállítás sikertelen: {0}", "Kijelzőváltozások megerősítése",
        "Váltás után megerősítést mutat; ha időben nincs válasz, az előző elrendezés automatikusan visszaáll (védelem fekete képernyő esetén).", "Megerősítés alkalmazás után (gomb / tálca)", "Megerősítés alkalmazás után (gyorsbillentyű)", "Időkorlát (másodperc)",
        "Elrendezés", "Húzza a téglalapokat a monitorok mozgatásához. A szélek a szomszédokhoz ragadnak.", "Válasszon profilt a monitorok elrendezéséhez.", "Azonosítás",
        "Nagy számok megjelenítése minden fizikai monitoron.", "Monitorszámok megjelenítése…");

    d["id"] = L(
        "DisplayForge", "Simpan tata letak monitor dan beralih dengan satu sentuhan.", "Profil", "Profil terpilih",
        "Monitor saat ini", "Monitor profil", "Klik sel untuk mengedit nilai secara langsung.", "Baru dari konfigurasi saat ini",
        "Simpan konfigurasi monitor saat ini sebagai profil baru.", "Timpa dengan saat ini", "Ganti profil ini dengan konfigurasi monitor saat ini.", "Terapkan",
        "Alihkan monitor ke tata letak tersimpan ini.", "Duplikat", "Hapus", "Ganti nama",
        "Masukkan nama baru", "Pintasan", "Hapus", "Pengaturan",
        "Buka DisplayForge", "Keluar", "Bahasa", "Bawaan sistem",
        "Umum", "Mulai diminimalkan ke baki", "Tampilkan notifikasi saat beralih", "Aktifkan pintasan global",
        "Simpan", "Batal", "Siap", "Profil diterapkan: {0}",
        "Gagal: {0}", "Profil disimpan: {0}", "Profil dihapus", "Hapus profil \"{0}\"?",
        "Konfirmasi hapus", "Pintasan {0} sudah digunakan profil lain.", "Tidak dapat mendaftarkan {0}: {1}", "Klik lalu tekan pintasan (Ctrl/Alt/Shift/Win + tombol). Esc menghapus.",
        "Nama", "Utama", "Aktif", "Resolusi",
        "Lebar", "Tinggi", "Hz", "Segarkan",
        "Posisi", "X", "Y", "Orientasi",
        "Lanskap", "Potret", "Lanskap (terbalik)", "Potret (terbalik)",
        "Pilih atau buat profil.", "Belum ada profil.\n\nGunakan tombol kiri atas untuk menyimpan konfigurasi monitor saat ini sebagai profil.", "✓ Digunakan", "DisplayForge — profil multi-monitor",
        "DisplayForge sudah berjalan.", "Profil {0}", "Hilang: {0}", "Menutup menyembunyikan ke baki. Gunakan Keluar dari menu baki.",
        "Pengaturan tampilan", "Pertahankan pengaturan tampilan ini?", "Mengembalikan dalam {0} d…", "Pertahankan",
        "Kembalikan", "Pengaturan tampilan dikembalikan.", "Gagal mengembalikan: {0}", "Konfirmasi perubahan tampilan",
        "Menampilkan konfirmasi setelah beralih; jika tidak direspons tepat waktu, tata letak sebelumnya dipulihkan otomatis (pengaman jika layar hitam).", "Konfirmasi setelah menerapkan (tombol / baki)", "Konfirmasi setelah menerapkan (pintasan)", "Batas waktu (detik)",
        "Tata letak", "Seret persegi panjang untuk memindahkan monitor. Tepi menempel ke tetangga.", "Pilih profil untuk menata monitor.", "Identifikasi",
        "Tampilkan nomor besar di setiap monitor fisik.", "Menampilkan nomor monitor…");

    d["it"] = L(
        "DisplayForge", "Salva le disposizioni dei monitor e cambia con un tocco.", "Profili", "Profilo selezionato",
        "Monitor attuali", "Monitor del profilo", "Fai clic su una cella per modificare i valori.", "Nuovo dalla config. attuale",
        "Salva la configurazione attuale come nuovo profilo.", "Sovrascrivi con l’attuale", "Sostituisci questo profilo con la configurazione attuale.", "Applica",
        "Passa i monitor a questa disposizione salvata.", "Duplica", "Elimina", "Rinomina",
        "Inserisci un nuovo nome", "Scorciatoia", "Cancella", "Impostazioni",
        "Apri DisplayForge", "Esci", "Lingua", "Predefinita di sistema",
        "Generale", "Avvia ridotto nell’area di notifica", "Notifica al cambio", "Abilita scorciatoie globali",
        "Salva", "Annulla", "Pronto", "Profilo applicato: {0}",
        "Errore: {0}", "Profilo salvato: {0}", "Profilo eliminato", "Eliminare il profilo \"{0}\"?",
        "Conferma eliminazione", "La scorciatoia {0} è già usata da un altro profilo.", "Impossibile registrare la scorciatoia {0}: {1}", "Fai clic e premi una scorciatoia (Ctrl/Alt/Maiusc/Win + tasto). Esc cancella.",
        "Nome", "Principale", "Sì", "Risoluzione",
        "Larghezza", "Altezza", "Hz", "Aggiorna",
        "Posizione", "X", "Y", "Orientamento",
        "Orizzontale", "Verticale", "Orizzontale (capovolto)", "Verticale (capovolto)",
        "Seleziona o crea un profilo.", "Nessun profilo ancora.\n\nUsa il pulsante in alto a sinistra per salvare la configurazione attuale come profilo.", "✓ In uso", "DisplayForge — profili multi-monitor",
        "DisplayForge è già in esecuzione.", "Profilo {0}", "Mancanti: {0}", "Chiudere nasconde nell’area di notifica. Usa Esci dal menu per uscire.",
        "Impostazioni schermo", "Mantenere queste impostazioni schermo?", "Ripristino tra {0} s…", "Mantieni",
        "Ripristina", "Impostazioni schermo ripristinate.", "Ripristino non riuscito: {0}", "Conferma modifiche schermo",
        "Mostra una conferma dopo il cambio; se non rispondi in tempo, la disposizione precedente viene ripristinata automaticamente (protezione se lo schermo diventa nero).", "Conferma dopo l’applicazione (pulsante / area di notifica)", "Conferma dopo l’applicazione (scorciatoia)", "Timeout (secondi)",
        "Disposizione", "Trascina i rettangoli per spostare i monitor. I bordi si agganciano ai vicini.", "Seleziona un profilo per disporre i monitor.", "Identifica",
        "Mostra numeri grandi su ogni monitor fisico.", "Visualizzazione numeri monitor…");

    d["ko"] = L(
        "DisplayForge", "모니터 배치·해상도를 저장하고 원터치로 전환할 수 있습니다.", "프로필", "선택한 프로필",
        "현재 모니터", "프로필 모니터", "셀을 클릭하면 값을 직접 편집할 수 있습니다.", "현재 구성으로 새로 만들기",
        "현재 모니터 구성을 새 프로필로 저장합니다.", "현재 구성으로 덮어쓰기", "선택한 프로필을 현재 모니터 구성으로 덮어씁니다.", "적용",
        "저장한 구성대로 모니터를 전환합니다.", "복제", "삭제", "이름 바꾸기",
        "새 이름을 입력하세요", "단축키", "지우기", "설정",
        "DisplayForge 열기", "종료", "언어", "시스템 기본값",
        "일반", "시작 시 트레이로 최소화", "전환 시 알림 표시", "전역 단축키 사용",
        "저장", "취소", "준비됨", "프로필 적용됨: {0}",
        "실패: {0}", "프로필 저장됨: {0}", "프로필 삭제됨", "프로필 \"{0}\"을(를) 삭제할까요?",
        "삭제 확인", "단축키 {0}은(는) 다른 프로필에서 사용 중입니다.", "단축키 {0}을(를) 등록할 수 없음: {1}", "클릭한 뒤 단축키를 누르세요(Ctrl/Alt/Shift/Win + 키). Esc로 지웁니다.",
        "이름", "주", "켜짐", "해상도",
        "너비", "높이", "Hz", "새로 고침",
        "위치", "X", "Y", "방향",
        "가로", "세로", "가로(뒤집힘)", "세로(뒤집힘)",
        "프로필을 선택하거나 만드세요.", "아직 프로필이 없습니다.\n\n왼쪽 위 「현재 구성으로 새로 만들기」를 누르면\n현재 모니터 구성이 저장되어 언제든 불러올 수 있습니다.", "✓ 적용 중", "DisplayForge — 멀티 모니터 프로필",
        "DisplayForge가 이미 실행 중입니다.", "프로필 {0}", "연결 안 됨: {0}", "닫으면 트레이로 숨깁니다. 종료는 트레이 메뉴를 사용하세요.",
        "디스플레이 설정", "이 디스플레이 설정을 유지할까요?", "{0}초 후 되돌림…", "변경 유지",
        "되돌리기", "디스플레이 설정을 되돌렸습니다.", "되돌리기 실패: {0}", "디스플레이 변경 확인",
        "전환 후 확인 화면을 표시하며, 제한 시간 안에 조작이 없으면 이전 구성으로 자동 복원합니다(화면이 안 나올 때의 안전장치).", "적용 후 확인 (버튼 / 트레이)", "적용 후 확인 (단축키)", "제한 시간(초)",
        "배치", "사각형을 끌어 모니터를 이동합니다. 가장자리가 이웃에 스냅됩니다.", "모니터를 배치할 프로필을 선택하세요.", "식별",
        "각 실제 모니터에 큰 번호를 표시합니다.", "모니터 번호 표시 중…");

    d["ms"] = L(
        "DisplayForge", "Simpan susunan monitor dan tukar dengan satu sentuhan.", "Profil", "Profil dipilih",
        "Monitor semasa", "Monitor profil", "Klik sel untuk mengedit nilai secara terus.", "Baharu daripada konfigurasi semasa",
        "Simpan konfigurasi monitor semasa sebagai profil baharu.", "Tulis ganti dengan semasa", "Ganti profil ini dengan konfigurasi monitor semasa.", "Guna",
        "Tukar monitor kepada susunan tersimpan ini.", "Pendua", "Padam", "Namakan semula",
        "Masukkan nama baharu", "Pintasan", "Kosongkan", "Tetapan",
        "Buka DisplayForge", "Keluar", "Bahasa", "Lalai sistem",
        "Umum", "Mula diminimumkan ke dulang", "Tunjuk pemberitahuan apabila menukar", "Dayakan pintasan global",
        "Simpan", "Batal", "Sedia", "Profil digunakan: {0}",
        "Gagal: {0}", "Profil disimpan: {0}", "Profil dipadam", "Padam profil \"{0}\"?",
        "Sahkan padam", "Pintasan {0} sudah digunakan oleh profil lain.", "Tidak dapat mendaftar {0}: {1}", "Klik lalu tekan pintasan (Ctrl/Alt/Shift/Win + kekunci). Esc mengosongkan.",
        "Nama", "Utama", "Hidup", "Resolusi",
        "Lebar", "Tinggi", "Hz", "Muat semula",
        "Kedudukan", "X", "Y", "Orientasi",
        "Landskap", "Potret", "Landskap (songsang)", "Potret (songsang)",
        "Pilih atau cipta profil.", "Belum ada profil.\n\nGunakan butang kiri atas untuk menyimpan konfigurasi monitor semasa sebagai profil.", "✓ Digunakan", "DisplayForge — profil berbilang monitor",
        "DisplayForge sudah berjalan.", "Profil {0}", "Tiada: {0}", "Menutup menyembunyikan ke dulang. Gunakan Keluar daripada menu dulang.",
        "Tetapan paparan", "Kekalkan tetapan paparan ini?", "Mengembalikan dalam {0} s…", "Kekalkan",
        "Kembalikan", "Tetapan paparan dikembalikan.", "Gagal mengembalikan: {0}", "Sahkan perubahan paparan",
        "Menunjukkan pengesahan selepas menukar; jika tiada respons dalam masa, susunan sebelumnya dipulihkan secara automatik (perlindungan jika skrin hitam).", "Sahkan selepas guna (butang / dulang)", "Sahkan selepas guna (pintasan)", "Tamat masa (saat)",
        "Susunan", "Seret segi empat untuk alihkan monitor. Tepi melekat pada jiran.", "Pilih profil untuk menyusun monitor.", "Kenal pasti",
        "Tunjuk nombor besar pada setiap monitor fizikal.", "Menunjukkan nombor monitor…");

    d["nb"] = L(
        "DisplayForge", "Lagre skjermoppsett og bytt med ett trykk.", "Profiler", "Valgt profil",
        "Gjeldende skjermer", "Profilskjermer", "Klikk på en celle for å redigere verdier direkte.", "Ny fra gjeldende konfigurasjon",
        "Lagre gjeldende skjermoppsett som en ny profil.", "Overskriv med gjeldende", "Erstatt denne profilen med gjeldende skjermoppsett.", "Bruk",
        "Bytt skjermer til dette lagrede oppsettet.", "Dupliser", "Slett", "Gi nytt navn",
        "Skriv inn et nytt navn", "Hurtigtast", "Tøm", "Innstillinger",
        "Åpne DisplayForge", "Avslutt", "Språk", "Systemstandard",
        "Generelt", "Start minimert i systemstatusfeltet", "Vis varsel ved bytte", "Aktiver globale hurtigtaster",
        "Lagre", "Avbryt", "Klar", "Profil brukt: {0}",
        "Mislyktes: {0}", "Profil lagret: {0}", "Profil slettet", "Slette profilen \"{0}\"?",
        "Bekreft sletting", "Hurtigtasten {0} brukes allerede av en annen profil.", "Kunne ikke registrere {0}: {1}", "Klikk og trykk en hurtigtast (Ctrl/Alt/Shift/Win + tast). Esc tømmer.",
        "Navn", "Primær", "På", "Oppløsning",
        "Bredde", "Høyde", "Hz", "Oppdater",
        "Posisjon", "X", "Y", "Retning",
        "Liggende", "Stående", "Liggende (speilvendt)", "Stående (speilvendt)",
        "Velg eller opprett en profil.", "Ingen profiler ennå.\n\nBruk knappen øverst til venstre for å lagre gjeldende skjermoppsett som profil.", "✓ Aktiv", "DisplayForge — flerskjermprofiler",
        "DisplayForge kjører allerede.", "Profil {0}", "Mangler: {0}", "Lukking skjuler til systemstatusfeltet. Avslutt fra menyen der.",
        "Skjerminnstillinger", "Beholde disse skjerminnstillingene?", "Tilbakestiller om {0} s…", "Behold",
        "Tilbakestill", "Skjerminnstillinger tilbakestilt.", "Kunne ikke tilbakestille: {0}", "Bekreft skjermendringer",
        "Viser en bekreftelse etter bytte; uten svar i tide gjenopprettes forrige oppsett automatisk (sikkerhet ved svart skjerm).", "Bekreft etter bruk (knapp / systemstatusfelt)", "Bekreft etter bruk (hurtigtast)", "Tidsavbrudd (sekunder)",
        "Oppsett", "Dra rektangler for å flytte skjermer. Kanter festes til naboer.", "Velg en profil for å ordne skjermer.", "Identifiser",
        "Vis store tall på hver fysisk skjerm.", "Viser skjermnumre…");

    d["nl"] = L(
        "DisplayForge", "Sla monitorindelingen op en wissel met één druk.", "Profielen", "Geselecteerd profiel",
        "Huidige monitoren", "Profielmonitoren", "Klik op een cel om waarden direct te bewerken.", "Nieuw vanuit huidige config.",
        "Huidige monitorconfiguratie als nieuw profiel opslaan.", "Overschrijven met huidige", "Dit profiel vervangen door de huidige monitorconfiguratie.", "Toepassen",
        "Monitors omschakelen naar deze opgeslagen indeling.", "Dupliceren", "Verwijderen", "Hernoemen",
        "Voer een nieuwe naam in", "Sneltoets", "Wissen", "Instellingen",
        "DisplayForge openen", "Afsluiten", "Taal", "Systeemstandaard",
        "Algemeen", "Geminimaliseerd starten in systeemvak", "Melding bij wisselen", "Algemene sneltoetsen inschakelen",
        "Opslaan", "Annuleren", "Gereed", "Profiel toegepast: {0}",
        "Mislukt: {0}", "Profiel opgeslagen: {0}", "Profiel verwijderd", "Profiel \"{0}\" verwijderen?",
        "Verwijderen bevestigen", "Sneltoets {0} wordt al door een ander profiel gebruikt.", "Kan sneltoets {0} niet registreren: {1}", "Klik en druk op een sneltoets (Ctrl/Alt/Shift/Win + toets). Esc wist.",
        "Naam", "Primair", "Aan", "Resolutie",
        "Breedte", "Hoogte", "Hz", "Vernieuwen",
        "Positie", "X", "Y", "Oriëntatie",
        "Liggend", "Staand", "Liggend (omgedraaid)", "Staand (omgedraaid)",
        "Selecteer of maak een profiel.", "Nog geen profielen.\n\nGebruik de knop linksboven om de huidige monitorconfiguratie als profiel op te slaan.", "✓ Actief", "DisplayForge — multimmonitorprofielen",
        "DisplayForge is al gestart.", "Profiel {0}", "Ontbrekend: {0}", "Sluiten verbergt naar het systeemvak. Gebruik Afsluiten in het menu.",
        "Beeldscherminstellingen", "Deze beeldscherminstellingen behouden?", "Terugzetten over {0} s…", "Behouden",
        "Terugzetten", "Beeldscherminstellingen teruggezet.", "Terugzetten mislukt: {0}", "Beeldschermwijzigingen bevestigen",
        "Toont na omschakelen een bevestiging; zonder reactie binnen de tijd wordt de vorige indeling automatisch hersteld (veiligheid bij zwart scherm).", "Bevestigen na toepassen (knop / systeemvak)", "Bevestigen na toepassen (sneltoets)", "Time-out (seconden)",
        "Indeling", "Sleep rechthoeken om monitoren te verplaatsen. Randen kleven aan buren.", "Selecteer een profiel om monitoren te ordenen.", "Identificeren",
        "Grote nummers op elke fysieke monitor tonen.", "Monitornummers worden getoond…");

    d["pl"] = L(
        "DisplayForge", "Zapisuj układy monitorów i przełączaj je jednym dotknięciem.", "Profile", "Wybrany profil",
        "Bieżące monitory", "Monitory profilu", "Kliknij komórkę, aby bezpośrednio edytować wartości.", "Nowy z bieżącej konfiguracji",
        "Zapisz bieżącą konfigurację monitorów jako nowy profil.", "Nadpisz bieżącą", "Zastąp ten profil bieżącą konfiguracją monitorów.", "Zastosuj",
        "Przełącz monitory na ten zapisany układ.", "Duplikuj", "Usuń", "Zmień nazwę",
        "Wpisz nową nazwę", "Skrót", "Wyczyść", "Ustawienia",
        "Otwórz DisplayForge", "Zakończ", "Język", "Domyślny systemowy",
        "Ogólne", "Uruchom zminimalizowany w zasobniku", "Powiadomienie przy przełączaniu", "Włącz globalne skróty",
        "Zapisz", "Anuluj", "Gotowe", "Zastosowano profil: {0}",
        "Błąd: {0}", "Zapisano profil: {0}", "Usunięto profil", "Usunąć profil „{0}”?",
        "Potwierdź usunięcie", "Skrót {0} jest już używany przez inny profil.", "Nie można zarejestrować skrótu {0}: {1}", "Kliknij i naciśnij skrót (Ctrl/Alt/Shift/Win + klawisz). Esc czyści.",
        "Nazwa", "Główny", "Wł.", "Rozdzielczość",
        "Szerokość", "Wysokość", "Hz", "Odśwież",
        "Pozycja", "X", "Y", "Orientacja",
        "Poziomo", "Pionowo", "Poziomo (odwrócone)", "Pionowo (odwrócone)",
        "Wybierz lub utwórz profil.", "Brak profili.\n\nUżyj przycisku w lewym górnym rogu, aby zapisać bieżącą konfigurację monitorów jako profil.", "✓ Aktywny", "DisplayForge — profile wielu monitorów",
        "DisplayForge jest już uruchomiony.", "Profil {0}", "Brak: {0}", "Zamknięcie ukrywa w zasobniku. Zakończ z menu zasobnika.",
        "Ustawienia wyświetlania", "Zachować te ustawienia wyświetlania?", "Przywracanie za {0} s…", "Zachowaj",
        "Przywróć", "Przywrócono ustawienia wyświetlania.", "Nie udało się przywrócić: {0}", "Potwierdź zmiany wyświetlania",
        "Po przełączeniu pokazuje potwierdzenie; bez odpowiedzi w limicie czasu automatycznie przywraca poprzedni układ (zabezpieczenie przy czarnym ekranie).", "Potwierdź po zastosowaniu (przycisk / zasobnik)", "Potwierdź po zastosowaniu (skrót)", "Limit czasu (sekundy)",
        "Układ", "Przeciągnij prostokąty, aby przenieść monitory. Krawędzie przyciągają do sąsiadów.", "Wybierz profil, aby ułożyć monitory.", "Identyfikuj",
        "Pokaż duże numery na każdym fizycznym monitorze.", "Wyświetlanie numerów monitorów…");

    d["pt-BR"] = L(
        "DisplayForge", "Salve layouts de monitores e alterne com um toque.", "Perfis", "Perfil selecionado",
        "Monitores atuais", "Monitores do perfil", "Clique em uma célula para editar os valores.", "Novo a partir da config. atual",
        "Salvar a configuração atual como um novo perfil.", "Substituir pela atual", "Substituir este perfil pela configuração atual.", "Aplicar",
        "Alternar monitores para este layout salvo.", "Duplicar", "Excluir", "Renomear",
        "Digite um novo nome", "Atalho", "Limpar", "Configurações",
        "Abrir DisplayForge", "Sair", "Idioma", "Padrão do sistema",
        "Geral", "Iniciar minimizado na bandeja", "Mostrar notificação ao alternar", "Ativar atalhos globais",
        "Salvar", "Cancelar", "Pronto", "Perfil aplicado: {0}",
        "Falha: {0}", "Perfil salvo: {0}", "Perfil excluído", "Excluir o perfil \"{0}\"?",
        "Confirmar exclusão", "O atalho {0} já é usado por outro perfil.", "Não foi possível registrar o atalho {0}: {1}", "Clique e pressione um atalho (Ctrl/Alt/Shift/Win + tecla). Esc limpa.",
        "Nome", "Principal", "Sim", "Resolução",
        "Largura", "Altura", "Hz", "Atualizar",
        "Posição", "X", "Y", "Orientação",
        "Paisagem", "Retrato", "Paisagem (invertida)", "Retrato (invertido)",
        "Selecione ou crie um perfil.", "Ainda não há perfis.\n\nUse o botão no canto superior esquerdo para salvar a configuração atual como perfil.", "✓ Em uso", "DisplayForge — perfis multimonitor",
        "O DisplayForge já está em execução.", "Perfil {0}", "Ausentes: {0}", "Fechar oculta na bandeja. Use Sair no menu da bandeja para encerrar.",
        "Configurações de exibição", "Manter estas configurações de exibição?", "Revertendo em {0} s…", "Manter",
        "Reverter", "Configurações de exibição revertidas.", "Falha ao reverter: {0}", "Confirmar alterações de exibição",
        "Mostra uma confirmação após a troca; se não houver resposta a tempo, o layout anterior é restaurado automaticamente (proteção se a tela ficar preta).", "Confirmar após aplicar (botão / bandeja)", "Confirmar após aplicar (atalho)", "Tempo limite (segundos)",
        "Disposição", "Arraste os retângulos para mover monitores. As bordas encaixam nos vizinhos.", "Selecione um perfil para dispor monitores.", "Identificar",
        "Mostrar números grandes em cada monitor físico.", "Exibindo números dos monitores…");

    d["pt-PT"] = L(
        "DisplayForge", "Guarde disposições de monitores e alterne com um toque.", "Perfis", "Perfil selecionado",
        "Monitores atuais", "Monitores do perfil", "Clique numa célula para editar os valores.", "Novo a partir da config. atual",
        "Guardar a configuração atual como um novo perfil.", "Substituir pela atual", "Substituir este perfil pela configuração atual.", "Aplicar",
        "Alternar monitores para esta disposição guardada.", "Duplicar", "Eliminar", "Mudar o nome",
        "Introduza um novo nome", "Atalho", "Limpar", "Definições",
        "Abrir DisplayForge", "Sair", "Idioma", "Predefinição do sistema",
        "Geral", "Iniciar minimizado na área de notificação", "Mostrar notificação ao mudar", "Ativar atalhos globais",
        "Guardar", "Cancelar", "Pronto", "Perfil aplicado: {0}",
        "Falha: {0}", "Perfil guardado: {0}", "Perfil eliminado", "Eliminar o perfil \"{0}\"?",
        "Confirmar eliminação", "O atalho {0} já é utilizado por outro perfil.", "Não foi possível registar o atalho {0}: {1}", "Clique e prima um atalho (Ctrl/Alt/Shift/Win + tecla). Esc limpa.",
        "Nome", "Principal", "Sim", "Resolução",
        "Largura", "Altura", "Hz", "Atualizar",
        "Posição", "X", "Y", "Orientação",
        "Paisagem", "Retrato", "Paisagem (invertida)", "Retrato (invertido)",
        "Selecione ou crie um perfil.", "Ainda não existem perfis.\n\nUtilize o botão no canto superior esquerdo para guardar a configuração atual como perfil.", "✓ Em uso", "DisplayForge — perfis multi-monitor",
        "O DisplayForge já está em execução.", "Perfil {0}", "Em falta: {0}", "Fechar oculta na área de notificação. Utilize Sair no menu para terminar.",
        "Definições de ecrã", "Manter estas definições de ecrã?", "A reverter em {0} s…", "Manter",
        "Reverter", "Definições de ecrã revertidas.", "Falha ao reverter: {0}", "Confirmar alterações de ecrã",
        "Mostra uma confirmação após a mudança; se não responder a tempo, a disposição anterior é restaurada automaticamente (segurança se o ecrã ficar preto).", "Confirmar após aplicar (botão / área de notificação)", "Confirmar após aplicar (atalho)", "Tempo limite (segundos)",
        "Disposição", "Arraste os retângulos para mover monitores. As margens encaixam nos vizinhos.", "Selecione um perfil para dispor monitores.", "Identificar",
        "Mostrar números grandes em cada monitor físico.", "A mostrar números dos monitores…");

    d["ro"] = L(
        "DisplayForge", "Salvați aranjamentele monitoarelor și comutați dintr-o atingere.", "Profiluri", "Profil selectat",
        "Monitoare curente", "Monitoarele profilului", "Faceți clic pe o celulă pentru a edita valorile.", "Nou din configurația curentă",
        "Salvați configurația actuală ca profil nou.", "Suprascrie cu cea curentă", "Înlocuiți acest profil cu configurația actuală.", "Aplică",
        "Comutați monitoarele la acest aranjament salvat.", "Duplică", "Șterge", "Redenumește",
        "Introduceți un nume nou", "Scurtătură", "Șterge", "Setări",
        "Deschide DisplayForge", "Ieșire", "Limbă", "Implicit sistem",
        "General", "Pornește minimizat în zona de notificare", "Notificare la comutare", "Activează scurtăturile globale",
        "Salvează", "Anulează", "Gata", "Profil aplicat: {0}",
        "Eșec: {0}", "Profil salvat: {0}", "Profil șters", "Ștergeți profilul „{0}”?",
        "Confirmare ștergere", "Scurtătura {0} este deja folosită de alt profil.", "Nu s-a putut înregistra {0}: {1}", "Faceți clic și apăsați o scurtătură (Ctrl/Alt/Shift/Win + tastă). Esc șterge.",
        "Nume", "Principal", "Pornit", "Rezoluție",
        "Lățime", "Înălțime", "Hz", "Reîmprospătează",
        "Poziție", "X", "Y", "Orientare",
        "Peisaj", "Portret", "Peisaj (inversat)", "Portret (inversat)",
        "Selectați sau creați un profil.", "Nu există încă profiluri.\n\nFolosiți butonul din stânga sus pentru a salva configurația actuală ca profil.", "✓ Activ", "DisplayForge — profiluri multi-monitor",
        "DisplayForge rulează deja.", "Profil {0}", "Lipsă: {0}", "Închiderea ascunde în zona de notificare. Ieșiți din meniul zonei de notificare.",
        "Setări afișaj", "Păstrați aceste setări de afișaj?", "Revenire în {0} s…", "Păstrează",
        "Revino", "Setările de afișaj au fost restabilite.", "Nu s-a putut restabili: {0}", "Confirmare modificări afișaj",
        "Afișează o confirmare după comutare; fără răspuns la timp, aranjamentul anterior este restaurat automat (siguranță dacă ecranul devine negru).", "Confirmă după aplicare (buton / zonă de notificare)", "Confirmă după aplicare (scurtătură)", "Timp limită (secunde)",
        "Aranjament", "Trageți dreptunghiurile pentru a muta monitoarele. Marginile se aliniază la vecini.", "Selectați un profil pentru a aranja monitoarele.", "Identificare",
        "Afișați numere mari pe fiecare monitor fizic.", "Se afișează numerele monitoarelor…");

    d["ru"] = L(
        "DisplayForge", "Сохраняйте раскладки мониторов и переключайте одним касанием.", "Профили", "Выбранный профиль",
        "Текущие мониторы", "Мониторы профиля", "Щёлкните ячейку, чтобы изменить значения.", "Новый из текущей конфигурации",
        "Сохранить текущую конфигурацию мониторов как новый профиль.", "Перезаписать текущей", "Заменить этот профиль текущей конфигурацией мониторов.", "Применить",
        "Переключить мониторы на эту сохранённую раскладку.", "Дублировать", "Удалить", "Переименовать",
        "Введите новое имя", "Горячая клавиша", "Очистить", "Параметры",
        "Открыть DisplayForge", "Выход", "Язык", "Системный",
        "Общие", "Запускать свёрнутым в трей", "Уведомление при переключении", "Включить глобальные горячие клавиши",
        "Сохранить", "Отмена", "Готово", "Профиль применён: {0}",
        "Ошибка: {0}", "Профиль сохранён: {0}", "Профиль удалён", "Удалить профиль «{0}»?",
        "Подтверждение удаления", "Горячая клавиша {0} уже используется другим профилем.", "Не удалось зарегистрировать {0}: {1}", "Нажмите и введите сочетание (Ctrl/Alt/Shift/Win + клавиша). Esc очищает.",
        "Имя", "Основной", "Вкл.", "Разрешение",
        "Ширина", "Высота", "Гц", "Обновить",
        "Позиция", "X", "Y", "Ориентация",
        "Альбомная", "Книжная", "Альбомная (перевёрнутая)", "Книжная (перевёрнутая)",
        "Выберите или создайте профиль.", "Профилей пока нет.\n\nИспользуйте кнопку слева сверху, чтобы сохранить текущую конфигурацию мониторов как профиль.", "✓ Активен", "DisplayForge — профили нескольких мониторов",
        "DisplayForge уже запущен.", "Профиль {0}", "Нет: {0}", "Закрытие скрывает в трей. Выход — из меню трея.",
        "Параметры экрана", "Сохранить эти параметры экрана?", "Отмена через {0} с…", "Сохранить",
        "Отменить", "Параметры экрана отменены.", "Не удалось отменить: {0}", "Подтверждение изменений экрана",
        "После переключения показывает подтверждение; если нет ответа вовремя, предыдущая раскладка восстанавливается автоматически (защита при чёрном экране).", "Подтверждать после применения (кнопка / трей)", "Подтверждать после применения (горячая клавиша)", "Тайм-аут (секунды)",
        "Расположение", "Перетаскивайте прямоугольники, чтобы перемещать мониторы. Края прилипают к соседним.", "Выберите профиль, чтобы расположить мониторы.", "Определить",
        "Показать крупные номера на каждом физическом мониторе.", "Показ номеров мониторов…");

    d["sv"] = L(
        "DisplayForge", "Spara skärmlayouter och växla med en knapptryckning.", "Profiler", "Vald profil",
        "Aktuella bildskärmar", "Profilens bildskärmar", "Klicka på en cell för att redigera värden direkt.", "Ny från aktuell konfiguration",
        "Spara aktuell skärmkonfiguration som en ny profil.", "Skriv över med aktuell", "Ersätt den här profilen med aktuell skärmkonfiguration.", "Använd",
        "Växla skärmar till den här sparade layouten.", "Duplicera", "Ta bort", "Byt namn",
        "Ange ett nytt namn", "Kortkommando", "Rensa", "Inställningar",
        "Öppna DisplayForge", "Avsluta", "Språk", "Systemets standard",
        "Allmänt", "Starta minimerad i meddelandefältet", "Visa avisering vid byte", "Aktivera globala kortkommandon",
        "Spara", "Avbryt", "Klar", "Profil tillämpad: {0}",
        "Misslyckades: {0}", "Profil sparad: {0}", "Profil borttagen", "Ta bort profilen \"{0}\"?",
        "Bekräfta borttagning", "Kortkommandot {0} används redan av en annan profil.", "Kunde inte registrera {0}: {1}", "Klicka och tryck på ett kortkommando (Ctrl/Alt/Shift/Win + tangent). Esc rensar.",
        "Namn", "Primär", "På", "Upplösning",
        "Bredd", "Höjd", "Hz", "Uppdatera",
        "Position", "X", "Y", "Orientering",
        "Liggande", "Stående", "Liggande (vriden)", "Stående (vriden)",
        "Välj eller skapa en profil.", "Inga profiler ännu.\n\nAnvänd knappen uppe till vänster för att spara aktuell skärmkonfiguration som profil.", "✓ Aktiv", "DisplayForge — flerskärmsprofiler",
        "DisplayForge körs redan.", "Profil {0}", "Saknas: {0}", "Stäng döljer till meddelandefältet. Avsluta via menyn i meddelandefältet.",
        "Bildskärmsinställningar", "Behåll dessa bildskärmsinställningar?", "Återställer om {0} s…", "Behåll",
        "Återställ", "Bildskärmsinställningar återställda.", "Kunde inte återställa: {0}", "Bekräfta skärmändringar",
        "Visar en bekräftelse efter växling; utan svar i tid återställs den tidigare layouten automatiskt (skydd om skärmen blir svart).", "Bekräfta efter tillämpning (knapp / meddelandefält)", "Bekräfta efter tillämpning (kortkommando)", "Tidsgräns (sekunder)",
        "Arrangemang", "Dra rektanglar för att flytta skärmar. Kanter fäster vid grannar.", "Välj en profil för att arrangera skärmar.", "Identifiera",
        "Visa stora nummer på varje fysisk skärm.", "Visar skärmnummer…");

    d["th"] = L(
        "DisplayForge", "บันทึกการจัดวางจอภาพแล้วสลับได้ในครั้งเดียว", "โปรไฟล์", "โปรไฟล์ที่เลือก",
        "จอภาพปัจจุบัน", "จอภาพในโปรไฟล์", "คลิกเซลล์เพื่อแก้ไขค่าได้โดยตรง", "สร้างใหม่จากค่าปัจจุบัน",
        "บันทึกการตั้งค่าจอปัจจุบันเป็นโปรไฟล์ใหม่", "เขียนทับด้วยค่าปัจจุบัน", "แทนที่โปรไฟล์นี้ด้วยการตั้งค่าจอปัจจุบัน", "ใช้",
        "สลับจอภาพตามเค้าโครงที่บันทึกไว้", "ทำซ้ำ", "ลบ", "เปลี่ยนชื่อ",
        "ป้อนชื่อใหม่", "คีย์ลัด", "ล้าง", "การตั้งค่า",
        "เปิด DisplayForge", "ออก", "ภาษา", "ตามระบบ",
        "ทั่วไป", "เริ่มแบบย่อไปที่ถาดระบบ", "แสดงการแจ้งเตือนเมื่อสลับ", "เปิดใช้คีย์ลัดส่วนกลาง",
        "บันทึก", "ยกเลิก", "พร้อม", "ใช้โปรไฟล์แล้ว: {0}",
        "ล้มเหลว: {0}", "บันทึกโปรไฟล์แล้ว: {0}", "ลบโปรไฟล์แล้ว", "ลบโปรไฟล์ \"{0}\" หรือไม่?",
        "ยืนยันการลบ", "คีย์ลัด {0} ถูกโปรไฟล์อื่นใช้แล้ว", "ลงทะเบียนคีย์ลัด {0} ไม่ได้: {1}", "คลิกแล้วกดคีย์ลัด (Ctrl/Alt/Shift/Win + ปุ่ม) Esc เพื่อล้าง",
        "ชื่อ", "หลัก", "เปิด", "ความละเอียด",
        "กว้าง", "สูง", "Hz", "รีเฟรช",
        "ตำแหน่ง", "X", "Y", "การวางแนว",
        "แนวนอน", "แนวตั้ง", "แนวนอน (กลับด้าน)", "แนวตั้ง (กลับด้าน)",
        "เลือกหรือสร้างโปรไฟล์", "ยังไม่มีโปรไฟล์\n\nใช้ปุ่มมุมซ้ายบนเพื่อบันทึกการตั้งค่าจอปัจจุบันเป็นโปรไฟล์", "✓ กำลังใช้", "DisplayForge — โปรไฟล์หลายจอ",
        "DisplayForge กำลังทำงานอยู่แล้ว", "โปรไฟล์ {0}", "ไม่พบ: {0}", "ปิดแล้วจะซ่อนไปที่ถาด ใช้ ออก จากเมนูถาดเพื่อปิดโปรแกรม",
        "การตั้งค่าจอแสดงผล", "เก็บการตั้งค่าจอแสดงผลเหล่านี้หรือไม่?", "จะคืนค่าใน {0} วินาที…", "เก็บการเปลี่ยนแปลง",
        "คืนค่า", "คืนค่าการตั้งค่าจอแสดงผลแล้ว", "คืนค่าไม่สำเร็จ: {0}", "ยืนยันการเปลี่ยนจอแสดงผล",
        "แสดงหน้ายืนยันหลังสลับ หากไม่ตอบภายในเวลาที่กำหนด จะคืนค่าเค้าโครงเดิมโดยอัตโนมัติ (กันจอดับ)", "ยืนยันหลังใช้ (ปุ่ม / ถาด)", "ยืนยันหลังใช้ (คีย์ลัด)", "หมดเวลา (วินาที)",
        "การจัดวาง", "ลากสี่เหลี่ยมเพื่อย้ายจอ ขอบจะยึดกับจอข้างเคียง", "เลือกโปรไฟล์เพื่อจัดวางจอ", "ระบุ",
        "แสดงหมายเลขขนาดใหญ่บนจอจริงแต่ละจอ", "กำลังแสดงหมายเลขจอ…");

    d["tr"] = L(
        "DisplayForge", "Monitör düzenlerini kaydedin ve tek dokunuşla geçiş yapın.", "Profiller", "Seçili profil",
        "Geçerli monitörler", "Profil monitörleri", "Değerleri doğrudan düzenlemek için bir hücreye tıklayın.", "Geçerli yapılandırmadan yeni",
        "Geçerli monitör yapılandırmasını yeni profil olarak kaydet.", "Geçerli ile üzerine yaz", "Bu profili geçerli monitör yapılandırmasıyla değiştir.", "Uygula",
        "Monitörleri bu kayıtlı düzene geçir.", "Çoğalt", "Sil", "Yeniden adlandır",
        "Yeni bir ad girin", "Kısayol", "Temizle", "Ayarlar",
        "DisplayForge’u aç", "Çıkış", "Dil", "Sistem varsayılanı",
        "Genel", "Tepsiye küçültülmüş başlat", "Geçişte bildirim göster", "Genel kısayolları etkinleştir",
        "Kaydet", "İptal", "Hazır", "Profil uygulandı: {0}",
        "Başarısız: {0}", "Profil kaydedildi: {0}", "Profil silindi", "\"{0}\" profili silinsin mi?",
        "Silmeyi onayla", "{0} kısayolu başka bir profil tarafından kullanılıyor.", "{0} kısayolu kaydedilemedi: {1}", "Tıklayın ve kısayola basın (Ctrl/Alt/Shift/Win + tuş). Esc temizler.",
        "Ad", "Birincil", "Açık", "Çözünürlük",
        "Genişlik", "Yükseklik", "Hz", "Yenile",
        "Konum", "X", "Y", "Yönlendirme",
        "Yatay", "Dikey", "Yatay (ters)", "Dikey (ters)",
        "Bir profil seçin veya oluşturun.", "Henüz profil yok.\n\nGeçerli monitör yapılandırmasını profil olarak kaydetmek için sol üstteki düğmeyi kullanın.", "✓ Kullanımda", "DisplayForge — çoklu monitör profilleri",
        "DisplayForge zaten çalışıyor.", "Profil {0}", "Eksik: {0}", "Kapatmak tepsiye gizler. Çıkmak için tepsi menüsünü kullanın.",
        "Görüntü ayarları", "Bu görüntü ayarları korunsun mu?", "{0} sn içinde geri alınacak…", "Değişiklikleri koru",
        "Geri al", "Görüntü ayarları geri alındı.", "Geri alınamadı: {0}", "Ekran değişikliklerini onayla",
        "Geçişten sonra onay gösterir; süre içinde yanıt yoksa önceki düzen otomatik geri yüklenir (ekran kararırsa güvenlik ağı).", "Uyguladıktan sonra onayla (düğme / tepsi)", "Uyguladıktan sonra onayla (kısayol)", "Zaman aşımı (saniye)",
        "Düzen", "Monitörleri taşımak için dikdörtgenleri sürükleyin. Kenarlar komşulara yapışır.", "Monitörleri düzenlemek için bir profil seçin.", "Tanımla",
        "Her fiziksel monitörde büyük numaralar göster.", "Monitör numaraları gösteriliyor…");

    d["uk"] = L(
        "DisplayForge", "Зберігайте розкладки моніторів і перемикайте одним дотиком.", "Профілі", "Вибраний профіль",
        "Поточні монітори", "Монітори профілю", "Клацніть клітинку, щоб змінити значення.", "Новий із поточної конфігурації",
        "Зберегти поточну конфігурацію моніторів як новий профіль.", "Перезаписати поточною", "Замінити цей профіль поточною конфігурацією моніторів.", "Застосувати",
        "Перемкнути монітори на цю збережену розкладку.", "Дублювати", "Видалити", "Перейменувати",
        "Введіть нову назву", "Гаряча клавіша", "Очистити", "Параметри",
        "Відкрити DisplayForge", "Вихід", "Мова", "Системна",
        "Загальні", "Запускати згорнутим у трей", "Сповіщення під час перемикання", "Увімкнути глобальні гарячі клавіші",
        "Зберегти", "Скасувати", "Готово", "Профіль застосовано: {0}",
        "Помилка: {0}", "Профіль збережено: {0}", "Профіль видалено", "Видалити профіль «{0}»?",
        "Підтвердження видалення", "Гарячу клавішу {0} уже використовує інший профіль.", "Не вдалося зареєструвати {0}: {1}", "Клацніть і натисніть сполучення (Ctrl/Alt/Shift/Win + клавіша). Esc очищає.",
        "Назва", "Основний", "Увімк.", "Роздільність",
        "Ширина", "Висота", "Гц", "Оновити",
        "Позиція", "X", "Y", "Орієнтація",
        "Альбомна", "Книжкова", "Альбомна (перевернута)", "Книжкова (перевернута)",
        "Виберіть або створіть профіль.", "Профілів ще немає.\n\nСкористайтеся кнопкою зліва зверху, щоб зберегти поточну конфігурацію моніторів як профіль.", "✓ Активний", "DisplayForge — профілі кількох моніторів",
        "DisplayForge уже запущено.", "Профіль {0}", "Відсутні: {0}", "Закриття ховає в трей. Вихід — з меню трея.",
        "Параметри екрана", "Зберегти ці параметри екрана?", "Скасування через {0} с…", "Зберегти",
        "Скасувати", "Параметри екрана скасовано.", "Не вдалося скасувати: {0}", "Підтвердження змін екрана",
        "Після перемикання показує підтвердження; якщо немає відповіді вчасно, попередню розкладку відновлено автоматично (захист при чорному екрані).", "Підтверджувати після застосування (кнопка / трей)", "Підтверджувати після застосування (гаряча клавіша)", "Час очікування (секунди)",
        "Розташування", "Перетягуйте прямокутники, щоб переміщувати монітори. Краї прилипають до сусідніх.", "Виберіть профіль, щоб розташувати монітори.", "Визначити",
        "Показати великі номери на кожному фізичному моніторі.", "Показ номерів моніторів…");

    d["vi"] = L(
        "DisplayForge", "Lưu bố cục màn hình và chuyển bằng một chạm.", "Hồ sơ", "Hồ sơ đã chọn",
        "Màn hình hiện tại", "Màn hình trong hồ sơ", "Nhấp vào ô để chỉnh sửa giá trị trực tiếp.", "Mới từ cấu hình hiện tại",
        "Lưu cấu hình màn hình hiện tại thành hồ sơ mới.", "Ghi đè bằng hiện tại", "Ghi đè hồ sơ này bằng cấu hình màn hình hiện tại.", "Áp dụng",
        "Chuyển màn hình sang bố cục đã lưu này.", "Nhân bản", "Xóa", "Đổi tên",
        "Nhập tên mới", "Phím tắt", "Xóa", "Cài đặt",
        "Mở DisplayForge", "Thoát", "Ngôn ngữ", "Mặc định hệ thống",
        "Chung", "Khởi động thu nhỏ khay hệ thống", "Hiện thông báo khi chuyển", "Bật phím tắt toàn cục",
        "Lưu", "Hủy", "Sẵn sàng", "Đã áp dụng hồ sơ: {0}",
        "Thất bại: {0}", "Đã lưu hồ sơ: {0}", "Đã xóa hồ sơ", "Xóa hồ sơ \"{0}\"?",
        "Xác nhận xóa", "Phím tắt {0} đã được hồ sơ khác dùng.", "Không thể đăng ký phím tắt {0}: {1}", "Nhấp rồi nhấn phím tắt (Ctrl/Alt/Shift/Win + phím). Esc để xóa.",
        "Tên", "Chính", "Bật", "Độ phân giải",
        "Rộng", "Cao", "Hz", "Làm mới",
        "Vị trí", "X", "Y", "Hướng",
        "Ngang", "Dọc", "Ngang (lật)", "Dọc (lật)",
        "Chọn hoặc tạo hồ sơ.", "Chưa có hồ sơ nào.\n\nDùng nút góc trên bên trái để lưu cấu hình màn hình hiện tại thành hồ sơ.", "✓ Đang dùng", "DisplayForge — hồ sơ đa màn hình",
        "DisplayForge đang chạy.", "Hồ sơ {0}", "Thiếu: {0}", "Đóng sẽ ẩn vào khay. Dùng Thoát trong menu khay để thoát.",
        "Cài đặt hiển thị", "Giữ các cài đặt hiển thị này?", "Hoàn nguyên sau {0} giây…", "Giữ thay đổi",
        "Hoàn nguyên", "Đã hoàn nguyên cài đặt hiển thị.", "Không thể hoàn nguyên: {0}", "Xác nhận thay đổi màn hình",
        "Hiển thị xác nhận sau khi chuyển; nếu không phản hồi kịp, bố cục trước đó được khôi phục tự động (an toàn khi màn hình đen).", "Xác nhận sau khi áp dụng (nút / khay)", "Xác nhận sau khi áp dụng (phím tắt)", "Thời gian chờ (giây)",
        "Sắp xếp", "Kéo hình chữ nhật để di chuyển màn hình. Cạnh sẽ dính vào màn hình lân cận.", "Chọn hồ sơ để sắp xếp màn hình.", "Nhận dạng",
        "Hiển thị số lớn trên mỗi màn hình vật lý.", "Đang hiển thị số màn hình…");

    d["zh-Hans"] = L(
        "DisplayForge", "保存显示器布局与分辨率，一键切换。", "配置方案", "当前所选方案",
        "当前显示器", "方案中的显示器", "单击单元格可直接编辑数值。", "从当前配置新建",
        "将当前显示器配置保存为新方案。", "用当前配置覆盖", "用当前显示器配置覆盖所选方案。", "应用",
        "按已保存的配置切换显示器。", "复制", "删除", "重命名",
        "请输入新名称", "快捷键", "清除", "设置",
        "打开 DisplayForge", "退出", "语言", "跟随系统",
        "常规", "启动时最小化到托盘", "切换时显示通知", "启用全局快捷键",
        "保存", "取消", "就绪", "已应用方案: {0}",
        "失败: {0}", "已保存方案: {0}", "已删除方案", "删除方案“{0}”？",
        "确认删除", "快捷键 {0} 已被其他方案使用。", "无法注册快捷键 {0}: {1}", "点击后按下快捷键（Ctrl/Alt/Shift/Win + 键）。Esc 清除。",
        "名称", "主", "开", "分辨率",
        "宽度", "高度", "Hz", "刷新",
        "位置", "X", "Y", "方向",
        "横向", "纵向", "横向（翻转）", "纵向（翻转）",
        "请选择或创建方案。", "还没有方案。\n\n点击左上角「从当前配置新建」，即可保存当前显示器配置，随时切换使用。", "✓ 使用中", "DisplayForge — 多显示器配置方案",
        "DisplayForge 已在运行。", "方案 {0}", "未连接: {0}", "关闭后将隐藏到托盘。请从托盘菜单选择退出。",
        "显示设置", "保留这些显示设置？", "{0} 秒后还原…", "保留更改",
        "还原", "已还原显示设置。", "还原失败: {0}", "确认显示更改",
        "切换后显示确认界面；若在限定时间内未操作，将自动恢复到先前配置（防止画面无法显示）。", "应用后确认（按钮 / 托盘）", "应用后确认（快捷键）", "超时（秒）",
        "排列", "拖动矩形以移动显示器。边缘会吸附到相邻显示器。", "选择方案以排列显示器。", "识别",
        "在每台物理显示器上显示大号编号。", "正在显示显示器编号…");

    d["zh-Hant"] = L(
        "DisplayForge", "儲存螢幕配置與解析度，一鍵切換。", "設定檔", "目前選取的設定檔",
        "目前的螢幕", "設定檔中的螢幕", "按一下儲存格即可直接編輯數值。", "從目前設定新增",
        "將目前螢幕設定儲存為新設定檔。", "以目前設定覆寫", "以目前螢幕設定覆寫所選設定檔。", "套用",
        "依已儲存的設定切換螢幕。", "複製", "刪除", "重新命名",
        "請輸入新名稱", "快速鍵", "清除", "設定",
        "開啟 DisplayForge", "結束", "語言", "系統預設",
        "一般", "啟動時最小化到系統匣", "切換時顯示通知", "啟用全域快速鍵",
        "儲存", "取消", "就緒", "已套用設定檔: {0}",
        "失敗: {0}", "已儲存設定檔: {0}", "已刪除設定檔", "刪除設定檔「{0}」？",
        "確認刪除", "快速鍵 {0} 已被其他設定檔使用。", "無法註冊快速鍵 {0}: {1}", "按一下後按下快速鍵（Ctrl/Alt/Shift/Win + 鍵）。Esc 清除。",
        "名稱", "主", "開", "解析度",
        "寬度", "高度", "Hz", "重新整理",
        "位置", "X", "Y", "方向",
        "橫向", "直向", "橫向（翻轉）", "直向（翻轉）",
        "請選取或建立設定檔。", "尚無設定檔。\n\n按一下左上角「從目前設定新增」，即可儲存目前螢幕設定，隨時切換使用。", "✓ 使用中", "DisplayForge — 多螢幕設定檔",
        "DisplayForge 已在執行。", "設定檔 {0}", "未連線: {0}", "關閉後會隱藏到系統匣。請從系統匣選單結束。",
        "顯示設定", "保留這些顯示設定？", "{0} 秒後還原…", "保留變更",
        "還原", "已還原顯示設定。", "還原失敗: {0}", "確認顯示變更",
        "切換後顯示確認畫面；若在時限內未操作，會自動還原先前設定（避免畫面無法顯示）。", "套用後確認（按鈕 / 系統匣）", "套用後確認（快速鍵）", "逾時（秒）",
        "排列", "拖曳矩形以移動螢幕。邊緣會貼齊相鄰螢幕。", "選取設定檔以排列螢幕。", "識別",
        "在每台實體螢幕上顯示大型編號。", "正在顯示螢幕編號…");

    return d;
}

static string XmlEscape(string s) =>
    s.Replace("&", "&amp;", StringComparison.Ordinal)
     .Replace("<", "&lt;", StringComparison.Ordinal)
     .Replace(">", "&gt;", StringComparison.Ordinal)
     .Replace("\n", "&#10;", StringComparison.Ordinal);

static Dictionary<string, string> L(params string[] values)
{
    string[] keys =
    [
    "AppName","AppTagline","Profiles","SelectedProfileSection","CurrentMonitors","ProfileMonitors",
    "ProfileMonitorsHint","NewProfile","NewProfileHint","SaveToProfile","SaveToProfileHint","Apply",
    "ApplyHint","Duplicate","Delete","Rename","RenamePrompt","Hotkey",
    "ClearHotkey","Settings","ShowMainWindow","Exit","Language","LanguageAuto",
    "GeneralSection","StartMinimized","ShowNotifications","HotkeysEnabled","Save","Cancel",
    "StatusReady","StatusApplied","StatusFailed","StatusSaved","StatusDeleted","ConfirmDelete",
    "ConfirmDeleteTitle","HotkeyConflict","HotkeyRegisterFailed","HotkeyHint","Name","Primary",
    "Enabled","Resolution","Width","Height","Refresh","RefreshMonitors",
    "Position","PosX","PosY","Orientation","OrientationLandscape","OrientationPortrait",
    "OrientationLandscapeFlipped","OrientationPortraitFlipped","NoProfileSelected","EmptyStateNoProfiles","AppliedBadge","TrayTooltip",
    "AlreadyRunning","NewProfileName","MissingMonitors","CloseToTray","ConfirmKeepSettingsTitle","ConfirmKeepSettingsMessage",
    "ConfirmKeepSettingsCountdown","KeepChanges","RevertChanges","StatusReverted","StatusRevertFailed","ConfirmApplySection",
    "ConfirmApplyHint","ConfirmApplyFromUi","ConfirmApplyFromHotkey","ConfirmApplyTimeoutSeconds","LayoutEditor","LayoutEditorHint",
    "LayoutEditorEmpty","IdentifyMonitors","IdentifyMonitorsHint","StatusIdentifyShown"
    ];
    if (values.Length != keys.Length)
        throw new InvalidOperationException($"Expected {keys.Length} values, got {values.Length}");
    var map = new Dictionary<string, string>(keys.Length);
    for (var i = 0; i < keys.Length; i++)
        map[keys[i]] = values[i];
    return map;
}
