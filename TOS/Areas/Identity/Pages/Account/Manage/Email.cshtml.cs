// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Localization;
using TOS.Models;
using TOS.Resources;

namespace TOS.Areas.Identity.Pages.Account.Manage
{
    public class EmailModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IEmailSender _emailSender;
        private readonly IStringLocalizer _localizer;

        public EmailModel(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IEmailSender emailSender,
            IStringLocalizer<SharedResource> localizer)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _emailSender = emailSender;
            _localizer = localizer;
        }
        
        public string Email { get; set; }
        
        public bool IsEmailConfirmed { get; set; }
        
        [TempData]
        public string StatusMessage { get; set; }
        
        [BindProperty]
        public InputModel Input { get; set; }
        
        public class InputModel
        {
            [Required(ErrorMessageResourceType = typeof(ValidationErrorResource), ErrorMessageResourceName = "ERROR_EmailRequired")]
            [EmailAddress]
            public string NewEmail { get; set; }
        }

        private async Task LoadAsync(ApplicationUser user)
        {
            var email = await _userManager.GetEmailAsync(user);
            Email = email;

            Input = new InputModel
            {
                NewEmail = email,
            };

            IsEmailConfirmed = await _userManager.IsEmailConfirmedAsync(user);
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            await LoadAsync(user);
            return Page();
        }

        public async Task<IActionResult> OnPostChangeEmailAsync()
        {
            Input.NewEmail = Input.NewEmail.Trim().ToLower();
            
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            if (!ModelState.IsValid)
            {
                await LoadAsync(user);
                return Page();
            }
            
            if (_userManager.Users.Any(x => x.Email!.Equals(Input.NewEmail)))
            {
                StatusMessage = _localizer["Confirmation_Email_NotChanged"];
                return RedirectToPage();
            }

            var email = await _userManager.GetEmailAsync(user);
            if (Input.NewEmail != email)
            {
                var userId = await _userManager.GetUserIdAsync(user);
                var code = await _userManager.GenerateChangeEmailTokenAsync(user, Input.NewEmail);
                code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                var callbackUrl = Url.Page(
                    "/Account/ConfirmEmailChange",
                    pageHandler: null,
                    values: new { area = "Identity", userId, email = Input.NewEmail, code },
                    protocol: Request.Scheme);
                await _emailSender.SendEmailAsync(Input.NewEmail, _localizer["Confirmation_Email_Subject"].Value,
                    _localizer["Confirmation_Email_Body"].Value +
                    $" <a href='{HtmlEncoder.Default.Encode(callbackUrl ?? string.Empty)}'>" +
                    _localizer["Confirmation_Email_Link"] + "</a>.");

                StatusMessage = _localizer["Confirmation_Email_Sent"];
                return RedirectToPage();
            }

            StatusMessage = _localizer["Confirmation_Email_NotChanged"];
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostSendVerificationEmailAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            if (!ModelState.IsValid)
            {
                await LoadAsync(user);
                return Page();
            }

            var userId = await _userManager.GetUserIdAsync(user);
            //var email = await _userManager.GetEmailAsync(user);
            var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
            var callbackUrl = Url.Page(
                "/Account/ConfirmEmail",
                pageHandler: null,
                values: new { area = "Identity", userId, code },
                protocol: Request.Scheme);
        
            await _emailSender.SendEmailAsync(Input.NewEmail, _localizer["Confirmation_Email_Subject"].Value,
                _localizer["Confirmation_Email_Body"].Value +
                $" <a href='{HtmlEncoder.Default.Encode(callbackUrl ?? string.Empty)}'>" +
                _localizer["Confirmation_Email_Link"] + "</a>.");


            StatusMessage = _localizer["Confirmation_Email_Sent"];
            return RedirectToPage();
        }
    }
}
