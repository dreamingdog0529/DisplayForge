# Regenerates tools/GenLocales/Program.cs from current Strings.*.resx files
# (ja is source of key order; all cultures under Resources are included).
# Run: pwsh -File tools/GenLocales/sync-from-resx.ps1

$ErrorActionPreference = "Stop"
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$resDir = Join-Path $repoRoot "src\DisplayForge\Resources"
$outPath = Join-Path $PSScriptRoot "Program.cs"

function Get-ResxMap([string]$path) {
    $map = [ordered]@{}
    $doc = New-Object System.Xml.XmlDocument
    $doc.Load($path)
    foreach ($d in $doc.root.data) {
        $map[$d.name] = [string]$d.value
    }
    return $map
}

function Escape-Cs([string]$s) {
    return $s.Replace('\', '\\').Replace('"', '\"').Replace("`r`n", '\n').Replace("`n", '\n').Replace("`r", '\n')
}

$jaMap = Get-ResxMap (Join-Path $resDir "Strings.ja.resx")
$keys = @($jaMap.Keys)

$cultures = Get-ChildItem (Join-Path $resDir "Strings.*.resx") |
    ForEach-Object { if ($_.Name -match '^Strings\.(.+)\.resx$') { $Matches[1] } } |
    Sort-Object { if ($_ -eq 'ja') { '0' } else { $_ } }

# Prefer ja first, then alphabetical for the rest
$cultures = @('ja') + ($cultures | Where-Object { $_ -ne 'ja' } | Sort-Object)

$keysLiteral = ($keys | ForEach-Object { "`"$_`"" }) -join ","
# Format keys array in readable rows of ~6
$keyLines = New-Object System.Collections.Generic.List[string]
$buf = New-Object System.Collections.Generic.List[string]
foreach ($k in $keys) {
    $buf.Add("`"$k`"")
    if ($buf.Count -ge 6) {
        $keyLines.Add("    " + ($buf -join ",") + ",")
        $buf.Clear()
    }
}
if ($buf.Count -gt 0) {
    $keyLines.Add("    " + ($buf -join ",") )
}

$sb = New-Object System.Text.StringBuilder
[void]$sb.AppendLine(@'
using System.Text;

// Generates Strings.<culture>.resx under src/DisplayForge/Resources
// Run: dotnet run --project tools/GenLocales
// Sync from resx: pwsh -File tools/GenLocales/sync-from-resx.ps1

var repoRoot = FindRepoRoot();
var outDir = Path.Combine(repoRoot, "src", "DisplayForge", "Resources");
Directory.CreateDirectory(outDir);

string[] keys =
[
'@)
foreach ($line in $keyLines) {
    [void]$sb.AppendLine($line)
}
[void]$sb.AppendLine(@'
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
'@)

foreach ($culture in $cultures) {
    $map = Get-ResxMap (Join-Path $resDir "Strings.$culture.resx")
    [void]$sb.AppendLine()
    [void]$sb.AppendLine("    d[`"$culture`"] = L(")
    $values = foreach ($k in $keys) {
        if (-not $map.Contains($k)) { throw "Culture $culture missing $k" }
        Escape-Cs $map[$k]
    }
    for ($i = 0; $i -lt $values.Count; $i += 4) {
        $chunk = @()
        for ($j = $i; $j -lt [Math]::Min($i + 4, $values.Count); $j++) {
            $chunk += "`"$($values[$j])`""
        }
        $line = "        " + ($chunk -join ", ")
        if ($i + 4 -ge $values.Count) {
            $line += ");"
        } else {
            $line += ","
        }
        [void]$sb.AppendLine($line)
    }
}

# Build L() helper with same keys
[void]$sb.AppendLine(@'

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
'@)
foreach ($line in $keyLines) {
    [void]$sb.AppendLine($line)
}
[void]$sb.AppendLine(@'
    ];
    if (values.Length != keys.Length)
        throw new InvalidOperationException($"Expected {keys.Length} values, got {values.Length}");
    var map = new Dictionary<string, string>(keys.Length);
    for (var i = 0; i < keys.Length; i++)
        map[keys[i]] = values[i];
    return map;
}
'@)

$utf8NoBom = New-Object System.Text.UTF8Encoding $false
[System.IO.File]::WriteAllText($outPath, $sb.ToString(), $utf8NoBom)
Write-Host "Wrote $outPath ($($keys.Count) keys, $($cultures.Count) locales)"
