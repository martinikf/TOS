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

namespace TOS.Areas.Identity.Pages.Account
{
    public class ForgotPasswordModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailSender _emailSender;
        private readonly IStringLocalizer<SharedResource> _localizer;

        public ForgotPasswordModel(UserManager<ApplicationUser> userManager, IEmailSender emailSender, IStringLocalizer<SharedResource> localizer)
        {
            _userManager = userManager;
            _emailSender = emailSender;
            _localizer = localizer;
        }
        
        [BindProperty]
        public InputModel Input { get; set; }
        
        public class InputModel
        {
            [Required(ErrorMessageResourceType = typeof(ValidationErrorResource), ErrorMessageResourceName = "ERROR_EmailRequired")]
            [EmailAddress(ErrorMessageResourceType = typeof(ValidationErrorResource), ErrorMessageResourceName = "ERROR_EmailInvalid")]
            public string Email { get; set; }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            //If AD/Stag user tries to reset password, redirect to login page
            if (Input.Email.ToLower().EndsWith("@upol.cz"))
            {
                //TODO: Error message
                return RedirectToPage("./Login");
            }
            
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(Input.Email);
                if (user == null || !(await _userManager.IsEmailConfirmedAsync(user)))
                {
                    // Don't reveal that the user does not exist or is not confirmed
                    return RedirectToPage("./ForgotPasswordConfirmation");
                }
                
                var code = await _userManager.GeneratePasswordResetTokenAsync(user);
                code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                var callbackUrl = Url.Page(
                    "/Account/ResetPassword",
                    pageHandler: null,
                    values: new { area = "Identity", code },
                    protocol: Request.Scheme);

                await _emailSender.SendEmailAsync(
                    Input.Email,
                    _localizer["ForgotPassword_Email_Subject"],
                    _localizer["ForgotPassword_Email_Body"] + $" <a href='{HtmlEncoder.Default.Encode(callbackUrl ?? string.Empty)}'>" + _localizer["ForgotPassword_Email_Link"] + "</a>.");

                return RedirectToPage("./ForgotPasswordConfirmation");
            }

            return Page();
        }
    }
}
