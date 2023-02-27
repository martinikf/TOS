// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
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
    [AllowAnonymous]
    public class ResendEmailConfirmationModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailSender _emailSender;
        private readonly IStringLocalizer<SharedResource> _sharedLocalizer;

        public ResendEmailConfirmationModel(UserManager<ApplicationUser> userManager, IEmailSender emailSender, IStringLocalizer<SharedResource> sharedLocalizer)
        {
            _userManager = userManager;
            _emailSender = emailSender;
            _sharedLocalizer = sharedLocalizer;
        }

        [BindProperty]
        public InputModel Input { get; set; }
        
        public class InputModel
        {
            [Required(ErrorMessageResourceType = typeof(Resources.ValidationErrorResource), ErrorMessageResourceName = "ERROR_EmailRequired")]
            [EmailAddress]
            public string Email { get; set; }
        }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var user = await _userManager.FindByEmailAsync(Input.Email);
            if (user == null)
            {
                return Page();
            }

            var userId = await _userManager.GetUserIdAsync(user);
            var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
            var callbackUrl = Url.Page(
                "/Account/ConfirmEmail",
                pageHandler: null,
                values: new { userId = userId, code = code },
                protocol: Request.Scheme);
            await _emailSender.SendEmailAsync(Input.Email, _sharedLocalizer["Confirmation_Email_Subject"].Value,
                _sharedLocalizer["Confirmation_Email_Body"].Value +
                $"<a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>" +
                _sharedLocalizer["Confirmation_Email_Link"] + "</a>.");
            
            return Page();
        }
    }
}
