using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;

using ThesisNest.Data;
using ThesisNest.Hubs;
using ThesisNest.Models;
using ThesisNest.Services;

var builder = WebApplication.CreateBuilder(args);

// ------------------------
// 1) Configuration & User Secrets
// ------------------------
builder.Configuration.AddUserSecrets<Program>(optional: true);

// ------------------------
// 2) Db Provider (PostgreSQL only)
// ------------------------
var pgConn =
    builder.Configuration.GetConnectionString("DefaultConnection")
    ?? builder.Configuration["ConnectionStrings:DefaultConnection"]
    ?? Environment.GetEnvironmentVariable("CONNECTION_STRING");

if (string.IsNullOrWhiteSpace(pgConn))
    throw new InvalidOperationException(
        "PostgreSQL connection string not found. Set ConnectionStrings:DefaultConnection or CONNECTION_STRING.");

builder.Services.AddDbContext<ApplicationDbContext>(opt => opt.UseNpgsql(pgConn));

// ------------------------
// 3) Identity + Roles
// ------------------------
builder.Services
    .AddDefaultIdentity<ApplicationUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
        options.Password.RequireDigit = false;
        options.Password.RequireLowercase = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequiredLength = 6;
        options.User.RequireUniqueEmail = true;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

// ------------------------
// 4) Cookies
// ------------------------
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Identity/Account/Login";
    options.LogoutPath = "/Identity/Account/Logout";
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.SlidingExpiration = true;
    options.ExpireTimeSpan = TimeSpan.FromDays(14);
});

builder.Services.ConfigureExternalCookie(options =>
{
    options.Cookie.SameSite = SameSiteMode.None;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});

// ------------------------
// 4.1) Forwarded headers (Render/Proxy safe HTTPS)
// ------------------------
builder.Services.Configure<ForwardedHeadersOptions>(o =>
{
    o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    o.KnownNetworks.Clear();
    o.KnownProxies.Clear();
});

// ------------------------
// 5) External Auth (Google + GitHub) — only if keys exist
// ------------------------
bool Has(IConfiguration cfg, string key) => !string.IsNullOrWhiteSpace(cfg[key]);
var cfgAll = builder.Configuration;

var auth = builder.Services.AddAuthentication();

// Google
if (Has(cfgAll, "Authentication:Google:ClientId") &&
    Has(cfgAll, "Authentication:Google:ClientSecret"))
{
    auth.AddGoogle(options =>
    {
        options.SignInScheme = IdentityConstants.ExternalScheme;
        options.ClientId = cfgAll["Authentication:Google:ClientId"]!;
        options.ClientSecret = cfgAll["Authentication:Google:ClientSecret"]!;
        options.CallbackPath = "/signin-google";
        options.SaveTokens = true;
        options.Events.OnRedirectToAuthorizationEndpoint = context =>
        {
            var uri = context.RedirectUri;
            if (!uri.Contains("prompt="))
                uri += "&prompt=select_account";
            context.Response.Redirect(uri);
            return Task.CompletedTask;
        };
    });
}
else
{
    Console.WriteLine("Google OAuth not configured (missing Authentication:Google:*). Skipping.");
}

// GitHub (OAuth)
if (Has(cfgAll, "Authentication:GitHub:ClientId") &&
    Has(cfgAll, "Authentication:GitHub:ClientSecret"))
{
    auth.AddOAuth("GitHub", options =>
    {
        options.SignInScheme = IdentityConstants.ExternalScheme;
        options.ClientId = cfgAll["Authentication:GitHub:ClientId"]!;
        options.ClientSecret = cfgAll["Authentication:GitHub:ClientSecret"]!;
        options.CallbackPath = new PathString("/signin-github");

        options.AuthorizationEndpoint = "https://github.com/login/oauth/authorize";
        options.TokenEndpoint = "https://github.com/login/oauth/access_token";
        options.UserInformationEndpoint = "https://api.github.com/user";

        options.Scope.Add("read:user");
        options.Scope.Add("user:email");
        options.SaveTokens = true;

        options.ClaimActions.MapJsonKey(ClaimTypes.NameIdentifier, "id");
        options.ClaimActions.MapJsonKey(ClaimTypes.Name, "name");
        options.ClaimActions.MapJsonKey(ClaimTypes.Email, "email");

        options.Events = new OAuthEvents
        {
            OnCreatingTicket = async context =>
            {
                using var userReq = new HttpRequestMessage(HttpMethod.Get, options.UserInformationEndpoint);
                userReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", context.AccessToken);
                userReq.Headers.UserAgent.ParseAdd("ThesisNestApp/1.0");

                using var userRes = await context.Backchannel.SendAsync(userReq);
                userRes.EnsureSuccessStatusCode();

                using var userJson = JsonDocument.Parse(await userRes.Content.ReadAsStringAsync());
                context.RunClaimActions(userJson.RootElement);

                if (!context.Identity!.HasClaim(c => c.Type == ClaimTypes.Email))
                {
                    using var emailsReq = new HttpRequestMessage(HttpMethod.Get, "https://api.github.com/user/emails");
                    emailsReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", context.AccessToken);
                    emailsReq.Headers.UserAgent.ParseAdd("ThesisNestApp/1.0");

                    using var emailsRes = await context.Backchannel.SendAsync(emailsReq);
                    emailsRes.EnsureSuccessStatusCode();

                    using var emailsJson = JsonDocument.Parse(await emailsRes.Content.ReadAsStringAsync());
                    foreach (var e in emailsJson.RootElement.EnumerateArray())
                    {
                        var email = e.GetProperty("email").GetString();
                        var primary = e.TryGetProperty("primary", out var p) && p.GetBoolean();
                        var verified = e.TryGetProperty("verified", out var v) && v.GetBoolean();
                        if (primary && verified && !string.IsNullOrEmpty(email))
                        {
                            context.Identity.AddClaim(new Claim(ClaimTypes.Email, email!));
                            break;
                        }
                    }
                }
            }
        };
    });
}
else
{
    Console.WriteLine("GitHub OAuth not configured (missing Authentication:GitHub:*). Skipping.");
}

