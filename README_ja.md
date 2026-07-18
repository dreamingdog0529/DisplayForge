# DisplayForge

[English](./README.md) | 日本語

[![GitHub release](https://img.shields.io/github/v/release/dreamingdog0529/DisplayForge?include_prereleases)](https://github.com/dreamingdog0529/DisplayForge/releases/latest)
[![CI](https://github.com/dreamingdog0529/DisplayForge/actions/workflows/ci.yml/badge.svg)](https://github.com/dreamingdog0529/DisplayForge/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

Windows 向けマルチモニター **プロファイル切替** アプリです。  
NirSoft MultiMonitorTool の「構成の保存・復元」をベースに、**プロファイルごとのグローバルホットキー** と **トレイ常駐** を第一級機能として備えています。

## スクリーンショット

**メインウィンドウ** — プロファイル、ホットキー、レイアウト編集、モニター詳細

![DisplayForge メインウィンドウ](docs/images/main-window.png)

**システムトレイ** — プロファイル適用・アプリ起動・終了をコンテキストメニューから

![DisplayForge システムトレイメニュー](docs/images/tray-menu.png)

## ダウンロード

**[最新リリースを開く](https://github.com/dreamingdog0529/DisplayForge/releases/latest)**

| 項目 | 内容 |
|------|------|
| 対応 OS | Windows 10 / 11（x64） |
| おすすめ | **`…-Setup.exe`**（必要なものをまとめてインストール） |
| その他 | **`….msi`**（アプリ本体のみ。下記参照） |
| ファイル名例 | `DisplayForge-0.1.1-win-x64-ja-JP-Setup.exe` / `…-en-US-Setup.exe` |
| 設定の保存先 | `%AppData%\DisplayForge\`（アンインストール後も残ります） |

### どれをダウンロードすればいい？（Setup.exe と MSI）

リリースには 2 種類のインストーラーがあります。**どちらも同じアプリ** を入れます。違いは「必要なランタイムを自動で入れるかどうか」です。

| ファイル | 向いている人 | 内容 |
|----------|--------------|------|
| **`…-Setup.exe`**（おすすめ） | ほとんどの方 | DisplayForge をインストールします。必要な [.NET 10 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/10.0) が入っていなければ、先に自動で入れます。 |
| **`….msi`** | 詳しい方向け・社内配布など | DisplayForge 本体だけを入れます。Desktop Runtime が既に入っていないとアプリは動きません。`msiexec` での一括導入にも向きます。 |

**迷ったら Setup.exe を選んでください。**

- ファイル名の **ja-JP** / **en-US** は、*インストール画面* の言語（日本語 / 英語）です。アプリ本体の UI はどちらを選んでも [31 言語](#対応言語) に対応しています。
- Windows に管理者権限を求められたら許可して実行してください。
- リリース一覧に無い場合はソースから [ビルド](docs/building.md) できます。

## 機能

- 現在のモニター構成をプロファイルとして保存
  - 有効 / 無効、主モニター、解像度、リフレッシュレート、向き、配置
- プロファイルの適用・複製・削除・名前変更
- プロファイルごとにグローバルホットキーを割り当てて即切替
- システムトレイ常駐（右クリックメニューから適用）
- UI の **31 言語** 対応（既定はシステム言語に追従。**設定** から変更可能）
- 設定・プロファイルは `%AppData%\DisplayForge\` に JSON で保存

## 対応言語

### アプリ UI（31 言語）

既定では Windows の表示言語に合わせます。**設定 → 言語** からいつでも変更できます。いずれのインストーラーにも、以下の UI 言語がすべて含まれます。

> **ご了承ください:** UI 文言（およびインストーラーの多く）は AI による翻訳です。不自然な表現や誤訳がある場合があります。改善のご指摘・ Pull Request は歓迎です。

| コード | 言語 | コード | 言語 |
|--------|------|--------|------|
| `en` | English | `ja` | 日本語 |
| `zh-Hans` | 简体中文 | `zh-Hant` | 繁體中文 |
| `ko` | 한국어 | `de` | Deutsch |
| `fr` | Français | `es` | Español |
| `pt-BR` | Português (Brasil) | `pt-PT` | Português (Portugal) |
| `it` | Italiano | `nl` | Nederlands |
| `pl` | Polski | `ru` | Русский |
| `uk` | Українська | `tr` | Türkçe |
| `cs` | Čeština | `sv` | Svenska |
| `da` | Dansk | `nb` | Norsk bokmål |
| `fi` | Suomi | `hu` | Magyar |
| `ro` | Română | `el` | Ελληνικά |
| `vi` | Tiếng Việt | `th` | ไทย |
| `id` | Bahasa Indonesia | `ms` | Bahasa Melayu |
| `hi` | हिन्दी | `ar` | العربية |
| `he` | עברית | | |

### インストーラー（Setup / MSI ウィザード）

GitHub Releases では現在 **en-US** と **ja-JP** の Setup.exe（と MSI）を配布しています（インストーラーウィザードの表示言語のみの違い）。上記アプリ UI 言語はどちらのパッケージでも同じです。

## 使い方

1. アプリを起動（既定ではメインウィンドウが表示されます）
2. 必要ならトレイにしまい、トレイアイコンのダブルクリックで再表示
3. **現在の構成から新規** でプロファイルを作成
4. ホットキー欄をクリックし、例: `Ctrl+Alt+1` を押して割り当て
5. Windows のディスプレイ設定で構成を変えたら、別プロファイルを同様に保存
6. ホットキーまたはトレイメニューで切替

通常起動では、ウィンドウを閉じてもトレイに残ります。終了はトレイメニューの **終了** から。起動時からトレイのみにしたい場合は **設定 → 起動時はトレイに最小化** を有効にしてください。

## データ配置

| ファイル | 内容 |
|----------|------|
| `%AppData%\DisplayForge\profiles.json` | プロファイル一覧 |
| `%AppData%\DisplayForge\settings.json` | 言語・通知・ホットキー有効など |

## 開発者向け（クイック）

必要環境: Windows 10/11 x64、[.NET 10 SDK](https://dotnet.microsoft.com/download)

```powershell
dotnet build
dotnet test
dotnet run --project src/DisplayForge
```

インストーラー（Setup.exe + MSI）をローカル生成:

```powershell
.\build-msi.ps1
```

詳細なビルド手順・サイレントインストール・CI/リリース: **[docs/building.md](docs/building.md)**  
コントリビュート手順: **[CONTRIBUTING.md](CONTRIBUTING.md)**

### アーキテクチャ概要

```
src/DisplayForge                 WPF UI / トレイ / ホットキー
src/DisplayForge.Core            ディスプレイ API・プロファイル・マッチング
tests/DisplayForge.Core.Tests
installer/DisplayForge.Installer WiX MSI
installer/DisplayForge.Bootstrapper WiX Bundle（Setup.exe）
```

ディスプレイ操作は Windows **CCD** API（`QueryDisplayConfig` / `SetDisplayConfig`）を使用します。  
主モニターは仮想デスクトップ座標の原点 `(0,0)` として扱います。

`dotnet run` で起動した場合は、ウィンドウを閉じるとプロセスも終了します（シェルがブロックされないようにするため）。トレイ常駐のまま試したいときは:

```powershell
dotnet run --project src/DisplayForge -- --tray-on-close
```

逆に、通常起動でも閉じたら終了させたい場合は `--exit-on-close` を付けます。

## 既知の制限

- 拡張デスクトップ（Extend）前提。クローン専用の高度編集は未対応
- モニター未接続時は該当エントリをスキップして部分適用
- Windows 11 の一部環境では `SetDisplayConfig` が不安定なことがあります。失敗時は Windows のディスプレイ設定を一度触ってから再試行してください
- DPI スケーリング / HDR / ウィンドウ位置の復元は今後の拡張候補

## ライセンス

[MIT License](LICENSE)

### サードパーティ

- アイコンは [Lucide](https://lucide.dev/) に基づきます（[ISC License](https://lucide.dev/license)）：
  - アプリ / トレイ表示: `monitor-cog`（出典 SVG は `src/DisplayForge/Assets/lucide/`。配布バイナリは `Assets/app.ico`）
  - トレイメニュー: `monitor`, `check`, `settings`, `app-window`, `log-out`（同フォルダ）
  - ライセンス全文: `src/DisplayForge/Assets/LICENSES/lucide-LICENSE.txt`
