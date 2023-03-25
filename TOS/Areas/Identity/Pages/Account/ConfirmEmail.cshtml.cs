// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.Linq;
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
using TOS.Services;

namespace TOS.Areas.Identity.Pages.Account
{
    public class ConfirmEmailModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IStringLocalizer<SharedResource> _sharedLocalizer;
        private readonly INotificationManager _notificationManager; 

        public ConfirmEmailModel(UserManager<ApplicationUser> userManager,
            IStringLocalizer<SharedResource> sharedLocalizer,
            INotificationManager notificationManager)
        {
            _userManager = userManager;
            _sharedLocalizer = sharedLocalizer;
            _notificationManager = notificationManager;
        }
        
        [TempData]
        public string StatusMessage { get; set; }
        public async Task<IActionResult> OnGetAsync(string userId, string code)
        {
            if (userId == null || code == null)
            {
                return RedirectToPage("/Index");
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{userId}'.");
            }
            
            code = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code));
            var result = await _userManager.ConfirmEmailAsync(user, code);
            StatusMessage = result.Succeeded ? _sharedLocalizer["Manage_ConfirmEmail_Success"] : _sharedLocalizer["Manage_ConfirmEmail_Error"];
            
            if (result.Succeeded)
            {
                var c = "https://" + HttpContext.Request.Host + $"/Administration/Users?searchString={user.Email}";
                await _notificationManager.NewExternalUser(user, c);
            }
            return Page();
        }
    }
}
