using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using System.Drawing;
using System.Drawing.Imaging;
using PdfiumViewer;

namespace PreviewServer.Services
{
    /// <summary>
    /// ファイルプレビュー生成サービスの実装
    /// </summary>
    public class FilePreviewService : IFilePreviewService
    {
        private readonly ILogger<FilePreviewService> _logger;
        private readonly string _baseFilePath;
        
        // サポートされる画像形式
        private static readonly HashSet<string> SupportedImageExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp"
        };

        public FilePreviewService(ILogger<FilePreviewService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _baseFilePath = configuration.GetValue<string>("FileStorage:BasePath") ?? @"C:\FileStorage";
        }

        public async Task<(byte[] imageData, string contentType)?> GenerateDealPreviewAsync(
            string period, 
            string filename, 
            int width = 300, 
            int height = 300,
            bool keepAspectRatio = false)
        {
            try
            {
                // ファイルパスを構築（期間フォルダ内のdealIDで検索）
                var periodPath = Path.Combine(_baseFilePath, period);
                if (!Directory.Exists(periodPath))
                {
                    _logger.LogWarning("期間フォルダが存在しません: {Period}", period);
                    return await GenerateDefaultIconAsync(width, height);
                }

                // ファイルパスを直接構築して存在確認
                var filePath = Path.Combine(periodPath, filename);
                _logger.LogInformation("構築されたファイルパス: {FilePath}", filePath);
                _logger.LogInformation("期間フォルダ内のファイル一覧: {Files}", string.Join(", ", Directory.GetFiles(periodPath)));
                
                if (!File.Exists(filePath))
                {
                    _logger.LogWarning("ファイルが見つかりません: {Filename} in {Period}", filename, period);
                    return null; // nullを返すことで404エラーになる
                }
                
                // アスペクト比を保持する場合
                if (keepAspectRatio)
                {
                    return await GeneratePreviewWithAspectRatioAsync(filePath, width, height, 1);
                }
                else
                {
                    return await GeneratePreviewFromPathAsync(filePath, width, height, 1);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "プレビュー生成中にエラーが発生しました: Filename={Filename}, Period={Period}", filename, period);
                return await GenerateDefaultIconAsync(width, height);
            }
        }

