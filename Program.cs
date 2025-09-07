using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;

using ThesisNest.Data;
using ThesisNest.Models;
using ThesisNest.Services; // <-- Plagiarism services

var builder = WebApplication.CreateBuilder(args);

// ------------------------
// 1) Configuration & User Secrets
// ------------------------
builder.Configuration.AddUserSecrets<Program>(optional: true);

// ------------------------
// 2) Connection String & DbContext
// ------------------------
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
// যদি SQLite ব্যবহার করতে চাও, উপরকার লাইন বদলে এটি দাও:
// builder.Services.AddDbContext<ApplicationDbContext>(options =>
//     options.UseSqlite(connectionString));

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
// 5) Authentication Providers
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
    .AddOAuth("LinkedIn", options =>
    {
        options.SignInScheme = IdentityConstants.ExternalScheme;
        options.ClientId = builder.Configuration["Authentication:LinkedIn:ClientId"];
        options.ClientSecret = builder.Configuration["Authentication:LinkedIn:ClientSecret"];
        options.CallbackPath = new PathString("/signin-linkedin");

        options.AuthorizationEndpoint = "https://www.linkedin.com/oauth/v2/authorization";
        options.TokenEndpoint = "https://www.linkedin.com/oauth/v2/accessToken";
        options.UserInformationEndpoint = "https://api.linkedin.com/v2/me";

        options.Scope.Add("r_liteprofile");
        options.Scope.Add("r_emailaddress");
        options.SaveTokens = true;

        options.Events = new OAuthEvents
        {
            OnCreatingTicket = async context =>
            {
                using var profileReq = new HttpRequestMessage(HttpMethod.Get, options.UserInformationEndpoint);
                profileReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", context.AccessToken);
                profileReq.Headers.Add("X-Restli-Protocol-Version", "2.0.0");

                using var profileRes = await context.Backchannel.SendAsync(profileReq);
                profileRes.EnsureSuccessStatusCode();

                using var profileJson = JsonDocument.Parse(await profileRes.Content.ReadAsStringAsync());
                var root = profileJson.RootElement;

                var id = root.GetProperty("id").GetString() ?? string.Empty;
                var firstName = root.TryGetProperty("localizedFirstName", out var fn) ? fn.GetString() ?? "" : "";
                var lastName = root.TryGetProperty("localizedLastName", out var ln) ? ln.GetString() ?? "" : "";

                context.Identity!.AddClaim(new Claim(ClaimTypes.NameIdentifier, id));
                context.Identity!.AddClaim(new Claim(ClaimTypes.Name, $"{firstName} {lastName}".Trim()));

                using var emailReq = new HttpRequestMessage(
                    HttpMethod.Get,
                    "https://api.linkedin.com/v2/emailAddress?q=members&projection=(elements*(handle~))"
                );
                emailReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", context.AccessToken);
                emailReq.Headers.Add("X-Restli-Protocol-Version", "2.0.0");

                using var emailRes = await context.Backchannel.SendAsync(emailReq);
                emailRes.EnsureSuccessStatusCode();

                using var emailJson = JsonDocument.Parse(await emailRes.Content.ReadAsStringAsync());
                var elements = emailJson.RootElement.GetProperty("elements");
                if (elements.GetArrayLength() > 0 &&
                    elements[0].TryGetProperty("handle~", out var handle) &&
                    handle.TryGetProperty("emailAddress", out var emailProp))
                {
                    var email = emailProp.GetString();
                    if (!string.IsNullOrWhiteSpace(email))
                        context.Identity!.AddClaim(new Claim(ClaimTypes.Email, email!));
                }
            }
        };
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

                var hasEmail = context.Identity!.HasClaim(c => c.Type == ClaimTypes.Email);
                if (!hasEmail)
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
// 6) Google Maps Options
// ------------------------
builder.Services.Configure<GoogleMapsOptions>(
    builder.Configuration.GetSection("GoogleMaps"));

// ------------------------
// 7) MVC + Razor Pages
// ------------------------
builder.Services.AddControllersWithViews(options =>
{
    options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
});
builder.Services.AddRazorPages();

// ------------------------
// 8) Plagiarism Checker Services (NEW)
// ------------------------
builder.Services.AddScoped<IFileTextExtractor, FileTextExtractor>();
builder.Services.AddSingleton<SimilarityService>();

builder.Services.AddHttpClient();
builder.Services.AddTransient<GoogleSearchService>(sp =>
{
    var http = sp.GetRequiredService<IHttpClientFactory>().CreateClient();
    var cfg = sp.GetRequiredService<IConfiguration>();
    var apiKey = cfg["GoogleCustomSearch:ApiKey"];
    var cx = cfg["GoogleCustomSearch:Cx"];
    return new GoogleSearchService(http, apiKey, cx);
});

var app = builder.Build();

// ------------------------
// 9) Middleware
// ------------------------
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseCookiePolicy(new CookiePolicyOptions
{
    MinimumSameSitePolicy = SameSiteMode.Lax,
    Secure = CookieSecurePolicy.Always
});

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseStatusCodePages();

// ------------------------
// 10) Database Migration + Seed
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
// 11) Routes
// ------------------------
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

app.Run();

// ------------------------
// Google Maps Options Class
// ------------------------
public sealed class GoogleMapsOptions
{
    public string? ApiKey { get; set; }
}
