using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using DepartmentLibrary.Models;
using DepartmentLibrary.Services;

namespace DepartmentLibrary.Controllers
{
    public class AuthMvcController : Controller
    {
        private readonly AuthService _authService;
        private readonly ILogger<AuthMvcController> _logger;

        public AuthMvcController(AuthService authService, ILogger<AuthMvcController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        // GET Login
        [HttpGet]
        public IActionResult Login()
        {
            return View(); // Працює, якщо view знаходиться у Views/AuthMvc/Login.cshtml
        }

        /// <summary>
        /// Triggers on login form submit
        /// checks credentials if valid appends jwt token in cookie
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> Login(LoginModel dto)
        {
            if (!ModelState.IsValid)
                return View(dto);

            try
            {
                var token = await _authService.LoginAsync(dto.Email, dto.Password);

                Response.Cookies.Append("jwt", token, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict
                });

                _logger.LogInformation("User {Email} logged in at {Time}.", dto.Email, DateTime.UtcNow);

                return RedirectToAction("Index", "Home");
            }
            catch (UnauthorizedAccessException)
            {
                _logger.LogWarning("Failed login attempt for {Email} at {Time}.", dto.Email, DateTime.UtcNow);
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                return View(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during login for {Email}.", dto.Email);
                ModelState.AddModelError(string.Empty, $"An error occurred: {ex.Message}");
                return View(dto);
            }
        }

        // GET Register (Admin Only)
        [Authorize(Roles = "admin")]
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        /// <summary>
        /// Triggers when register form is submited
        /// if everything is correct creates new user
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        [Authorize(Roles = "admin")]
        [HttpPost]
        public async Task<IActionResult> Register(RegisterModel dto)
        {
            if (!ModelState.IsValid)
                return View(dto);

            try
            {
                await _authService.RegisterAsync(dto.Email, dto.Password, dto.Role, User.Identity?.Name);
                _logger.LogInformation("Admin registered a new user {Email} with role {Role} at {Time}.", dto.Email, dto.Role, DateTime.UtcNow);
                return RedirectToAction("Login"); // later can be home page idk
            }
            catch (UnauthorizedAccessException)
            {
                _logger.LogWarning("Unauthorized registration attempt by user {Email} at {Time}.", User.Identity?.Name, DateTime.UtcNow);
                ModelState.AddModelError(string.Empty, "Only admins can register users.");
                return View(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during registration for {Email}.", dto.Email);
                ModelState.AddModelError(string.Empty, $"An error occurred: {ex.Message}");
                return View(dto);
            }
        }

        /// <summary>
        /// Delets jwt token and redirects to login
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public IActionResult Logout()
        {
            System.Diagnostics.Debug.WriteLine("Logout");
            var email = User.Identity?.Name ?? "Unknown";
            Response.Cookies.Delete("jwt");

            _logger.LogInformation("User {Email} logged out at {Time}.", email, DateTime.UtcNow);

            return RedirectToAction("Login");
        }
    }
}
