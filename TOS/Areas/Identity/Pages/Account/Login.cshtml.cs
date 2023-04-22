// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Localization;
using TOS.Data;
using TOS.Models;
using TOS.Resources;
using TOS.Services;

namespace TOS.Areas.Identity.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<LoginModel> _logger;
        private readonly ApplicationDbContext _context;
        private readonly IAuthentication _authentication;
        private readonly IStringLocalizer<SharedResource> _localizer;

        public LoginModel(SignInManager<ApplicationUser> signInManager, ILogger<LoginModel> logger, ApplicationDbContext context, UserManager<ApplicationUser> userManager, IAuthentication authentication, IStringLocalizer<SharedResource> localizer)
        {
            _signInManager = signInManager;
            _logger = logger;
            _context = context;
            _userManager = userManager;
            _authentication = authentication;
            _localizer = localizer;
        }
        
        [BindProperty]
        public InputModel Input { get; set; }
        
        public IList<AuthenticationScheme> ExternalLogins { get; set; }
        
        public string ReturnUrl { get; set; }
        
        [TempData]
        public string ErrorMessage { get; set; }
        
        public class InputModel
        {
            [Required(ErrorMessageResourceType = typeof(ValidationErrorResource), ErrorMessageResourceName = "ERROR_LoginRequired")]
            public string Username { get; set; }
            
            [Required(ErrorMessageResourceType = typeof(ValidationErrorResource), ErrorMessageResourceName = "ERROR_PasswordRequired")]
            [DataType(DataType.Password)]
            public string Password { get; set; }
            
            [DataType(DataType.Password)]
            public string PasswordStag { get; set; }
            
            public bool RememberMe { get; set; }
        }

        public async Task OnGetAsync(string returnUrl = null)
        {
            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                ModelState.AddModelError(string.Empty, ErrorMessage);
            }

            returnUrl ??= Url.Content("~/");

            // Clear the existing external cookie to ensure a clean login process
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            ReturnUrl = returnUrl;
        }

        //For first login user has to use portalID or portalID + upol.cz. For logins after that user can also use full email
        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");

            var result = _authentication.Authenticate(Input.Username, Input.Password, Input.PasswordStag, Input.RememberMe);
            
            switch (await result)
            {
                case UpolAuthenticationResponse.Success:
                    return LocalRedirect(returnUrl);
                case UpolAuthenticationResponse.WrongCredentialsActiveDirectory:
                    ModelState.AddModelError(string.Empty, _localizer["ERROR:WrongCredentialsActiveDirectory"]);
                    if(Input.PasswordStag != null && Input.PasswordStag.Length > 0) ViewData["ShowStagPassword"] = true;
                    return Page();
                case UpolAuthenticationResponse.WrongCredentialsStag:
                    ModelState.AddModelError(string.Empty, _localizer["ERROR:WrongCredentialsStag"]);
                    ViewData["ShowStagPassword"] = true;
                    ViewData["Password"] = Input.Password;
                    return Page();
                case UpolAuthenticationResponse.WrongCredentialsLocal:
                    ModelState.AddModelError(string.Empty, _localizer["ERROR:WrongCredentialsLocal"]);
                    return Page();
            }

            return InvalidLoginAttempt();
        }

        private IActionResult InvalidLoginAttempt()
        {
            ModelState.AddModelError(string.Empty, _localizer["ERROR:InvalidLoginAttempt"]);
            return Page();
        }
    }
}
