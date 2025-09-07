
namespace PreviewServer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // 環境変数から設定を取得
            var basePath = Environment.GetEnvironmentVariable("PREVIEW_BASEPATH");
            if (!string.IsNullOrEmpty(basePath))
            {
                builder.Configuration["FileStorage:BasePath"] = basePath;
            }

            var port = Environment.GetEnvironmentVariable("PREVIEW_PORT");
            if (!string.IsNullOrEmpty(port))
            {
                builder.WebHost.UseUrls($"http://*:{port}");
            }

            var mode = Environment.GetEnvironmentVariable("PREVIEW_MODE");
            if (!string.IsNullOrEmpty(mode))
            {
                // "debug" を "Development" にマッピング
                if (mode.Equals("debug", StringComparison.OrdinalIgnoreCase))
                {
                    builder.Environment.EnvironmentName = "Development";
                }
                else
                {
                    builder.Environment.EnvironmentName = mode;
                }
            }

            // Add services to the container.
            builder.Services.AddScoped<PreviewServer.Services.IFilePreviewService, PreviewServer.Services.FilePreviewService>();

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
