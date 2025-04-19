// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using MarketPlaceDomain.Model;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services; // Якщо використовуєш IEmailSender
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace MarketPlaceInfrastructure.Areas.Identity.Pages.Account
{
    [AllowAnonymous] // Дозволяємо анонімний доступ до сторінки логіну
    public class LoginModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager; // Додано UserManager
        private readonly ILogger<LoginModel> _logger;

        // Оновлено конструктор для ін'єкції UserManager
        public LoginModel(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager, // Додано параметр
            ILogger<LoginModel> logger)
        {
            _signInManager = signInManager;
            _userManager = userManager; // Ініціалізовано поле
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        public string ReturnUrl { get; set; }

        [TempData]
        public string ErrorMessage { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Поле Email обов'язкове.")] // Додано повідомлення
            [Display(Name = "Email або Логін")] // Змінено DisplayName
            public string Email { get; set; } // Залишаємо назву Email для простоти, але це може бути і логін

            [Required(ErrorMessage = "Поле Пароль обов'язкове.")]
            [DataType(DataType.Password)]
            [Display(Name = "Пароль")]
            public string Password { get; set; }

            [Display(Name = "Запам'ятати мене?")]
            public bool RememberMe { get; set; }
        }

        public async Task OnGetAsync(string returnUrl = null)
        {
            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                ModelState.AddModelError(string.Empty, ErrorMessage);
            }

            returnUrl ??= Url.Content("~/");

            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            ReturnUrl = returnUrl;
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");

            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            if (ModelState.IsValid)
            {
                // --- ПОЧАТОК ЗМІНЕНОЇ ЛОГІКИ ---

                ApplicationUser user = null;
                string userInput = Input.Email; // Те, що ввів користувач

                // 1. Спочатку спробуємо знайти користувача за тим, що він ввів, як є (якщо ввів короткий UserName)
                user = await _userManager.FindByNameAsync(userInput);

                // 2. Якщо не знайшли за іменем, і введений рядок схожий на email,
                //    спробуємо знайти за повним email АБО за похідним UserName.
                if (user == null && userInput.Contains('@'))
                {
                    // Спробуємо знайти за полем Email
                    _logger.LogInformation("Attempting to find user by email: {Email}", userInput);
                    user = await _userManager.FindByEmailAsync(userInput);

                    // Якщо все ще не знайшли за Email, спробуємо знайти за похідним UserName
                    if (user == null)
                    {
                        var userNameDerived = userInput.Split('@')[0];
                        // Перевіряємо, чи похідне ім'я не порожнє і чи воно відрізняється від повного email
                        if (!string.IsNullOrEmpty(userNameDerived) && userNameDerived != userInput)
                        {
                            _logger.LogInformation("Attempting to find user by derived username: {UserName}", userNameDerived);
                            user = await _userManager.FindByNameAsync(userNameDerived);
                        }
                    }
                }

                // 3. Якщо користувача знайдено (будь-яким способом)
                if (user != null)
                {
                    _logger.LogInformation("User found: {UserName}", user.UserName);

                    // Перевіряємо, чи акаунт не заблоковано
                    if (await _userManager.IsLockedOutAsync(user))
                    {
                        _logger.LogWarning("User account locked out: {UserName}", user.UserName);
                        return RedirectToPage("./Lockout");
                    }

                    // Перевіряємо пароль окремо
                    var passwordCorrect = await _userManager.CheckPasswordAsync(user, Input.Password);

                    if (passwordCorrect)
                    {
                        // --- Перевірка Email Confirmation (якщо потрібно) ---
                        // if (!await _userManager.IsEmailConfirmedAsync(user))
                        // {
                        //      _logger.LogWarning("User login failed - email not confirmed: {UserName}", user.UserName);
                        //      ModelState.AddModelError(string.Empty, "Будь ласка, підтвердіть вашу електронну пошту.");
                        //      return Page();
                        // }
                        // --- Кінець перевірки Email Confirmation ---

                        // Вхід успішний - тепер використовуємо SignInAsync
                        await _signInManager.SignInAsync(user, isPersistent: Input.RememberMe);
                        _logger.LogInformation("User logged in: {UserName}", user.UserName);
                        return LocalRedirect(returnUrl);
                    }
                    else
                    {
                        _logger.LogWarning("Invalid password attempt for user: {UserName}", user.UserName);
                    }
                }
                else
                {
                    _logger.LogWarning("User not found for input: {UserInput}", userInput);
                }

                // Якщо користувача не знайдено або пароль невірний
                ModelState.AddModelError(string.Empty, "Неправильна спроба входу.");
                return Page();

                // --- КІНЕЦЬ ЗМІНЕНОЇ ЛОГІКИ ---
            }

            // Якщо ModelState не валідний, повертаємо сторінку
            return Page();
        }
    }
}