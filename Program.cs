// Program.cs
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Net.Http;
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
// 2) Db Provider Selection
// ------------------------
string? sqlServerConn = builder.Configuration.GetConnectionString("DefaultConnection");
bool useSqlServer = !string.IsNullOrWhiteSpace(sqlServerConn);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    if (useSqlServer)
        options.UseSqlServer(sqlServerConn!);
    else
        options.UseSqlite(builder.Configuration.GetConnectionString("SqliteConnection") ?? "Data Source=thesisnest.db");
});

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
// 5) External Auth (Google + GitHub) – optional
// ------------------------
builder.Services
    .AddAuthentication()
    .AddGoogle(options =>
    {
        options.SignInScheme = IdentityConstants.ExternalScheme;
        options.ClientId = builder.Configuration["Authentication:Google:ClientId"];
        options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
        options.SaveTokens = true;
    })
    .AddOAuth("GitHub", options =>
    {
        options.SignInScheme = IdentityConstants.ExternalScheme;
        options.ClientId = builder.Configuration["Authentication:GitHub:ClientId"];
        options.ClientSecret = builder.Configuration["Authentication:GitHub:ClientSecret"];
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

// ------------------------
// 6) Options (Google Maps, ICE)
// ------------------------
builder.Services.Configure<GoogleMapsOptions>(builder.Configuration.GetSection("GoogleMaps"));
builder.Services.Configure<IceOptions>(builder.Configuration.GetSection("Ice"));

// ------------------------
// 7) MVC + Razor Pages (+ Controllers)
// ------------------------
builder.Services.AddControllersWithViews(o =>
{
    o.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
});
builder.Services.AddRazorPages();

// ------------------------
// 8) Misc Services (optional)
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

// ------------------------
// 9) SignalR
// ------------------------
builder.Services.AddSignalR();

// ------------------------
// 10) CORS (wide open dev policy)
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
// 11) Hugging Face HttpClient
// ------------------------
builder.Services.AddHttpClient("HuggingFace", (sp, client) =>
{
    var cfg = sp.GetRequiredService<IConfiguration>();
    var token = cfg["HuggingFace:ApiToken"];
    client.BaseAddress = new Uri("https://api-inference.huggingface.co/");
    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

    if (!string.IsNullOrWhiteSpace(token))
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
});


builder.Services.AddSingleton<BackgroundOpenAIQueue>();
builder.Services.AddHostedService<OpenAIWorker>();

// ------------------------
// 12) Groq HttpClient
// ------------------------
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
// 13) Build app
// ------------------------
var app = builder.Build();

// ------------------------
// 14) Middleware
// ------------------------
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
// 15) Database Migration + Seed
// ------------------------
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var db = services.GetRequiredService<ApplicationDbContext>();

    try
    {
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
    catch (Exception ex)
    {
        var provider = useSqlServer ? "SQL Server" : "SQLite";
        Console.WriteLine($"[DB-INIT-ERROR] Provider={provider}");
        Console.WriteLine($"[DB-INIT-ERROR] DefaultConnection={(sqlServerConn ?? "(null)")} ");
        Console.WriteLine(ex);
        throw;
    }
}

// ------------------------
// 16) Endpoints
// ------------------------
app.MapControllers(); // Controllers (e.g., DiagController)

app.MapHub<ThesisNest.Hubs.CommunicationHub>("/hubs/comm");
app.MapHub<ChatHub>("/chathub");

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

// Optional: ICE config endpoint
app.MapGet("/rtc/ice", (IOptions<IceOptions> opt) =>
{
    var ice = opt.Value?.IceServers ?? new List<IceServer>();
    return Results.Json(new { iceServers = ice });
});

app.Run();

// ------------------------
// Option Classes
// ------------------------
public sealed class GoogleMapsOptions
{
    public string? ApiKey { get; set; }
}

public sealed class IceOptions
{
    public List<IceServer> IceServers { get; set; } = new();
}

public sealed class IceServer
{
    public List<string> Urls { get; set; } = new();
    public string? Username { get; set; }
    public string? Credential { get; set; }
}
