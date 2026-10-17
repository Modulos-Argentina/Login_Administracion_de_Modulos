var builder = WebApplication.CreateBuilder(args);

// Cargar variables de entorno desde .env o variables del sistema operativo
Login_Administracion_de_Modulos.Seguridad.ConfiguracionSegura.CargarVariablesEntorno(builder);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddDistributedMemoryCache();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(60);
    options.Cookie.Name = ".IESTP.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = Microsoft.AspNetCore.Authentication.Google.GoogleDefaults.AuthenticationScheme;
})
.AddCookie()
.AddGoogle(options =>
{
    var googleAuthNSection = builder.Configuration.GetSection("Authentication:Google");
    string clientId = googleAuthNSection["ClientId"] ?? string.Empty;
    string clientSecret = googleAuthNSection["ClientSecret"] ?? string.Empty;

    if (string.IsNullOrWhiteSpace(clientId) || clientId.Contains("CONFIGURAR", StringComparison.OrdinalIgnoreCase))
    {
        clientId = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_ID") ?? string.Empty;
    }
    if (string.IsNullOrWhiteSpace(clientSecret) || clientSecret.Contains("CONFIGURAR", StringComparison.OrdinalIgnoreCase))
    {
        clientSecret = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_SECRET") ?? string.Empty;
    }

    // Si aún no está configurado, asignar un valor dummy inofensivo para que ASP.NET Core / IIS
    // arranque sin excepciones en entornos donde Google OAuth aún no se haya provisto
    if (string.IsNullOrWhiteSpace(clientId) || clientId.Contains("CONFIGURAR", StringComparison.OrdinalIgnoreCase))
    {
        clientId = "no-configurado.apps.googleusercontent.com";
    }
    if (string.IsNullOrWhiteSpace(clientSecret) || clientSecret.Contains("CONFIGURAR", StringComparison.OrdinalIgnoreCase))
    {
        clientSecret = "no-configurado-secret";
    }

    options.ClientId = clientId;
    options.ClientSecret = clientSecret;
});

var app = builder.Build();

// Inyección de cabeceras de seguridad HTTP
app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-Frame-Options", "SAMEORIGIN");
    context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
    await next();
});

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Login}/{action=Index}/{id?}")
   .WithStaticAssets();

app.Run();
