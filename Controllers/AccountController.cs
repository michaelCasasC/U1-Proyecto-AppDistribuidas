using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Proyecto.Models;
using Proyecto.Services;
using Proyecto.ViewModels;

namespace Proyecto.Controllers
{
    public class AccountController : Controller
    {
        private const string PendingTwoFactorUserIdKey = "PendingTwoFactorUserId";
        private const string PendingTwoFactorReturnUrlKey = "PendingTwoFactorReturnUrl";

        private readonly IUserService _userService;
        private readonly IUserSessionService _userSession;
        private readonly IEmailSender _emailSender;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IAuditService _auditService;
        private readonly IEmailService _emailService;

        public AccountController(
            IUserService userService,
            IUserSessionService userSession,
            IEmailSender emailSender,
            UserManager<ApplicationUser> userManager,
            IAuditService auditService,
            IEmailService emailService)
        {
            _userService = userService;
            _userSession = userSession;
            _emailSender = emailSender;
            _userManager = userManager;
            _auditService = auditService;
            _emailService = emailService;
        }

        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View(new LoginViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userService.AuthenticateAsync(model.Email, model.Password);
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Correo electrónico o contraseña incorrectos.");
                return View(model);
            }

            if (await _userManager.GetTwoFactorEnabledAsync(user))
            {
                SetPendingTwoFactorUser(user.Id, returnUrl);
                return RedirectToAction(nameof(VerifyAuthenticator));
            }

            _userSession.SetCurrentUserId(user.Id);
            await _auditService.LogAsync("Inicio de sesión", "Usuario", user.Id, user.Id);
            return RedirectToLocal(returnUrl);
        }

