using System.Globalization;
using EnfoqueAutomotriz.UI.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Localization;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(o => o.Limits.MaxRequestBodySize = 524_288_000); // 500 MB

// Cultura mexicana por defecto
var culturaMx = new CultureInfo("es-MX");
CultureInfo.DefaultThreadCurrentCulture = culturaMx;
CultureInfo.DefaultThreadCurrentUICulture = culturaMx;

var razorPagesBuilder = builder.Services.AddRazorPages();
if (builder.Environment.IsDevelopment())
{
    razorPagesBuilder.AddRazorRuntimeCompilation();
}
builder.Services.AddHttpContextAccessor();

builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "RequestVerificationToken";
});

// HttpClient apuntando a EnfoqueAutomotriz.Api
// En desarrollo ignoramos el certificado SSL auto-firmado
builder.Services.AddHttpClient("Api", client =>
{
    var baseUrl = builder.Configuration["ApiSettings:BaseUrl"] ?? "https://localhost:7101";
    client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromMinutes(10);
})
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
});

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath        = "/Login";
        options.LogoutPath       = "/Logout";
        options.AccessDeniedPath = "/AccessDenied";
        options.ExpireTimeSpan   = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly  = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    });

builder.Services.AddAuthorization(options =>
{
    // Roles del taller automotriz:
    // 0=SuperAdmin SaaS, 1=Admin Taller, 2=Mecánico, 3=Recepcionista,
    // 4=Enfermería (reservado/no usado), 5=Facturación/Contador, 6=Soporte IT
    options.AddPolicy("AccesoTotal", policy => policy.RequireRole("0", "1", "6"));
    options.AddPolicy("SoloAdminTaller", policy => policy.RequireRole("1", "6"));
    options.AddPolicy("SoloSoporteIT", policy => policy.RequireRole("6"));
    options.AddPolicy("AdminPermisos", policy => policy.RequireRole("1", "6"));
    options.AddPolicy("Mecanico", policy => policy.RequireRole("1", "2", "6"));
    options.AddPolicy("Recepcionista", policy => policy.RequireRole("1", "3", "6"));
    options.AddPolicy("Facturacion", policy => policy.RequireRole("1", "5", "6"));
    options.AddPolicy("SaasAdmin", policy => policy.RequireRole("0"));
});

builder.Services.AddScoped<IApiService, ApiService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUsuarioService, UsuarioService>();
builder.Services.AddScoped<IEmpresaContextService, EmpresaContextService>();
builder.Services.AddScoped<IFeatureFlagService, FeatureFlagService>();
builder.Services.AddScoped<ITenantInfoService, TenantInfoService>();
builder.Services.AddMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(2);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();

app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture("es-MX"),
    SupportedCultures = [new CultureInfo("es-MX")],
    SupportedUICultures = [new CultureInfo("es-MX")]
});

app.UseAuthentication();
app.UseAuthorization();
app.MapRazorPages();

app.MapGet("/api-uploads/{**path}", async (string path, IHttpClientFactory httpClientFactory) =>
{
    var client = httpClientFactory.CreateClient("Api");
    try
    {
        var response = await client.GetAsync($"uploads/{path}");
        if (!response.IsSuccessStatusCode)
            return Results.NotFound();

        var contentType = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";
        var stream = await response.Content.ReadAsStreamAsync();
        return Results.Stream(stream, contentType, enableRangeProcessing: true);
    }
    catch
    {
        return Results.NotFound();
    }
});

// ── Proxy genérico: reenvía llamadas JS del navegador al API backend ────────
app.Map("/api/{**path}", async (string path, HttpContext ctx, IHttpClientFactory httpClientFactory) =>
{
    ctx.Request.EnableBuffering();

    var client = httpClientFactory.CreateClient("Api");

    var target = $"api/{path}";
    if (ctx.Request.QueryString.HasValue)
        target += ctx.Request.QueryString.Value;

    var request = new HttpRequestMessage(new HttpMethod(ctx.Request.Method), target);

    if (ctx.Request.Headers.TryGetValue("Authorization", out var authHeader))
        request.Headers.TryAddWithoutValidation("Authorization", authHeader.ToString());
    else if (ctx.User.Identity?.IsAuthenticated == true)
    {
        // Navegación directa por <a href> (ej. botón "Imprimir/PDF") no adjunta el header
        // Authorization como lo haría un fetch() de JS; lo tomamos del JWT guardado en la
        // cookie de sesión del usuario.
        var jwt = ctx.User.FindFirst("JWTToken")?.Value;
        if (!string.IsNullOrEmpty(jwt))
            request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {jwt}");
    }

    if (ctx.Request.ContentLength > 0 || ctx.Request.Headers.ContainsKey("Transfer-Encoding"))
    {
        ctx.Request.Body.Position = 0;
        using var ms = new MemoryStream();
        await ctx.Request.Body.CopyToAsync(ms);
        request.Content = new ByteArrayContent(ms.ToArray());

        if (ctx.Request.ContentType != null)
            request.Content.Headers.TryAddWithoutValidation("Content-Type", ctx.Request.ContentType);
    }

    try
    {
        var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
        var contentType = response.Content.Headers.ContentType?.ToString() ?? "application/json";
        var stream = await response.Content.ReadAsStreamAsync();
        ctx.Response.StatusCode = (int)response.StatusCode;
        ctx.Response.ContentType = contentType;

        if (response.Content.Headers.ContentDisposition != null)
            ctx.Response.Headers["Content-Disposition"] = response.Content.Headers.ContentDisposition.ToString();

        await stream.CopyToAsync(ctx.Response.Body);
    }
    catch
    {
        ctx.Response.StatusCode = 502;
        await ctx.Response.WriteAsync("Error al conectar con el servidor API");
    }
});

app.Run();
