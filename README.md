# PreviewServer

高性能なファイルプレビュー生成サーバー。PDFや画像ファイルからサムネイル画像を動的に生成するASP.NET Core Web APIです。

## 特徴

- 🖼️ 多様なファイル形式に対応（PDF、JPEG、PNG、GIF、BMP、WebP）
- ⚡ 高速なプレビュー生成とキャッシュ機能
- 📏 カスタマイズ可能なサイズ指定
- 🔒 セキュリティ対策済み（パストラバーサル攻撃防止）
- 📊 詳細なヘルスチェック機能
- 🐳 Docker対応

## 前提条件

### 必須要件
- .NET 8.0 SDK
- Windows、Linux、またはmacOS

### 依存ライブラリ
- **ImageSharp** - 画像処理ライブラリ
- **PdfiumViewer** - PDF描画ライブラリ
- **Swashbuckle** - Swagger/OpenAPI ドキュメント生成

## インストール

### 1. リポジトリのクローン
```bash
git clone https://github.com/yourusername/PreviewServer.git
cd PreviewServer
```

### 2. 依存関係のインストール
```bash
cd PreviewServer
dotnet restore
```

### 3. ビルド
```bash
dotnet build
```

## 設定

### 環境変数

| 変数名 | 説明 | デフォルト値 |
|--------|------|------------|
| `PREVIEW_BASEPATH` | ファイルストレージのベースパス | `C:\FileStorage` |
| `PREVIEW_PORT` | サーバーポート番号 | `5000` |
| `PREVIEW_MODE` | 実行モード（`debug`でDevelopmentモード） | `Production` |
| `ASPNETCORE_ENVIRONMENT` | ASP.NET Core環境 | `Production` |

### appsettings.json
```json
{
  "FileStorage": {
    "BasePath": "C:\\FileStorage"
  }
}
```

## 実行方法

### 開発環境での実行
```bash
dotnet run --project PreviewServer
```

### 本番環境での実行
```bash
dotnet publish -c Release -o ./publish
cd publish
dotnet PreviewServer.dll
```

### Dockerでの実行
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5000

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["PreviewServer/PreviewServer.csproj", "PreviewServer/"]
RUN dotnet restore "PreviewServer/PreviewServer.csproj"
COPY . .
WORKDIR "/src/PreviewServer"
RUN dotnet build "PreviewServer.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "PreviewServer.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENV PREVIEW_PORT=5000
ENV PREVIEW_BASEPATH=/data
ENTRYPOINT ["dotnet", "PreviewServer.dll"]
```

```bash
docker build -t preview-server .
docker run -p 5000:5000 -v /path/to/files:/data preview-server
```

## API仕様

### 基本情報
- ベースURL: `http://localhost:5000`
- Swagger UI: `http://localhost:5000/swagger` (Developmentモードのみ)

### エンドポイント一覧

#### 1. 汎用ファイルプレビュー API
ファイルパスを指定してプレビュー画像を生成します。

**エンドポイント:** `GET /v1/api/file/preview`

**パラメータ:**
| パラメータ | 型 | 必須 | 説明 | デフォルト |
|-----------|-----|------|------|-----------|
| `path` | string | ✓ | 相対ファイルパス（PREVIEW_BASEPATH下） | - |
| `width` | int | - | プレビュー画像の幅（1-1920px） | 256 |
| `height` | int | - | プレビュー画像の高さ（1-1920px） | 256 |

**レスポンス:**
- `200 OK`: プレビュー画像（image/jpeg または image/png）
- `400 Bad Request`: パラメータエラー
- `404 Not Found`: ファイルが見つからない
- `500 Internal Server Error`: サーバーエラー

**使用例:**
```bash
curl "http://localhost:5000/v1/api/file/preview?path=/documents/sample.pdf&width=300&height=400"
```

#### 2. 電帳君専用API
「電帳君」アプリケーション専用のプレビュー生成API。期間とファイル名で管理されたファイル構造に対応。

**エンドポイント:** `GET /v1/api/preview`

**パラメータ:**
| パラメータ | 型 | 必須 | 説明 | デフォルト |
|-----------|-----|------|------|-----------|
| `period` | string | ✓ | 期間名（例: 2024-01） | - |
| `filename` | string | ✓ | ファイル名 | - |
| `width` | int | - | プレビュー画像の幅（1-1920px） | 300 |
| `height` | int | - | プレビュー画像の高さ（1-1920px） | 300 |

