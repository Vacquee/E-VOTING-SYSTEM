using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VotingSystem.Models;
using VotingSystem.Services;

namespace VotingSystem.Controllers
{
    public class AccountController : Controller
    {
        private readonly AuthService _authService;
        private readonly AuditService _auditService;

        public AccountController(AuthService authService, AuditService auditService)
        {
            _authService = authService;
            _auditService = auditService;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Dashboard", "Student");
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _authService.AuthenticateAsync(model.StudentNumber, model.Password);

            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Invalid student number or password.");
                return View(model);
            }

            var principal = await _authService.CreateClaimsPrincipalAsync(user);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal,
                new AuthenticationProperties
                {
                    IsPersistent = model.RememberMe,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddHours(2)
                });

            await _auditService.LogAsync(user.Id!, user.Name, "Login", "User", user.Id!, "User logged in");

            // Redirect based on role
            if (user.Roles.Contains("Admin"))
            {
                return RedirectToAction("Dashboard", "Admin");
            }

            if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            {
                return Redirect(model.ReturnUrl);
            }

            return RedirectToAction("Dashboard", "Student");
        }

        [HttpGet]
        public IActionResult Register()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Dashboard", "Student");
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var success = await _authService.RegisterUserAsync(model);

            if (!success)
            {
                ModelState.AddModelError(string.Empty, "Email or Student Number already exists.");
                return View(model);
            }

            TempData["SuccessMessage"] = "Registration successful! Please login.";
            return RedirectToAction(nameof(Login));
        }

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // In a real application, send password reset email
            // For this school project, we'll just show a success message
            TempData["InfoMessage"] = "If an account with that student number and email exists, a password reset link has been sent.";
            return RedirectToAction(nameof(Login));
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
            var userName = User.Identity?.Name ?? "Unknown";

            await _auditService.LogAsync(userId, userName, "Logout", "User", userId, "User logged out");
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
