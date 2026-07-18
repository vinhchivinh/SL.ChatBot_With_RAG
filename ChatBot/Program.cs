using ChatBot.Data;
using ChatBot.Services;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

// Đảm bảo output tiếng Việt UTF-8 đúng chuẩn (console + response encoding).
Console.OutputEncoding = System.Text.Encoding.UTF8;

// Add services to the container.
builder.Services.AddControllers();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "ERP RAG ChatBot API",
        Version = "v1",
        Description = "RAG pipeline: chunking + embedding local (ONNX) + pgvector + Claude API",
    });

    // POST /api/documents/ingest có 2 action cùng route, khác nhau bởi [Consumes]
    // (multipart/form-data cho file, application/json cho raw text) — routing lúc runtime
    // tự phân biệt đúng theo Content-Type, nhưng OpenAPI 3.0 không hỗ trợ khai báo 2 operation
    // trùng route+method, nên chỉ hiển thị 1 action (multipart) trên Swagger UI để demo;
    // action còn lại (JSON) vẫn hoạt động bình thường, test qua ChatBot.http hoặc curl.
    options.ResolveConflictingActions(apiDescriptions => apiDescriptions.First());
});

// --- Cấu hình strongly-typed từ appsettings.json / user-secrets ---
builder.Services.Configure<EmbeddingOptions>(builder.Configuration.GetSection(EmbeddingOptions.SectionName));
builder.Services.Configure<ClaudeOptions>(builder.Configuration.GetSection(ClaudeOptions.SectionName));

// --- Npgsql + pgvector ---
var connectionString = builder.Configuration.GetConnectionString("Postgres")
    ?? throw new InvalidOperationException("Thiếu ConnectionStrings:Postgres trong appsettings.json / user-secrets.");

var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
dataSourceBuilder.UseVector();
builder.Services.AddSingleton(dataSourceBuilder.Build());

// --- Đăng ký các Service theo Dependency Injection ---
// EmbeddingService nạp model ONNX 1 lần khi khởi động -> đăng ký Singleton để tái sử dụng InferenceSession.
builder.Services.AddSingleton<IEmbeddingService, EmbeddingService>();
builder.Services.AddScoped<IDocumentChunkRepository, DocumentChunkRepository>();
builder.Services.AddScoped<IChunkingService, ChunkingService>();
builder.Services.AddScoped<IClaudeService, ClaudeService>();
builder.Services.AddScoped<IDocumentService, DocumentService>();
builder.Services.AddScoped<IRagService, RagService>();

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