**レスポンス:**
- `200 OK`: プレビュー画像（image/jpeg または image/png）
- `400 Bad Request`: パラメータエラー
- `404 Not Found`: ファイルが見つからない
- `500 Internal Server Error`: サーバーエラー

**使用例:**
```bash
curl "http://localhost:5000/v1/api/preview?period=2024-01&filename=invoice_001.pdf&width=500"
```

**ファイル構造:**
```
PREVIEW_BASEPATH/
├── 2024-01/
│   ├── invoice_001.pdf
│   └── receipt_002.jpg
└── 2024-02/
    └── document_003.pdf
```

#### 3. ヘルスチェック API

##### 基本ヘルスチェック
**エンドポイント:** `GET /api/health`

**レスポンス例:**
```json
{
  "status": "Healthy",
  "timestamp": "2024-01-15T10:30:00Z",
  "message": "サーバーは正常に稼働しています"
}
```

##### 詳細ヘルスチェック
**エンドポイント:** `GET /api/health/detailed`

**レスポンス例:**
```json
{
  "status": "Healthy",
  "timestamp": "2024-01-15T10:30:00Z",
  "serverInfo": {
    "applicationName": "PreviewServer",
    "version": "1.0.0",
    "environment": "Production",
    "machineName": "SERVER01",
    "processId": 1234,
    "startTime": "2024-01-15T09:00:00Z",
    "workingSet": 134217728,
    "threadCount": 25
  },
  "performance": {
    "uptimeSeconds": 5400,
    "memoryUsageMB": 128,
    "cpuTime": 15000
  }
}
```

##### 負荷テスト
**エンドポイント:** `GET /api/health/load-test`

**パラメータ:**
| パラメータ | 型 | 必須 | 説明 | デフォルト |
|-----------|-----|------|------|-----------|
| `delay` | int | - | 遅延時間（ミリ秒） | 1000 |

#### 4. 疎通確認 API
**エンドポイント:** `GET /ping`

**レスポンス:** `"pong"`

## パフォーマンス最適化

### キャッシュ
- すべてのプレビュー画像は24時間キャッシュされます
- ETagによる条件付きリクエストをサポート

### 画像処理
- ImageSharpによる高速な画像処理
- アスペクト比の自動保持
- 最大サイズ制限（1920x1920px）

### 同時実行
- 非同期処理による高い同時実行性能
- スレッドプールの効率的な利用

## セキュリティ

### パストラバーサル攻撃対策
- 相対パスに`..`や`~`を含むリクエストを拒否
- ベースパス外へのアクセスを防止

### 入力検証
- すべてのパラメータを厳密に検証
- サイズ制限の適用

### エラーハンドリング
- 詳細なエラー情報を隠蔽
- 適切なHTTPステータスコードの返却

## トラブルシューティング

### よくある問題

#### PDFプレビューが生成されない
- PdfiumViewerの依存ライブラリが正しくインストールされているか確認
- PDFファイルが破損していないか確認

#### メモリ使用量が高い
- 画像サイズの制限を確認
- キャッシュ設定を調整

#### ファイルが見つからない
- `PREVIEW_BASEPATH`が正しく設定されているか確認
- ファイルのアクセス権限を確認

## 開発者向け情報

### プロジェクト構造
```
PreviewServer/
├── Controllers/
│   ├── FilesController.cs      # ファイルプレビューAPI
│   ├── HealthController.cs     # ヘルスチェックAPI
│   └── PingController.cs       # 疎通確認API
├── Services/
│   ├── IFilePreviewService.cs  # サービスインターフェース
│   └── FilePreviewService.cs   # プレビュー生成実装
├── Program.cs                  # エントリーポイント
├── appsettings.json            # 設定ファイル
└── PreviewServer.csproj        # プロジェクトファイル
```

### テスト実行
```bash
dotnet test
```

### ビルド設定
```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
</Project>
```

## コントリビューション

プルリクエストを歓迎します！大きな変更の場合は、まずissueを開いて変更内容について議論してください。

1. フォーク
2. フィーチャーブランチを作成 (`git checkout -b feature/AmazingFeature`)
3. 変更をコミット (`git commit -m 'Add some AmazingFeature'`)
4. ブランチにプッシュ (`git push origin feature/AmazingFeature`)
5. プルリクエストを開く

## ライセンス

このプロジェクトはMITライセンスの下で公開されています。詳細は[LICENSE](LICENSE)ファイルを参照してください。

## 作者

- あなたの名前 (@yourusername)

## 謝辞

- ImageSharp開発チーム
- PdfiumViewer開発チーム
- .NETコミュニティ