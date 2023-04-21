// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TOS.Data;
using TOS.Models;

namespace TOS.Areas.Identity.Pages.Account.Manage
{
    public class NotificationModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _context;

        public NotificationModel(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            var notifications = new Dictionary<string, Notification>();
            
            foreach(var n in await _context.Notifications.ToListAsync())
            {
                if (_context.UserSubscribedNotifications.Any(x =>
                        x.UserId == user.Id && x.NotificationId == n.NotificationId))
                {
                    n.Selected = true;
                }
                notifications.Add(n.Name, n);
            }

            ViewData["Notification"] = notifications;
            
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int[] selectedNotifications)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            var toRemove = _context.UserSubscribedNotifications
                .Where(x => x.UserId == user.Id).ToList();
            _context.UserSubscribedNotifications.RemoveRange(toRemove);

            foreach (var not in selectedNotifications)
            {
                _context.UserSubscribedNotifications.Add(new() { UserId = user.Id, NotificationId = not});
            }

            await _context.SaveChangesAsync();

            return RedirectToPage();
        }
    }
}
