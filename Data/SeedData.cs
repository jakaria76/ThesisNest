using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace ThesisNest.Data
{
    public static class SeedData
    {
        public static async Task InitializeAsync(IServiceProvider services)
        {
            // simple retry: 3 বার চেষ্টা করবে (DB আস্তে উঠে গেলে)
            const int retries = 3;
            var delay = TimeSpan.FromSeconds(2);

            for (var i = 0; i < retries; i++)
            {
                try
                {
                    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

                    string[] roles = { "Student", "Teacher", "Admin" };
                    foreach (var role in roles)
                        if (!await roleManager.RoleExistsAsync(role))
                            await roleManager.CreateAsync(new IdentityRole(role));

                    break; // success ⇒ বেরিয়ে যান
                }
                catch (Exception)
                {
                    if (i == retries - 1) throw; // শেষবারেও ব্যর্থ ⇒ throw
                    await Task.Delay(delay);
                }
            }
        }
    }
}
