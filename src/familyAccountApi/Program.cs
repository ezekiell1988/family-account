using FamilyAccountApi.Infrastructure.Extensions;

// ExcelDataReader requiere el proveedor de codificaciones de páginas de código
System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

var builder = WebApplication.CreateBuilder(args);

// ─── Infraestructura (Key Vault + Options + EF Core + Redis + Hangfire) ───────
builder.AddInfrastructure();

// ─── Seguridad (JWT Bearer + Authorization + CORS) ────────────────────────────
builder.Services.AddJwtSecurity(builder.Configuration);
builder.Services.AddCorsPolicy();

// ─── OpenAPI + Scalar ─────────────────────────────────────────────────────────
builder.Services.AddOpenApiDocs();

// ─── Cross-cutting services ───────────────────────────────────────────────────
builder.Services.AddProblemDetails();
builder.Services.AddValidation();

// ─── Módulos de features ──────────────────────────────────────────────────────
builder.Services.AddAllFeaturesModules();

var app = builder.Build();

// ─── Pipeline (orden importa) ─────────────────────────────────────────────────
app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseHttpsRedirection();
app.UseCors("AllowAll");

var spaRoot = Path.GetFullPath(
    Path.Combine(app.Environment.ContentRootPath, "..", "familyAccountWeb", "www"));
app.UseSpa(spaRoot);

app.UseHangfireDashboard();
app.UseAuthentication();
app.UseAuthorization();
app.UseOpenApiDocs();
app.MapAllEndpoints();
app.MapRecurringJobs();

app.Run();