// ------------------------
// 6) Options (Google Maps, ICE)
// ------------------------
builder.Services.Configure<GoogleMapsOptions>(builder.Configuration.GetSection("GoogleMaps"));
builder.Services.Configure<IceOptions>(builder.Configuration.GetSection("Ice"));

// ------------------------
// 7) MVC + Razor Pages
// ------------------------
builder.Services.AddControllersWithViews(o =>
{
    o.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
});
builder.Services.AddRazorPages();

// ------------------------
// 8) Services & SignalR
// ------------------------
builder.Services.AddScoped<IFileTextExtractor, FileTextExtractor>();
builder.Services.AddSingleton<SimilarityService>();
builder.Services.AddHttpClient();
builder.Services.AddTransient<GoogleSearchService>(sp =>
{
    var http = sp.GetRequiredService<IHttpClientFactory>().CreateClient();
    var cfg = sp.GetRequiredService<IConfiguration>();
    return new GoogleSearchService(http, cfg["GoogleCustomSearch:ApiKey"], cfg["GoogleCustomSearch:Cx"]);
});

builder.Services.AddSignalR();
builder.Services.AddSingleton<BackgroundOpenAIQueue>();
builder.Services.AddHostedService<OpenAIWorker>();

builder.Services.AddHttpClient("Groq", (sp, client) =>
{
    var cfg = sp.GetRequiredService<IConfiguration>();
    var token = cfg["Groq:ApiKey"];
    client.BaseAddress = new Uri("https://api.groq.com/openai/v1/");
    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    if (!string.IsNullOrWhiteSpace(token))
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
});
builder.Services.AddScoped<GroqService>();

// ------------------------
// 9) Email Sender + MemoryCache
// ------------------------
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
builder.Services.AddSingleton<IEmailSender, SmtpEmailSender>();
builder.Services.AddMemoryCache();

// ------------------------
// 10) CORS
// ------------------------
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", p => p
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials()
        .SetIsOriginAllowed(_ => true));
});

// ------------------------
// 11) Build app
// ------------------------
var app = builder.Build();

// ------------------------
// 12) Middleware
// ------------------------
app.UseForwardedHeaders(); // <-- proxy headers first

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
    app.UseMigrationsEndPoint();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseCookiePolicy(new CookiePolicyOptions
{
    MinimumSameSitePolicy = SameSiteMode.Lax,
    Secure = CookieSecurePolicy.Always
});
app.UseRouting();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.UseStatusCodePages();

// ------------------------
// 13) DB Migration & Seed
// ------------------------
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var db = services.GetRequiredService<ApplicationDbContext>();
    await db.Database.MigrateAsync();

    var departments = new[] { "CSE", "EEE", "CV", "ME", "TE", "Arch", "IP" };
    foreach (var deptName in departments)
    {
        if (!await db.Departments.AnyAsync(d => d.Name == deptName))
            db.Departments.Add(new Department { Name = deptName });
    }
    await db.SaveChangesAsync();
    await SeedData.InitializeAsync(services);
}

// ------------------------
// 14) Endpoints
// ------------------------
app.MapControllers();
app.MapHub<CommunicationHub>("/hubs/comm");
app.MapHub<ChatHub>("/chathub");

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

app.MapGet("/rtc/ice", (IOptions<IceOptions> opt) =>
{
    var ice = opt.Value?.IceServers ?? new List<IceServer>();
    return Results.Json(new { iceServers = ice });
});

// ------------------------
// 15) Render PORT binding
// ------------------------
var port = Environment.GetEnvironmentVariable("PORT") ?? "10000";
app.Urls.Add($"http://0.0.0.0:{port}");

app.Run();

// ------------------------
// Option Classes
// ------------------------
public sealed class GoogleMapsOptions { public string? ApiKey { get; set; } }
public sealed class IceOptions { public List<IceServer> IceServers { get; set; } = new(); }
public sealed class IceServer { public List<string> Urls { get; set; } = new(); public string? Username { get; set; } public string? Credential { get; set; } }
