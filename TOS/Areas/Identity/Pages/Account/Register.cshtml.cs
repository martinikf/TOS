// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Localization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using TOS.Models;
using TOS.Resources;

namespace TOS.Areas.Identity.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUserStore<ApplicationUser> _userStore;
        private readonly IUserEmailStore<ApplicationUser> _emailStore;
        private readonly IEmailSender _emailSender;
        private readonly IHtmlLocalizer<SharedResource> _sharedLocalizer;

        public RegisterModel(
            UserManager<ApplicationUser> userManager,
            IUserStore<ApplicationUser> userStore,
            SignInManager<ApplicationUser> signInManager,
            IEmailSender emailSender,
            IHtmlLocalizer<SharedResource> sharedLocalizer)
        {
            _userManager = userManager;
            _userStore = userStore;
            _emailStore = GetEmailStore();
            _signInManager = signInManager;
            _emailSender = emailSender;
            _sharedLocalizer = sharedLocalizer;
        }
        
        [BindProperty]
        public InputModel Input { get; set; }
        
        public string ReturnUrl { get; set; }
        
        public IList<AuthenticationScheme> ExternalLogins { get; set; }
        
        public class InputModel
        {
            [Required(ErrorMessageResourceType = typeof(Resources.ValidationErrorResource), ErrorMessageResourceName = "ERROR_EmailRequired")]
            [EmailAddress]
            public string Email { get; set; }
            
            [Required(ErrorMessageResourceType = typeof(Resources.ValidationErrorResource), ErrorMessageResourceName = "ERROR_PasswordRequired")]
            [StringLength(100, ErrorMessageResourceType = typeof(Resources.ValidationErrorResource), ErrorMessageResourceName = "ERROR_PasswordLength", MinimumLength = 6)]
            [DataType("Password")]
            public string Password { get; set; }
            
            [Compare("Password", ErrorMessageResourceType = typeof(Resources.ValidationErrorResource), ErrorMessageResourceName = "ERROR_PasswordsNotMatch")]
            [DataType("Password")]
            public string ConfirmPassword { get; set; }
            
            [Required(ErrorMessageResourceType = typeof(Resources.ValidationErrorResource), ErrorMessageResourceName = "ERROR_FirstNameRequired")]
            public string Firstname { get; set; }
            
            [Required(ErrorMessageResourceType = typeof(Resources.ValidationErrorResource), ErrorMessageResourceName = "ERROR_LastNameRequired")]
            public string Lastname { get; set; }
            public string DisplayName { get; set; }
        }

        public void OnGetAsync(string returnUrl = null)
        {
            ReturnUrl = returnUrl;
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            
            var user = CreateUser();
            
            user.FirstName = Input.Firstname;
            user.LastName = Input.Lastname;
            user.DisplayName = Input.DisplayName;
            
            await _userStore.SetUserNameAsync(user, Input.Email, CancellationToken.None);
            await _emailStore.SetEmailAsync(user, Input.Email, CancellationToken.None);
            var result = await _userManager.CreateAsync(user, Input.Password);

            if (result.Succeeded)
            {
                var userId = await _userManager.GetUserIdAsync(user);
                var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                var callbackUrl = Url.Page(
                    "/Account/ConfirmEmail",
                    pageHandler: null,
                    values: new { area = "Identity", userId, code, returnUrl },
                    protocol: Request.Scheme);
                
                if (callbackUrl == null)
                    throw new NullReferenceException("Call back URL is null");

                try
                {
                    await _emailSender.SendEmailAsync(Input.Email, _sharedLocalizer["Confirmation_Email_Subject"].Value,
                        _sharedLocalizer["Confirmation_Email_Body"].Value +
                        $"<a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>" +
                        _sharedLocalizer["Confirmation_Email_Link"] + "</a>.");
                }
                catch
                {
                    Console.WriteLine("Email sending failed");
                }
                
                await _signInManager.SignInAsync(user, isPersistent: false);
                return LocalRedirect(returnUrl);
            }

            // If we got this far, something failed, redisplay form
            ViewData["Error"] = _sharedLocalizer["Register_Duplicate_Username"];
            return Page();
        }

        private ApplicationUser CreateUser()
        {
            try
            {
                return Activator.CreateInstance<ApplicationUser>();
            }
            catch
            {
                throw new InvalidOperationException($"Can't create an instance of '{nameof(ApplicationUser)}'. " +
                    $"Ensure that '{nameof(ApplicationUser)}' is not an abstract class and has a parameterless constructor, or alternatively " +
                    $"override the register page in /Areas/Identity/Pages/Account/Register.cshtml");
            }
        }

        private IUserEmailStore<ApplicationUser> GetEmailStore()
        {
            if (!_userManager.SupportsUserEmail)
            {
                throw new NotSupportedException("The default UI requires a user store with email support.");
            }
            return (IUserEmailStore<ApplicationUser>)_userStore;
        }
    }
}