        public IActionResult Register()
        {
            return View(new RegisterViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (await _userService.EmailExistsAsync(model.Email))
            {
                ModelState.AddModelError(nameof(model.Email), "El correo electrónico ya está en uso.");
                return View(model);
            }

            var user = await _userService.CreateUserAsync(model);
            _userSession.SetCurrentUserId(user.Id);
            await _auditService.LogAsync("Registro", "Usuario", user.Id, user.Id, $"Email: {user.Email}");

            return RedirectToAction("Index", "Home");
        }

        public IActionResult Logout()
        {
            _userSession.Clear();
            return RedirectToAction("Index", "Home");
        }

        // ── Recuperación de contraseña ──────────────────────────────────────────

        public IActionResult ForgotPassword()
        {
            return View(new ForgotPasswordViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null || !user.Activo)
            {
                // No revelar si el usuario existe o no
                TempData["Mensaje"] = "Si el correo está registrado, recibirás instrucciones para restablecer tu contraseña.";
                return RedirectToAction(nameof(ForgotPasswordConfirmation));
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var callbackUrl = Url.Action(
                nameof(ResetPassword),
                "Account",
                new { email = user.Email, token },
                Request.Scheme);

            var cuerpo = $@"
                <div style='font-family: Arial, sans-serif; max-width: 560px; margin: 0 auto;'>
                    <div style='background:#241F1B; padding:24px; border-radius:12px 12px 0 0;'>
                        <h1 style='color:#FBF4E8; margin:0; font-size:1.4rem;'>EmprendeMarket</h1>
                    </div>
                    <div style='background:#fff; padding:32px; border:1px solid #E2D3B4; border-radius:0 0 12px 12px;'>
                        <h2 style='color:#241F1B; margin:0 0 16px; font-size:1.2rem;'>Recuperación de contraseña</h2>
                        <p style='color:#241F1B; margin:0 0 20px;'>Hola {user.Nombre}, recibimos una solicitud para restablecer tu contraseña.</p>
                        <a href='{callbackUrl}' style='display:inline-block; background:#C75D3D; color:#fff; text-decoration:none; padding:12px 28px; border-radius:999px; font-weight:600;'>Restablecer contraseña</a>
                        <p style='color:#7a7164; margin-top:24px; font-size:0.85rem;'>Si no solicitaste esto, ignora este correo.</p>
                    </div>
                </div>";

            await _emailService.SendEmailAsync(
                user.Email!,
                "Recuperación de contraseña - EmprendeMarket",
                cuerpo,
                user.Id);

            await _auditService.LogAsync("Solicitud recuperación contraseña", "Usuario", user.Id, user.Id);
            TempData["Mensaje"] = "Revisa tu correo. Te enviamos un enlace para restablecer tu contraseña.";
            return RedirectToAction(nameof(ForgotPasswordConfirmation));
        }

        public IActionResult ForgotPasswordConfirmation()
        {
            return View();
        }

        public IActionResult ResetPassword(string? email, string? token)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(token))
                return View("Error");

            return View(new ResetPasswordViewModel
            {
                Email = email,
                Token = token
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                TempData["Mensaje"] = "No se pudo restablecer la contraseña. Solicita un nuevo enlace.";
                return RedirectToAction(nameof(ForgotPassword));
            }

            var result = await _userManager.ResetPasswordAsync(user, model.Token, model.Password);
            if (result.Succeeded)
            {
                await _auditService.LogAsync("Contraseña restablecida", "Usuario", user.Id, user.Id);
                TempData["Mensaje"] = "Contraseña restablecida correctamente. Ya puedes iniciar sesión.";
                return RedirectToAction(nameof(Login));
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);

            return View(model);
        }

        // ── Login con Google (OAuth) ────────────────────────────────────────────
        // Desafía a Google. Al volver, Identity invoca /signin-google (callback).
        public IActionResult ExternalLogin(string? returnUrl = null)
        {
            var redirectUrl = Url.Action(nameof(ExternalLoginCallback), "Account", new { returnUrl });
            var props = new AuthenticationProperties { RedirectUri = redirectUrl };
            return Challenge(props, "Google");
        }

        // Callback que ASP.NET Core Identity enruta a /signin-google.
        public async Task<IActionResult> ExternalLoginCallback(string? returnUrl = null, string? remoteError = null)
        {
            if (remoteError != null)
            {
                ModelState.AddModelError(string.Empty, $"Error de Google: {remoteError}");
                return View("Login", new LoginViewModel());
            }

            // 1. Obtener los datos del usuario autenticado por Google.
            var info = await HttpContext.AuthenticateAsync("Google");
            if (info?.Principal == null)
            {
                return RedirectToAction(nameof(Login));
            }

            var email = info.Principal.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(email))
            {
                ModelState.AddModelError(string.Empty, "No se pudo obtener el correo de Google.");
                return View("Login", new LoginViewModel());
            }

            var name = info.Principal.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value
                       ?? info.Principal.FindFirst("name")?.Value
                       ?? email;

            // Nota: Google no soporta SignOut, y como no creamos cookie de
            // Identity (usamos sesión propia), no es necesario cerrar nada aquí.

            // 2. Buscar o crear el usuario en AspNetUsers (sesión propia, sin cookie).
            var (user, fueCreado) = await _userService.FindOrCreateExternalUserAsync(email, name);

            // 3. Requerir verificación por correo si aún no está confirmado
            //    (ya sea recién creado o creado antes sin confirmar).
            if (!user.EmailConfirmed)
            {
                var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                var callbackUrl = Url.Action(
                    "ConfirmEmail", "Account",
                    new { userId = user.Id, token },
                    Request.Scheme);

                var cuerpo = $@"
                    <h2>Bienvenido a EmprendeMarket</h2>
                    <p>Hola {user.Nombre}, gracias por registrarte con Google.</p>
                    <p>Confirma tu correo para poder iniciar sesión:</p>
                    <p><a href='{callbackUrl}'>Confirmar mi correo</a></p>
                    <p>Si no te registraste, ignora este mensaje.</p>";

                await _emailSender.SendEmailAsync(
                    user.Email!, "Confirma tu correo - EmprendeMarket", cuerpo);

                // No iniciamos sesión hasta que confirme.
                TempData["Mensaje"] = "Te enviamos un correo de verificación. " +
                                        "Confírmalo para poder iniciar sesión.";
                return RedirectToAction("Login");
            }

            if (await _userManager.GetTwoFactorEnabledAsync(user))
            {
                SetPendingTwoFactorUser(user.Id, returnUrl);
                return RedirectToAction(nameof(VerifyAuthenticator));
            }

            // 4. Usuario ya verificado: iniciar sesión con sesión propia.
            _userSession.SetCurrentUserId(user.Id);

            return RedirectToLocal(returnUrl);
        }