        public async Task<(byte[] imageData, string contentType)?> GeneratePreviewFromPathAsync(
            string filePath, 
            int width = 300, 
            int height = 300, 
            int page = 1)
        {
            try
            {
                // サイズ制限チェック
                width = Math.Min(width, 1920);
                height = Math.Min(height, 1920);

                if (!File.Exists(filePath))
                {
                    _logger.LogWarning("ファイルが存在しません: {FilePath}", filePath);
                    return await GenerateDefaultIconAsync(width, height);
                }

                var extension = Path.GetExtension(filePath);
                
                // 画像ファイルの場合
                if (SupportedImageExtensions.Contains(extension))
                {
                    return await GenerateImagePreviewAsync(filePath, width, height);
                }
                
                // PDFファイルの場合
                if (extension.Equals(".pdf", StringComparison.OrdinalIgnoreCase))
                {
                    return await GeneratePdfPreviewAsync(filePath, width, height, page);
                }

                // その他のファイル形式の場合はデフォルトアイコンを返す
                _logger.LogInformation("サポートされていないファイル形式: {Extension}", extension);
                return await GenerateDefaultIconAsync(width, height);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "プレビュー生成中にエラーが発生しました: {FilePath}", filePath);
                return await GenerateDefaultIconAsync(width, height);
            }
        }

        /// <summary>
        /// 画像ファイルのプレビューを生成
        /// </summary>
        private async Task<(byte[] imageData, string contentType)> GenerateImagePreviewAsync(
            string filePath, 
            int width, 
            int height)
        {
            using var image = await SixLabors.ImageSharp.Image.LoadAsync(filePath);
            
            // アスペクト比を保持してリサイズ
            image.Mutate(x => x.Resize(new ResizeOptions
            {
                Size = new SixLabors.ImageSharp.Size(width, height),
                Mode = ResizeMode.Max
            }));

            using var memoryStream = new MemoryStream();
            
            // 元の形式に応じて保存
            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            if (extension == ".png" || extension == ".gif")
            {
                await image.SaveAsPngAsync(memoryStream);
                return (memoryStream.ToArray(), "image/png");
            }
            else
            {
                await image.SaveAsJpegAsync(memoryStream, new JpegEncoder { Quality = 85 });
                return (memoryStream.ToArray(), "image/jpeg");
            }
        }

        /// <summary>
        /// PDFファイルのプレビューを生成
        /// </summary>
        private async Task<(byte[] imageData, string contentType)> GeneratePdfPreviewAsync(
            string filePath, 
            int width, 
            int height, 
            int page)
        {
            return await Task.Run(() =>
            {
                try
                {
                    using var document = PdfDocument.Load(filePath);
                    
                    // ページ番号の調整（1ベース → 0ベース）
                    var pageIndex = Math.Max(0, Math.Min(page - 1, document.PageCount - 1));
                    
                    // PDFページを画像として描画
                    using var image = document.Render(pageIndex, 96, 96, false);
                    
                    // System.Drawing.ImageをBitmapにキャスト
                    using var bitmap = new Bitmap(image);
                    
                    // リサイズ処理
                    var resized = ResizeBitmap(bitmap, width, height);
                    
                    using var memoryStream = new MemoryStream();
                    resized.Save(memoryStream, ImageFormat.Jpeg);
                    resized.Dispose();
                    
                    return (memoryStream.ToArray(), "image/jpeg");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "PDF プレビュー生成エラー: {FilePath}", filePath);
                    throw;
                }
            });
        }

        /// <summary>
        /// デフォルトアイコンを生成
        /// </summary>
        private async Task<(byte[] imageData, string contentType)> GenerateDefaultIconAsync(int width, int height)
        {
            return await Task.Run(() =>
            {
                using var bitmap = new Bitmap(width, height);
                using var graphics = Graphics.FromImage(bitmap);
                
                // 背景色を設定
                graphics.Clear(System.Drawing.Color.LightGray);
                
                // ファイルアイコンを描画
                using var brush = new SolidBrush(System.Drawing.Color.DarkGray);
                using var font = new Font("Arial", Math.Min(width, height) / 8);
                
                var text = "📄";
                var textSize = graphics.MeasureString(text, font);
                var x = (width - textSize.Width) / 2;
                var y = (height - textSize.Height) / 2;
                
                graphics.DrawString(text, font, brush, x, y);
                
                using var memoryStream = new MemoryStream();
                bitmap.Save(memoryStream, ImageFormat.Png);
                
                return (memoryStream.ToArray(), "image/png");
            });
        }

        /// <summary>
        /// アスペクト比を保持してプレビューを生成
        /// </summary>
        private async Task<(byte[] imageData, string contentType)?> GeneratePreviewWithAspectRatioAsync(
            string filePath,
            int width,
            int height,
            int page)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    _logger.LogWarning("ファイルが存在しません: {FilePath}", filePath);
                    return await GenerateDefaultIconAsync(width, height);
                }

                var extension = Path.GetExtension(filePath);
                
                // 画像ファイルの場合
                if (SupportedImageExtensions.Contains(extension))
                {
                    using var image = await SixLabors.ImageSharp.Image.LoadAsync(filePath);
                    
                    // 元画像のアスペクト比
                    var originalRatio = (double)image.Width / image.Height;
                    
                    // widthだけ指定されている場合
                    if (width != 300 && height == 300)
                    {
                        height = (int)(width / originalRatio);
                    }
                    // heightだけ指定されている場合
                    else if (height != 300 && width == 300)
                    {
                        width = (int)(height * originalRatio);
                    }
                    
                    // サイズ制限チェック
                    width = Math.Min(width, 1920);
                    height = Math.Min(height, 1920);
                    
                    return await GenerateImagePreviewAsync(filePath, width, height);
                }
                
                // PDFファイルの場合
                if (extension.Equals(".pdf", StringComparison.OrdinalIgnoreCase))
                {
                    return await Task.Run(() =>
                    {
                        using var document = PdfDocument.Load(filePath);
                        var pageIndex = Math.Max(0, Math.Min(page - 1, document.PageCount - 1));
                        using var pdfImage = document.Render(pageIndex, 96, 96, false);
                        using var bitmap = new Bitmap(pdfImage);
                        
                        // 元画像のアスペクト比
                        var originalRatio = (double)bitmap.Width / bitmap.Height;
                        
                        // widthだけ指定されている場合
                        if (width != 300 && height == 300)
                        {
                            height = (int)(width / originalRatio);
                        }
                        // heightだけ指定されている場合
                        else if (height != 300 && width == 300)
                        {
                            width = (int)(height * originalRatio);
                        }
                        
                        // サイズ制限チェック
                        width = Math.Min(width, 1920);
                        height = Math.Min(height, 1920);
                        
                        var resized = ResizeBitmap(bitmap, width, height);
                        
                        using var memoryStream = new MemoryStream();
                        resized.Save(memoryStream, ImageFormat.Jpeg);
                        resized.Dispose();
                        
                        return (memoryStream.ToArray(), "image/jpeg");
                    });
                }

                // その他のファイル形式の場合はデフォルトアイコンを返す
                _logger.LogInformation("サポートされていないファイル形式: {Extension}", extension);
                return await GenerateDefaultIconAsync(width, height);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "アスペクト比保持プレビュー生成中にエラーが発生しました: {FilePath}", filePath);
                return await GenerateDefaultIconAsync(width, height);
            }
        }

        /// <summary>
        /// Bitmapをリサイズ
        /// </summary>
        private static Bitmap ResizeBitmap(Bitmap original, int width, int height)
        {
            // アスペクト比を保持してリサイズ
            var ratioX = (double)width / original.Width;
            var ratioY = (double)height / original.Height;
            var ratio = Math.Min(ratioX, ratioY);

            var newWidth = (int)(original.Width * ratio);
            var newHeight = (int)(original.Height * ratio);

            var resized = new Bitmap(newWidth, newHeight);
            using var graphics = Graphics.FromImage(resized);
            
            graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
            graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            
            graphics.DrawImage(original, 0, 0, newWidth, newHeight);
            
            return resized;
        }

        /// <summary>
        /// 相対パスからプレビュー画像を生成（PREVIEW_BASEPATH下）
        /// </summary>
        public async Task<(byte[] imageData, string contentType)?> GeneratePreviewFromRelativePathAsync(
            string relativePath, 
            int width = 256, 
            int height = 256)
        {
            try
            {
                // 相対パスの正規化（先頭のスラッシュを除去）
                if (relativePath.StartsWith('/') || relativePath.StartsWith('\\'))
                {
                    relativePath = relativePath.Substring(1);
                }

                // ベースパスと結合して絶対パスを作成
                var fullPath = Path.Combine(_baseFilePath, relativePath);
                
                _logger.LogInformation("相対パスからプレビュー生成: RelativePath={RelativePath}, FullPath={FullPath}", 
                    relativePath, fullPath);

                // 既存のGeneratePreviewFromPathAsyncメソッドを呼び出す
                return await GeneratePreviewFromPathAsync(fullPath, width, height);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "相対パスからのプレビュー生成に失敗しました: RelativePath={RelativePath}", relativePath);
                return await GenerateDefaultIconAsync(width, height);
            }
        }
    }
}
