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
        //Teacher must use their external login, TODO: not sure how they look like, but it probably works now..
        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            var success = await _authentication.Authenticate(Input.Username, Input.Password, Input.RememberMe);
            if (success)
            {
                return LocalRedirect(returnUrl);
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
