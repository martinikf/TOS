// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Localization;
using TOS.Models;
using TOS.Resources;

namespace TOS.Areas.Identity.Pages.Account
{
    public class ResetPasswordModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IStringLocalizer<SharedResource> _localizer;

        public ResetPasswordModel(UserManager<ApplicationUser> userManager, IStringLocalizer<SharedResource> localizer)
        {
            _userManager = userManager;
            _localizer = localizer;
        }
        
        [BindProperty]
        public InputModel Input { get; set; }
        
        public class InputModel
        {
            [Required]
            [EmailAddress(ErrorMessageResourceType = typeof(ValidationErrorResource), ErrorMessageResourceName = "ERROR_EmailInvalid")]
            public string Email { get; set; }
            
            [DataType("Password")]
            [Required(ErrorMessageResourceType = typeof(ValidationErrorResource), ErrorMessageResourceName = "ERROR_PasswordRequired")]
            [StringLength(100, ErrorMessageResourceType = typeof(ValidationErrorResource), ErrorMessageResourceName = "ERROR_PasswordLength", MinimumLength = 6)]
            public string Password { get; set; }
            
            [DataType(DataType.Password)]
            [Compare("Password", ErrorMessageResourceType = typeof(ValidationErrorResource), ErrorMessageResourceName = "ERROR_PasswordsNotMatch")]
            public string ConfirmPassword { get; set; }
            
            [Required]
            public string Code { get; set; }

        }

        public IActionResult OnGet(string code = null)
        {
            if (code == null)
            {
                return BadRequest("A code must be supplied for password reset.");
            }
            else
            {
                Input = new InputModel
                {
                    Code = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code))
                };
                return Page();
            }
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
                // Don't reveal that the user does not exist
                return RedirectToPage("./ResetPasswordConfirmation");
            }

            var result = await _userManager.ResetPasswordAsync(user, Input.Code, Input.Password);
            if (result.Succeeded)
            {
                return RedirectToPage("./ResetPasswordConfirmation");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            return Page();
        }
    }
}
