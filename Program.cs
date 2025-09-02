using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using System.Text.Json;
using ThesisNest.Data;
using ThesisNest.Models;

var builder = WebApplication.CreateBuilder(args);

// ------------------------
// Add User Secrets
// ------------------------
builder.Configuration.AddUserSecrets<Program>();

// ------------------------
// 1) Connection String
// ------------------------
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// ------------------------
// 2) DbContext
// ------------------------
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// ------------------------
// 3) Identity (Default Identity + Roles)
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
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

// Application cookie paths
builder.Services.ConfigureApplicationCookie(o =>
{
    o.LoginPath = "/Identity/Account/Login";
    o.LogoutPath = "/Identity/Account/Logout";
    o.AccessDeniedPath = "/Identity/Account/AccessDenied";
    o.Cookie.HttpOnly = true;
    o.SlidingExpiration = true;
});

// External cookie SameSite fix
builder.Services.ConfigureExternalCookie(o =>
{
    o.Cookie.SameSite = SameSiteMode.None;
});

// ------------------------
// 4) External Authentication Providers
//    IMPORTANT: do NOT override the default scheme set by Identity
// ------------------------
builder.Services.AddAuthentication()
    .AddGoogle(options =>
    {
        options.SignInScheme = IdentityConstants.ExternalScheme;
        options.ClientId = builder.Configuration["Authentication:Google:ClientId"];
        options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
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
                // Profile
                using var request = new HttpRequestMessage(HttpMethod.Get, options.UserInformationEndpoint);
                request.Headers.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", context.AccessToken);
                request.Headers.Add("X-Restli-Protocol-Version", "2.0.0");

                using var response = await context.Backchannel.SendAsync(request);
                response.EnsureSuccessStatusCode();

                using var user = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                var root = user.RootElement;

                var id = root.GetProperty("id").GetString() ?? "";
                var firstName = root.TryGetProperty("localizedFirstName", out var fn) ? fn.GetString() ?? "" : "";
                var lastName = root.TryGetProperty("localizedLastName", out var ln) ? ln.GetString() ?? "" : "";

                context.Identity!.AddClaim(new Claim(ClaimTypes.NameIdentifier, id));
                context.Identity!.AddClaim(new Claim(ClaimTypes.Name, $"{firstName} {lastName}".Trim()));

                // Email
                using var emailRequest = new HttpRequestMessage(HttpMethod.Get,
                    "https://api.linkedin.com/v2/emailAddress?q=members&projection=(elements*(handle~))");
                emailRequest.Headers.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", context.AccessToken);
                emailRequest.Headers.Add("X-Restli-Protocol-Version", "2.0.0");

                using var emailResponse = await context.Backchannel.SendAsync(emailRequest);
                emailResponse.EnsureSuccessStatusCode();

                using var emailDoc = JsonDocument.Parse(await emailResponse.Content.ReadAsStringAsync());
                var elements = emailDoc.RootElement.GetProperty("elements");
                var email = elements[0].GetProperty("handle~").GetProperty("emailAddress").GetString() ?? "";

                context.Identity!.AddClaim(new Claim(ClaimTypes.Email, email));
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

        options.Scope.Add("user:email");
        options.SaveTokens = true;

        options.ClaimActions.MapJsonKey(ClaimTypes.NameIdentifier, "id");
        options.ClaimActions.MapJsonKey(ClaimTypes.Name, "name");
        options.ClaimActions.MapJsonKey(ClaimTypes.Email, "email");

        options.Events = new OAuthEvents
        {
            OnCreatingTicket = async context =>
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, options.UserInformationEndpoint);
                request.Headers.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", context.AccessToken);
                request.Headers.Add("User-Agent", "ThesisNest-App");

                using var response = await context.Backchannel.SendAsync(request);
                response.EnsureSuccessStatusCode();

                using var root = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

                string? email = null;

                if (root.RootElement.TryGetProperty("email", out var eProp))
                    email = eProp.GetString();

                if (string.IsNullOrEmpty(email))
                {
                    using var emailsRequest = new HttpRequestMessage(HttpMethod.Get, "https://api.github.com/user/emails");
                    emailsRequest.Headers.Authorization =
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", context.AccessToken);
                    emailsRequest.Headers.Add("User-Agent", "ThesisNest-App");

                    using var emailsResponse = await context.Backchannel.SendAsync(emailsRequest);
                    emailsResponse.EnsureSuccessStatusCode();

                    using var emailsJson = JsonDocument.Parse(await emailsResponse.Content.ReadAsStringAsync());
                    foreach (var item in emailsJson.RootElement.EnumerateArray())
                    {
                        if (item.TryGetProperty("primary", out var primary) && primary.GetBoolean())
                        {
                            email = item.GetProperty("email").GetString();
                            break;
                        }
                    }
                    email ??= emailsJson.RootElement[0].GetProperty("email").GetString();
                }

                context.Identity!.AddClaim(new Claim(ClaimTypes.Email, email ?? ""));
                context.Identity!.AddClaim(new Claim(ClaimTypes.NameIdentifier, root.RootElement.GetProperty("id").ToString()));
                context.Identity!.AddClaim(new Claim(ClaimTypes.Name, root.RootElement.TryGetProperty("name", out var n) ? n.GetString() ?? "" : ""));
            }
        };
    });

// ------------------------
// 5) MVC + Razor Pages
// ------------------------
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

var app = builder.Build();

// ------------------------
// 6) Middleware
// ------------------------
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// ------------------------
// 7) Migrate + Seed (Roles, Departments)
// ------------------------
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var db = services.GetRequiredService<ApplicationDbContext>();
    await db.Database.MigrateAsync();

    // Seed Departments
    var departments = new[] { "CSE", "EEE", "CV", "ME", "TE", "Arch", "IP" };
    foreach (var deptName in departments)
    {
        if (!await db.Departments.AnyAsync(d => d.Name == deptName))
            db.Departments.Add(new Department { Name = deptName });
    }
    await db.SaveChangesAsync();

    await SeedData.InitializeAsync(services); // baki seed, roles etc.
}


// ------------------------
// 8) Routes
// ------------------------
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

app.Run();
