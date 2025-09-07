using Microsoft.AspNetCore.Mvc;

namespace PreviewServer.Services
{
    /// <summary>
    /// ファイルプレビュー生成サービスのインターフェース
    /// </summary>
    public interface IFilePreviewService
    {
        /// <summary>
        /// ファイル名からプレビュー画像を生成
        /// </summary>
        /// <param name="period">期間名</param>
        /// <param name="filename">ファイル名</param>
        /// <param name="width">プレビュー画像の幅</param>
        /// <param name="height">プレビュー画像の高さ</param>
        /// <param name="keepAspectRatio">アスペクト比を保持するか（片方のみ指定時）</param>
        /// <returns>画像データとコンテンツタイプ</returns>
        Task<(byte[] imageData, string contentType)?> GenerateDealPreviewAsync(
            string period, 
            string filename, 
            int width = 300, 
            int height = 300,
            bool keepAspectRatio = false);

        /// <summary>
        /// ファイルパスからプレビュー画像を生成
        /// </summary>
        /// <param name="filePath">ファイルパス</param>
        /// <param name="width">プレビュー画像の幅</param>
        /// <param name="height">プレビュー画像の高さ</param>
        /// <param name="page">PDFの場合のページ番号</param>
        /// <returns>画像データとコンテンツタイプ</returns>
        Task<(byte[] imageData, string contentType)?> GeneratePreviewFromPathAsync(
            string filePath, 
            int width = 300, 
            int height = 300, 
            int page = 1);

        /// <summary>
        /// 相対パスからプレビュー画像を生成（PREVIEW_BASEPATH下）
        /// </summary>
        /// <param name="relativePath">相対ファイルパス</param>
        /// <param name="width">プレビュー画像の幅</param>
        /// <param name="height">プレビュー画像の高さ</param>
        /// <returns>画像データとコンテンツタイプ</returns>
        Task<(byte[] imageData, string contentType)?> GeneratePreviewFromRelativePathAsync(
            string relativePath, 
            int width = 256, 
            int height = 256);
    }
}

