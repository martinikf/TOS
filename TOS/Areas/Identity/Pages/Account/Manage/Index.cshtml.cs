// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TOS.Data;
using TOS.Models;

namespace TOS.Areas.Identity.Pages.Account.Manage
{
    public class IndexModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _context;

        public IndexModel(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
        }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [TempData]
        public string StatusMessage { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [BindProperty]
        public InputModel Input { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public class InputModel
        {
            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Display(Name = "First name")]
            public string Firstname { get; set; }
            
            [Display(Name = "Last name")]
            public string Lastname { get; set; }
            
            [Display(Name = "Display name")]
            public string DisplayName { get; set; }
        }

        private async Task LoadAsync(ApplicationUser user)
        {
            var userName = await _userManager.GetUserNameAsync(user);
            //Get current user
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser is null) throw new Exception("User should not be null");
            var firstname = currentUser.FirstName;
            var lastname = currentUser.LastName;
            var displayName = currentUser.DisplayName;

            Username = userName;

            var notifications = _context.Notifications.ToList();
            foreach(var n in notifications)
            {
                if (_context.UserSubscribedNotifications.Any(x =>
                        x.UserId == user.Id && x.NotificationId == n.NotificationId))
                {
                    n.Selected = true;
                }
            }
            ViewData["Notifications"] = notifications;

            Input = new InputModel
            {
                Firstname = firstname,
                Lastname = lastname,
                DisplayName = displayName
            };
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

        public async Task<IActionResult> OnPostAsync(int[] selectedNotifications)
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
            
            //TODO Add stag api sync option for AD-users
            user.FirstName = Input.Firstname;
            user.LastName = Input.Lastname;
            user.DisplayName = Input.DisplayName;
            await _userManager.UpdateAsync(user);

            var toRemove = _context.UserSubscribedNotifications
                .Where(x => x.UserId == user.Id).ToList();
            _context.UserSubscribedNotifications.RemoveRange(toRemove);

            foreach (var not in selectedNotifications)
            {
                _context.UserSubscribedNotifications.Add(new() { UserId = user.Id, NotificationId = not});
            }

            await _context.SaveChangesAsync();
            
            await _signInManager.RefreshSignInAsync(user);
            StatusMessage = "Your profile has been updated";
            
            return RedirectToPage();
        }
    }
}
