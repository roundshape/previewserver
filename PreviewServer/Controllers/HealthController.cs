using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Reflection;

namespace PreviewServer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HealthController : ControllerBase
    {
        private readonly ILogger<HealthController> _logger;

        public HealthController(ILogger<HealthController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// 基本的なヘルスチェック
        /// </summary>
        /// <returns>サーバーの稼働状況</returns>
        [HttpGet]
        public IActionResult GetHealth()
        {
            try
            {
                var response = new
                {
                    Status = "Healthy",
                    Timestamp = DateTime.UtcNow,
                    Message = "サーバーは正常に稼働しています"
                };

                _logger.LogInformation("ヘルスチェック実行: {Status} at {Timestamp}", response.Status, response.Timestamp);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ヘルスチェック中にエラーが発生しました");
                return StatusCode(500, new { Status = "Unhealthy", Message = "サーバーでエラーが発生しています" });
            }
        }

        /// <summary>
        /// 詳細なヘルスチェック（システム情報含む）
        /// </summary>
        /// <returns>詳細なサーバー情報</returns>
        [HttpGet("detailed")]
        public IActionResult GetDetailedHealth()
        {
            try
            {
                var process = Process.GetCurrentProcess();
                var assembly = Assembly.GetExecutingAssembly();
                
                var response = new
                {
                    Status = "Healthy",
                    Timestamp = DateTime.UtcNow,
                    ServerInfo = new
                    {
                        ApplicationName = assembly.GetName().Name,
                        Version = assembly.GetName().Version?.ToString(),
                        Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown",
                        MachineName = Environment.MachineName,
                        ProcessId = process.Id,
                        StartTime = process.StartTime,
                        WorkingSet = process.WorkingSet64,
                        ThreadCount = process.Threads.Count
                    },
                    Performance = new
                    {
                        UptimeSeconds = (DateTime.UtcNow - process.StartTime).TotalSeconds,
                        MemoryUsageMB = process.WorkingSet64 / (1024 * 1024),
                        CpuTime = process.TotalProcessorTime.TotalMilliseconds
                    }
                };

                _logger.LogInformation("詳細ヘルスチェック実行: PID={ProcessId}, Memory={MemoryMB}MB", 
                    process.Id, response.Performance.MemoryUsageMB);
                
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "詳細ヘルスチェック中にエラーが発生しました");
                return StatusCode(500, new { Status = "Unhealthy", Message = "詳細情報の取得に失敗しました" });
            }
        }

        /// <summary>
        /// 同時リクエスト処理テスト用エンドポイント
        /// </summary>
        /// <param name="delay">遅延時間（ミリ秒）</param>
        /// <returns>処理結果</returns>
        [HttpGet("load-test")]
        public async Task<IActionResult> LoadTest([FromQuery] int delay = 1000)
        {
            var requestId = Guid.NewGuid().ToString("N")[..8];
            var startTime = DateTime.UtcNow;
            
            _logger.LogInformation("負荷テスト開始: RequestId={RequestId}, Delay={Delay}ms", requestId, delay);

            try
            {
                // 指定された時間だけ非同期で待機（同時リクエスト処理をテスト）
                await Task.Delay(delay);
                
                var endTime = DateTime.UtcNow;
                var actualDelay = (endTime - startTime).TotalMilliseconds;

                var response = new
                {
                    RequestId = requestId,
                    Status = "Completed",
                    RequestedDelay = delay,
                    ActualDelay = Math.Round(actualDelay, 2),
                    StartTime = startTime,
                    EndTime = endTime,
                    ThreadId = Thread.CurrentThread.ManagedThreadId
                };

                _logger.LogInformation("負荷テスト完了: RequestId={RequestId}, ActualDelay={ActualDelay}ms, ThreadId={ThreadId}", 
                    requestId, actualDelay, response.ThreadId);

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "負荷テスト中にエラーが発生しました: RequestId={RequestId}", requestId);
                return StatusCode(500, new { RequestId = requestId, Status = "Error", Message = ex.Message });
            }
        }
    }
}

