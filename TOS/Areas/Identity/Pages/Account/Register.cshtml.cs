// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System.ComponentModel.DataAnnotations;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Localization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using TOS.Data;
using TOS.Models;
using TOS.Resources;
using TOS.Services;

namespace TOS.Areas.Identity.Pages.Account
{
    [Authorize(Roles ="Administrator")]
    public class RegisterModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUserStore<ApplicationUser> _userStore;
        private readonly IUserEmailStore<ApplicationUser> _emailStore;
        private readonly IEmailSender _emailSender;
        private readonly IHtmlLocalizer<SharedResource> _sharedLocalizer;
        private readonly IAuthentication _authentication;
        private readonly ApplicationDbContext _ctx;

        public RegisterModel(
            UserManager<ApplicationUser> userManager,
            IUserStore<ApplicationUser> userStore,
            SignInManager<ApplicationUser> signInManager,
            IEmailSender emailSender,
            IHtmlLocalizer<SharedResource> sharedLocalizer,
            IAuthentication authentication,
            ApplicationDbContext ctx)
        {
            _userManager = userManager;
            _userStore = userStore;
            _emailStore = GetEmailStore();
            _signInManager = signInManager;
            _emailSender = emailSender;
            _sharedLocalizer = sharedLocalizer;
            _authentication = authentication;
            _ctx = ctx;
        }
        
        [BindProperty]
        public InputModel Input { get; set; }
        
        public string ReturnUrl { get; set; }
        
        public IList<AuthenticationScheme> ExternalLogins { get; set; }
        
        public class InputModel
        {
            [Required(ErrorMessageResourceType = typeof(ValidationErrorResource), ErrorMessageResourceName = "ERROR_EmailRequired")]
            [EmailAddress]
            public string Email { get; set; }

            [Required(ErrorMessageResourceType = typeof(ValidationErrorResource), ErrorMessageResourceName = "ERROR_FirstNameRequired")]
            public string Firstname { get; set; }
            
            [Required(ErrorMessageResourceType = typeof(ValidationErrorResource), ErrorMessageResourceName = "ERROR_LastNameRequired")]
            public string Lastname { get; set; }
            
            public string DisplayName { get; set; }
            
            public string RoleGroup { get; set; }
        }

        public void OnGetAsync()
        {
            
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var email = Input.Email.Trim().ToLower();
            var password = Guid.NewGuid().ToString().Substring(0, 8);
            
            var user = CreateUser();
            
            user.FirstName = Input.Firstname;
            user.LastName = Input.Lastname;
            user.DisplayName = Input.DisplayName;
            
            await _userStore.SetUserNameAsync(user, email, CancellationToken.None);
            await _emailStore.SetEmailAsync(user, email, CancellationToken.None);
            var result = await _userManager.CreateAsync(user, password);
            
           

            if (result.Succeeded)
            {
                var userId = await _userManager.GetUserIdAsync(user);
                var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                var callbackUrl = Url.Page(
                    "/Account/ConfirmEmail",
                    pageHandler: null,
                    values: new { area = "Identity", userId, code },
                    protocol: Request.Scheme);
                
                if (callbackUrl == null)
                    throw new NullReferenceException("Call back URL is null");

                await _emailSender.SendEmailAsync(email, _sharedLocalizer["Register_Email_Subject"].Value,
                    _sharedLocalizer["Register_Email_Body"].Value +
                    $"<h3>{user.UserName} : {password}</h3>" +
                    $" <a href='{callbackUrl}'>" +
                    _sharedLocalizer["Register_Email_Link"].Value + "</a>.");

                if (Input.RoleGroup != null && Input.RoleGroup.Length > 0)
                {
                    Enum.TryParse(Input.RoleGroup, out Role role);
                    await RoleHelper.AssignRoles(user, role, _ctx);
                }

                return RedirectToAction("Users", "Administration");
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
