using Microsoft.AspNetCore.Mvc;
using PreviewServer.Services;

namespace PreviewServer.Controllers
{
    /// <summary>
    /// ファイルプレビューAPI
    /// </summary>
    [ApiController]
    [Route("v1/api")]
    public class FilesController : ControllerBase
    {
        private readonly IFilePreviewService _filePreviewService;
        private readonly ILogger<FilesController> _logger;

        public FilesController(
            IFilePreviewService filePreviewService,
            ILogger<FilesController> logger)
        {
            _filePreviewService = filePreviewService;
            _logger = logger;
        }

        /// <summary>
        /// ファイル名からプレビュー画像を取得
        /// </summary>
        /// <param name="period">期間名（例: 2024-01）</param>
        /// <param name="filename">ファイル名</param>
        /// <param name="width">プレビュー画像の幅（オプション、デフォルト: 300、最大: 1920）</param>
        /// <param name="height">プレビュー画像の高さ（オプション、デフォルト: 300、最大: 1920）</param>
        /// <returns>プレビュー画像</returns>
        [HttpGet("preview")]
        [ProducesResponseType(typeof(FileResult), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetPreview(
            [FromQuery] string period,
            [FromQuery] string filename,
            [FromQuery] int? width = null,
            [FromQuery] int? height = null)
        {
            try
            {
                // パラメータ検証
                if (string.IsNullOrWhiteSpace(period))
                {
                    _logger.LogWarning("期間が指定されていません");
                    return BadRequest(new { message = "期間パラメータが必要です" });
                }

                if (string.IsNullOrWhiteSpace(filename))
                {
                    _logger.LogWarning("ファイル名が指定されていません");
                    return BadRequest(new { message = "ファイル名が必要です" });
                }

                // サイズ制限チェック
                if (width.HasValue && (width <= 0 || width > 1920))
                {
                    _logger.LogWarning("無効な幅が指定されました: Width={Width}", width);
                    return BadRequest(new { message = "幅は1から1920の間で指定してください" });
                }

                if (height.HasValue && (height <= 0 || height > 1920))
                {
                    _logger.LogWarning("無効な高さが指定されました: Height={Height}", height);
                    return BadRequest(new { message = "高さは1から1920の間で指定してください" });
                }

                // 両方とも指定されていない場合はデフォルト値を使用
                var actualWidth = width ?? 300;
                var actualHeight = height ?? 300;

                _logger.LogInformation("プレビュー生成開始: Period={Period}, Filename={Filename}, Width={Width}, Height={Height}, KeepAspectRatio={KeepAspectRatio}",
                    period, filename, width, height, !width.HasValue || !height.HasValue);

                // プレビュー画像を生成
                var result = await _filePreviewService.GenerateDealPreviewAsync(period, filename, actualWidth, actualHeight, !width.HasValue || !height.HasValue);

                if (result == null)
                {
                    _logger.LogWarning("プレビュー画像の生成に失敗しました: Period={Period}, Filename={Filename}", period, filename);
                    return NotFound(new { message = "ファイルが見つからないか、プレビューを生成できませんでした" });
                }

                var (imageData, contentType) = result.Value;

                // キャッシュヘッダーを設定
                Response.Headers.Append("Cache-Control", "max-age=86400"); // 24時間
                Response.Headers.Append("ETag", GenerateETag(period, filename, actualWidth, actualHeight));

                _logger.LogInformation("プレビュー生成完了: Period={Period}, Filename={Filename}, Size={Size}bytes",
                    period, filename, imageData.Length);

                return File(imageData, contentType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "プレビュー取得中にエラーが発生しました: Period={Period}, Filename={Filename}", period, filename);
                return StatusCode(500, new { message = "内部サーバーエラーが発生しました" });
            }
        }

        /// <summary>
        /// ファイルパスからプレビュー画像を取得
        /// </summary>
        /// <param name="path">相対ファイルパス（PREVIEW_BASEPATH下）</param>
        /// <param name="width">プレビュー画像の幅（オプション、デフォルト: 256、最大: 1920）</param>
        /// <param name="height">プレビュー画像の高さ（オプション、デフォルト: 256、最大: 1920）</param>
        /// <returns>プレビュー画像</returns>
        [HttpGet("file/preview")]
        [ProducesResponseType(typeof(FileResult), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetFilePreview(
            [FromQuery] string path,
            [FromQuery] int? width = null,
            [FromQuery] int? height = null)
        {
            try
            {
                // パラメータ検証
                if (string.IsNullOrWhiteSpace(path))
                {
                    _logger.LogWarning("ファイルパスが指定されていません");
                    return BadRequest(new { message = "ファイルパスが必要です" });
                }

                // パストラバーサル攻撃を防ぐ
                if (path.Contains("..") || path.Contains("~"))
                {
                    _logger.LogWarning("不正なパスが指定されました: Path={Path}", path);
                    return BadRequest(new { message = "不正なパスが指定されました" });
                }

                // サイズ制限チェック
                if (width.HasValue && (width <= 0 || width > 1920))
                {
                    _logger.LogWarning("無効な幅が指定されました: Width={Width}", width);
                    return BadRequest(new { message = "幅は1から1920の間で指定してください" });
                }

                if (height.HasValue && (height <= 0 || height > 1920))
                {
                    _logger.LogWarning("無効な高さが指定されました: Height={Height}", height);
                    return BadRequest(new { message = "高さは1から1920の間で指定してください" });
                }

                // デフォルト値を設定
                var actualWidth = width ?? 256;
                var actualHeight = height ?? 256;

                _logger.LogInformation("ファイルプレビュー生成開始: Path={Path}, Width={Width}, Height={Height}",
                    path, actualWidth, actualHeight);

                // プレビュー画像を生成
                var result = await _filePreviewService.GeneratePreviewFromRelativePathAsync(path, actualWidth, actualHeight);

                if (result == null)
                {
                    _logger.LogWarning("プレビュー画像の生成に失敗しました: Path={Path}", path);
                    return NotFound(new { message = "ファイルが見つからないか、プレビューを生成できませんでした" });
                }

                var (imageData, contentType) = result.Value;

                // キャッシュヘッダーを設定
                Response.Headers.Append("Cache-Control", "max-age=86400"); // 24時間
                Response.Headers.Append("ETag", GenerateETag(path, actualWidth, actualHeight));

                _logger.LogInformation("ファイルプレビュー生成完了: Path={Path}, Size={Size}bytes",
                    path, imageData.Length);

                return File(imageData, contentType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ファイルプレビュー取得中にエラーが発生しました: Path={Path}", path);
                return StatusCode(500, new { message = "内部サーバーエラーが発生しました" });
            }
        }

        /// <summary>
        /// ETagを生成
        /// </summary>
        private static string GenerateETag(string period, string filename, int width, int height)
        {
            var input = $"{period}-{filename}-{width}x{height}";
            var hash = System.Security.Cryptography.MD5.HashData(System.Text.Encoding.UTF8.GetBytes(input));
            return Convert.ToHexString(hash).ToLowerInvariant();
        }

        /// <summary>
        /// ETagを生成（パス用）
        /// </summary>
        private static string GenerateETag(string path, int width, int height)
        {
            var input = $"{path}-{width}x{height}";
            var hash = System.Security.Cryptography.MD5.HashData(System.Text.Encoding.UTF8.GetBytes(input));
            return Convert.ToHexString(hash).ToLowerInvariant();
        }
    }
}