        // GET: /Account/ConfirmEmail?userId=..&token=..
        public async Task<IActionResult> ConfirmEmail(int? userId, string? token)
        {
            if (userId == null || token == null)
                return View("Error");

            var user = await _userManager.FindByIdAsync(userId.Value.ToString());
            if (user == null)
                return View("Error");

            var result = await _userManager.ConfirmEmailAsync(user, token);
            if (result.Succeeded)
            {
                // Tras confirmar, iniciamos sesión con la sesión propia.
                _userSession.SetCurrentUserId(user.Id);
                ViewData["Mensaje"] = "Correo verificado correctamente. Ya puedes usar la plataforma.";
                return View("ConfirmEmail");
            }

            return View("Error");
        }

        public async Task<IActionResult> Security()
        {
            var user = await GetLoggedInUserAsync();
            if (user == null)
            {
                return RedirectToAction(nameof(Login), new { returnUrl = $"{Request.Path}{Request.QueryString}" });
            }

            ViewData["TwoFactorEnabled"] = await _userManager.GetTwoFactorEnabledAsync(user);
            return View(user);
        }

        public async Task<IActionResult> EnableAuthenticator()
        {
            var user = await GetLoggedInUserAsync();
            if (user == null)
            {
                return RedirectToAction(nameof(Login), new { returnUrl = $"{Request.Path}{Request.QueryString}" });
            }

            var model = await BuildEnableAuthenticatorViewModelAsync(user);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EnableAuthenticator(EnableAuthenticatorViewModel model)
        {
            var user = await GetLoggedInUserAsync();
            if (user == null)
            {
                return RedirectToAction(nameof(Login), new { returnUrl = $"{Request.Path}{Request.QueryString}" });
            }

            if (!ModelState.IsValid)
            {
                var invalidModel = await BuildEnableAuthenticatorViewModelAsync(user);
                invalidModel.Code = model.Code;
                return View(invalidModel);
            }

            var verificationCode = NormalizeAuthenticatorCode(model.Code);
            var isValid = await _userManager.VerifyTwoFactorTokenAsync(
                user,
                TokenOptions.DefaultAuthenticatorProvider,
                verificationCode);

            if (!isValid)
            {
                ModelState.AddModelError(nameof(model.Code), "El código no es válido.");
                var invalidModel = await BuildEnableAuthenticatorViewModelAsync(user);
                invalidModel.Code = model.Code;
                return View(invalidModel);
            }

            await _userManager.SetTwoFactorEnabledAsync(user, true);
            TempData["Mensaje"] = "La autenticación de doble factor quedó activada.";
            return RedirectToAction(nameof(Security));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DisableAuthenticator()
        {
            var user = await GetLoggedInUserAsync();
            if (user == null)
            {
                return RedirectToAction(nameof(Login), new { returnUrl = $"{Request.Path}{Request.QueryString}" });
            }

            await _userManager.SetTwoFactorEnabledAsync(user, false);
            await _userManager.ResetAuthenticatorKeyAsync(user);
            TempData["Mensaje"] = "La autenticación de doble factor quedó desactivada.";
            return RedirectToAction(nameof(Security));
        }

        public IActionResult VerifyAuthenticator(string? returnUrl = null)
        {
            var pendingUserId = HttpContext.Session.GetInt32(PendingTwoFactorUserIdKey);
            if (!pendingUserId.HasValue)
            {
                return RedirectToAction(nameof(Login));
            }

            return View(new VerifyAuthenticatorViewModel
            {
                ReturnUrl = returnUrl ?? HttpContext.Session.GetString(PendingTwoFactorReturnUrlKey)
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyAuthenticator(VerifyAuthenticatorViewModel model)
        {
            var pendingUserId = HttpContext.Session.GetInt32(PendingTwoFactorUserIdKey);
            if (!pendingUserId.HasValue)
            {
                return RedirectToAction(nameof(Login));
            }

            var user = await _userManager.FindByIdAsync(pendingUserId.Value.ToString());
            if (user == null || !user.Activo || !await _userManager.GetTwoFactorEnabledAsync(user))
            {
                ClearPendingTwoFactorUser();
                return RedirectToAction(nameof(Login));
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var verificationCode = NormalizeAuthenticatorCode(model.Code);
            var isValid = await _userManager.VerifyTwoFactorTokenAsync(
                user,
                TokenOptions.DefaultAuthenticatorProvider,
                verificationCode);

            if (!isValid)
            {
                ModelState.AddModelError(nameof(model.Code), "El código no es válido.");
                return View(model);
            }

            ClearPendingTwoFactorUser();
            _userSession.SetCurrentUserId(user.Id);
            return RedirectToLocal(model.ReturnUrl);
        }

        private IActionResult RedirectToLocal(string? returnUrl)
        {
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("Index", "Home");
        }

        private async Task<ApplicationUser?> GetLoggedInUserAsync()
        {
            var userId = _userSession.GetCurrentUserId();
            if (!userId.HasValue)
            {
                return null;
            }

            return await _userManager.FindByIdAsync(userId.Value.ToString());
        }

        private async Task<EnableAuthenticatorViewModel> BuildEnableAuthenticatorViewModelAsync(ApplicationUser user)
        {
            var unformattedKey = await _userManager.GetAuthenticatorKeyAsync(user);
            if (string.IsNullOrEmpty(unformattedKey))
            {
                await _userManager.ResetAuthenticatorKeyAsync(user);
                unformattedKey = await _userManager.GetAuthenticatorKeyAsync(user);
            }

            return new EnableAuthenticatorViewModel
            {
                SharedKey = FormatAuthenticatorKey(unformattedKey ?? string.Empty),
                AuthenticatorUri = GenerateAuthenticatorUri(user.Email ?? user.UserName ?? user.Id.ToString(), unformattedKey ?? string.Empty)
            };
        }

        private void SetPendingTwoFactorUser(int userId, string? returnUrl)
        {
            HttpContext.Session.SetInt32(PendingTwoFactorUserIdKey, userId);
            if (!string.IsNullOrEmpty(returnUrl))
            {
                HttpContext.Session.SetString(PendingTwoFactorReturnUrlKey, returnUrl);
            }
            else
            {
                HttpContext.Session.Remove(PendingTwoFactorReturnUrlKey);
            }
        }

        private void ClearPendingTwoFactorUser()
        {
            HttpContext.Session.Remove(PendingTwoFactorUserIdKey);
            HttpContext.Session.Remove(PendingTwoFactorReturnUrlKey);
        }

        private static string NormalizeAuthenticatorCode(string code)
        {
            return code.Replace(" ", string.Empty).Replace("-", string.Empty);
        }

        private static string FormatAuthenticatorKey(string unformattedKey)
        {
            return string.Join(" ", Enumerable.Range(0, (int)Math.Ceiling(unformattedKey.Length / 4.0))
                .Select(index => unformattedKey.Substring(index * 4, Math.Min(4, unformattedKey.Length - index * 4))))
                .ToLowerInvariant();
        }

        private static string GenerateAuthenticatorUri(string email, string unformattedKey)
        {
            const string issuer = "EmprendeMarket";
            return string.Format(
                "otpauth://totp/{0}:{1}?secret={2}&issuer={0}&digits=6",
                Uri.EscapeDataString(issuer),
                Uri.EscapeDataString(email),
                unformattedKey);
        }
    }
}
