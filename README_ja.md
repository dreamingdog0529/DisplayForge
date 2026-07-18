# DisplayForge

[English](./README.md) | [日本語](./README_ja.md)

[![GitHub release](https://img.shields.io/github/v/release/dreamingdog0529/DisplayForge?include_prereleases)](https://github.com/dreamingdog0529/DisplayForge/releases/latest)
[![CI](https://github.com/dreamingdog0529/DisplayForge/actions/workflows/ci.yml/badge.svg)](https://github.com/dreamingdog0529/DisplayForge/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

Windows 向けマルチモニター **プロファイル切替** アプリです。  
NirSoft MultiMonitorTool の「構成の保存・復元」をベースに、**プロファイルごとのグローバルホットキー** と **トレイ常駐** を第一級機能として備えています。

## ダウンロード

**[最新リリースを開く](https://github.com/dreamingdog0529/DisplayForge/releases/latest)**

| 項目 | 内容 |
|------|------|
| 対応 OS | Windows 10 / 11（x64） |
| 配布形式 | MSI インストーラー（自己完結・.NET ランタイム同梱） |
| ファイル名例 | `DisplayForge-1.0.0-win-x64-ja-JP.msi` / `…-en-US.msi` など言語別 |
| 設定の保存先 | `%AppData%\DisplayForge\`（アンインストール後も残ります） |

言語に合う MSI を選び、管理者権限で実行してください。リリース一覧に無い場合はソースから [ビルド](docs/building.md) できます。

## 機能

- 現在のモニター構成をプロファイルとして保存
  - 有効 / 無効、主モニター、解像度、リフレッシュレート、向き、配置
- プロファイルの適用・複製・削除・名前変更
- プロファイルごとにグローバルホットキーを割り当てて即切替
- システムトレイ常駐（右クリックメニューから適用）
- UI の多言語対応（英語・日本語・中国語・韓国語・欧州言語など 30+ 言語、システム言語に自動追従）
- 設定・プロファイルは `%AppData%\DisplayForge\` に JSON で保存

## 使い方

1. アプリを起動（既定ではトレイに格納）
2. トレイアイコンをダブルクリックしてメイン画面を開く
3. **現在の構成から新規** でプロファイルを作成
4. ホットキー欄をクリックし、例: `Ctrl+Alt+1` を押して割り当て
5. Windows のディスプレイ設定で構成を変えたら、別プロファイルを同様に保存
6. ホットキーまたはトレイメニューで切替

通常起動では、ウィンドウを閉じてもトレイに残ります。終了はトレイメニューの **終了** から。

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

MSI をローカル生成:

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

- アプリアイコンは [Lucide](https://lucide.dev/) の `monitor-cog` アイコンに基づきます（[ISC License](https://lucide.dev/license)）。
  - 出典 SVG: `src/DisplayForge/Assets/monitor-cog.svg`
  - ライセンス全文: `src/DisplayForge/Assets/LICENSES/lucide-LICENSE.txt`
