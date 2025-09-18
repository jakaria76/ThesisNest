using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Threading.Tasks;
using ThesisNest.Models;
using ThesisNest.Services;

namespace ThesisNest.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IMemoryCache _cache;
        private readonly IEmailSender _emailSender;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<IdentityRole> roleManager,
            IMemoryCache cache,
            IEmailSender emailSender)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _cache = cache;
            _emailSender = emailSender;
        }

        // -------------------------
        // REGISTER
        // -------------------------
        [AllowAnonymous]
        public IActionResult Register() => View();

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FullName = model.FullName,
                PhoneNumber = model.PhoneNumber,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                if (!await _roleManager.RoleExistsAsync("Student"))
                    await _roleManager.CreateAsync(new IdentityRole("Student"));

                await _userManager.AddToRoleAsync(user, "Student");
                await _signInManager.SignInAsync(user, isPersistent: false);

                return RedirectToAction("Index", "Home");
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Description);

            return View(model);
        }

        // -------------------------
        // LOGIN
        // -------------------------
        [AllowAnonymous]
        public IActionResult Login(string returnUrl = "/")
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = "/")
        {
            if (!ModelState.IsValid) return View(model);

            var result = await _signInManager.PasswordSignInAsync(
                model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);

            if (result.Succeeded)
                return RedirectToAction("Index", "Home");

            ModelState.AddModelError("", "Invalid login attempt.");
            return View(model);
        }

        // -------------------------
        // LOGOUT
        // -------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        // -------------------------
        // EXTERNAL LOGIN (Google, GitHub)
        // -------------------------
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public IActionResult GoogleLogin(string returnUrl = "/") => ExternalLogin("Google", returnUrl);

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public IActionResult GitHubLogin(string returnUrl = "/") => ExternalLogin("GitHub", returnUrl);

        [AllowAnonymous]
        private IActionResult ExternalLogin(string provider, string returnUrl)
        {
            var redirectUrl = Url.Action(nameof(ExternalLoginCallback), "Account", new { ReturnUrl = returnUrl });
            var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
            return Challenge(properties, provider);
        }

        [AllowAnonymous]
        public async Task<IActionResult> ExternalLoginCallback(string returnUrl = "/", string remoteError = null)
        {
            if (remoteError != null)
            {
                TempData["ErrorMessage"] = $"External provider error: {remoteError}";
                return RedirectToAction(nameof(Login));
            }

            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                TempData["ErrorMessage"] = "External login information not found.";
                return RedirectToAction(nameof(Login));
            }

            var signInResult = await _signInManager.ExternalLoginSignInAsync(
                info.LoginProvider, info.ProviderKey, isPersistent: false);

            if (!signInResult.Succeeded)
            {
                var email = info.Principal.FindFirstValue(ClaimTypes.Email);
                var name = info.Principal.FindFirstValue(ClaimTypes.Name) ?? email;

                if (string.IsNullOrEmpty(email))
                {
                    TempData["ErrorMessage"] = "We could not retrieve your email address from the external provider.";
                    return RedirectToAction(nameof(Login));
                }

                var user = await _userManager.FindByEmailAsync(email);
                if (user == null)
                {
                    user = new ApplicationUser
                    {
                        Email = email,
                        UserName = email,
                        FullName = name,
                        EmailConfirmed = true
                    };

                    var createRes = await _userManager.CreateAsync(user);
                    if (!createRes.Succeeded)
                    {
                        TempData["ErrorMessage"] = "Something went wrong while creating your account.";
                        return RedirectToAction(nameof(Login));
                    }

                    if (!await _roleManager.RoleExistsAsync("Student"))
                        await _roleManager.CreateAsync(new IdentityRole("Student"));
                    await _userManager.AddToRoleAsync(user, "Student");
                }

                var addLoginRes = await _userManager.AddLoginAsync(user, info);
                if (!addLoginRes.Succeeded)
                {
                    TempData["ErrorMessage"] = "Failed to link external login.";
                    return RedirectToAction(nameof(Login));
                }
            }

            var finalUser =
                await _userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey)
                ?? await _userManager.FindByEmailAsync(info.Principal.FindFirstValue(ClaimTypes.Email));

            if (finalUser == null)
            {
                TempData["ErrorMessage"] = "Failed to sign in with external provider.";
                return RedirectToAction(nameof(Login));
            }

            await _signInManager.SignInAsync(finalUser, isPersistent: false);
            return RedirectToAction("Index", "Home");
        }

        // -------------------------
        // FORGOT PASSWORD / OTP FLOW
        // -------------------------
        [AllowAnonymous]
        public IActionResult ForgotPassword() => View();

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var email = model.Email?.Trim().ToLowerInvariant();
            var user = await _userManager.FindByEmailAsync(email);
            TempData["Message"] = "If this email exists, an OTP has been sent.";

            if (user == null)
            {
                return RedirectToAction(nameof(ForgotPasswordConfirmation));
            }

            // Generate secure OTP
            var otpInt = RandomNumberGenerator.GetInt32(100000, 1000000);
            var otp = otpInt.ToString();

            _cache.Set($"OTP_{email}", otp, TimeSpan.FromMinutes(5));

            var subject = "ThesisNest — Password Reset OTP";
            var body = $"<p>Hello {user.FullName ?? user.Email},</p>" +
                       $"<p>Your OTP code is: <b>{otp}</b></p>" +
                       "<p>This code will expire in 5 minutes.</p>";

            await _emailSender.SendEmailAsync(email!, subject, body);

            return RedirectToAction(nameof(VerifyOtp), new { email = email });
        }

        [AllowAnonymous]
        public IActionResult ForgotPasswordConfirmation() => View();

        [AllowAnonymous]
        public IActionResult VerifyOtp(string email)
        {
            if (string.IsNullOrEmpty(email)) return RedirectToAction("Login");
            return View(new VerifyOtpViewModel { Email = email });
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public IActionResult VerifyOtp(VerifyOtpViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var email = model.Email?.Trim().ToLowerInvariant();

            if (_cache.TryGetValue($"OTP_{email}", out string cachedOtp) && cachedOtp == model.Otp)
            {
                _cache.Remove($"OTP_{email}");
                _cache.Set($"OTP_VERIFIED_{email}", true, TimeSpan.FromMinutes(10));
                return RedirectToAction(nameof(ResetPasswordOtp), new { email = email });
            }

            ModelState.AddModelError("", "Invalid or expired OTP.");
            return View(model);
        }

        [AllowAnonymous]
        public async Task<IActionResult> ResetPasswordOtp(string email)
        {
            if (string.IsNullOrEmpty(email))
                return RedirectToAction("Login");

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
                return RedirectToAction("Login");

            // Generate token & send it to view
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);

            return View(new ResetPasswordOtpViewModel
            {
                Email = email,
                Token = token
            });
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPasswordOtp(ResetPasswordOtpViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var email = model.Email?.Trim().ToLowerInvariant();

            // OTP verification check
            if (!_cache.TryGetValue($"OTP_VERIFIED_{email}", out bool verified) || !verified)
            {
                ModelState.AddModelError("", "OTP not verified or expired. Please repeat the process.");
                return View(model);
            }

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                TempData["ErrorMessage"] = "Invalid user.";
                return RedirectToAction("Login");
            }

            // Reset password using the token
            var result = await _userManager.ResetPasswordAsync(user, model.Token, model.NewPassword);

            if (result.Succeeded)
            {
                _cache.Remove($"OTP_VERIFIED_{email}"); // clear cache flag
                TempData["SuccessMessage"] = "✅ Your password has been reset successfully. Please login.";
                return RedirectToAction("Login");
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Description);

            return View(model);
        }


    }
}
