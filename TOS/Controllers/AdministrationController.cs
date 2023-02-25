using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using TOS.Data;
using TOS.Models;
using TOS.Resources;
using TOS.Services;

namespace TOS.Controllers;

[Authorize(Roles ="Administrator")]
public class AdministrationController : Controller
{
    
    private readonly ApplicationDbContext _context;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public AdministrationController(ApplicationDbContext context, IStringLocalizer<SharedResource> localizer)
    {
        _context = context;
        _localizer = localizer;
    }

    public async Task<IActionResult> Programmes()
    {
        var programmes = await _context.Programmes.OrderBy(x=>!x.Active).ToListAsync();
        
        return View(programmes);
    }

    public IActionResult CreateProgramme(string? error)
    {
        ViewData["Error"] = error;
        return View();
    }
    
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateProgramme([Bind("ProgrammeId,Name,NameEng,Active,Type")] Programme programme)
    {
        if (_context.Programmes.Any(x => (x.Name.Equals(programme.Name) || x.NameEng!.Equals(programme.NameEng)) && x.Type.Equals(programme.Type)))
        {
            return RedirectToAction("CreateProgramme", new{error=_localizer["Administration_CreateProgramme_Error_AlreadyExists"]});
        }
        
        if(string.IsNullOrEmpty(programme.NameEng))
            programme.NameEng = programme.Name;
        
        await _context.Programmes.AddAsync(programme);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Programmes));
    }
    
    public async Task<IActionResult> EditProgramme(int? id)
    {
        var p =  await _context.Programmes.FirstAsync(x => x.ProgrammeId == id);

        return View(p);
    }

    [HttpPost]
    public async Task<IActionResult> EditProgramme([Bind("ProgrammeId,Name,NameEng,Active,Type")]Programme programme)
    {
        if (_context.Programmes.Any(x => x.ProgrammeId != programme.ProgrammeId && (x.Name.Equals(programme.Name) || x.NameEng!.Equals(programme.NameEng)) && x.Type.Equals(programme.Type)))
        {
            return RedirectToAction("CreateProgramme", new{error="ALREADY_EXISTS"});
        }
        
        _context.Update(programme);
        await _context.SaveChangesAsync();
        
        return RedirectToAction(nameof(Programmes));
    }
    
    public async Task<IActionResult> DeleteProgramme(int? id)
    {
        //Find programme
        var p = await _context.Programmes.FirstAsync(x => x.ProgrammeId.Equals(id));
        //Delete programme
        _context.Programmes.Remove(p);
        //Save changes
        await _context.SaveChangesAsync();
        
        return RedirectToAction(nameof(Programmes));
    }
    
    public async Task<IActionResult> Users(string searchString = "")
    {
        var users =  _context.Users;

        if (searchString.Length > 2)
        {
            ViewData["searchString"] = searchString;
            searchString = searchString.ToLower();

            return View( await users.Where(x=>x.FirstName!.ToLower().Contains(searchString) 
                                              || x.LastName!.ToLower().Contains(searchString) ||
                                              x.Email!.ToLower().Contains(searchString)).ToListAsync());
        }
       
        return View(new List<ApplicationUser>());
    }
    
    public async Task<IActionResult> EditRoles(int? id, bool error = false)
    {
        var user = await _context.Users.FirstAsync(x => x.Id.Equals(id));
        ViewData["Roles"] = new List<string> {"Student", "Teacher", "Administrator", "External"};

        ViewData["Student"] = await _context.UserRoles.AnyAsync(x => x.UserId.Equals(user.Id) && x.RoleId.Equals(_context.Roles.First(y=>y.Name == "Student").Id));
        ViewData["Teacher"] = await _context.UserRoles.AnyAsync(x => x.UserId.Equals(user.Id) && x.RoleId.Equals(_context.Roles.First(y=>y.Name =="Teacher").Id));
        ViewData["Administrator"] = await _context.UserRoles.AnyAsync(x => x.UserId.Equals(user.Id) && x.RoleId.Equals(_context.Roles.First(y=>y.Name =="Administrator").Id));
        ViewData["External"] = await _context.UserRoles.AnyAsync(x => x.UserId.Equals(user.Id) && x.RoleId.Equals(_context.Roles.First(y=>y.Name =="External").Id));

        if(error)
            ViewData["Error"] = _localizer["ERROR:0Administrators"].Value;
        return View();
    }
    
    [HttpPost]
    public async Task<IActionResult> EditRoles(int id, string roleGroup)
    {
        var user = await _context.Users.FirstAsync(x => x.Id.Equals(id));
        if (roleGroup != Role.Administrator.ToString())
        {
            var administratorRole = _context.Roles.First(x=>x.Name == Role.Administrator.ToString());
            if (_context.UserRoles.Count(x => x.RoleId == administratorRole.Id) <= 1)
            {
                return await EditRoles(id, true);
            }
        }

        var role = roleGroup switch
        {
            "Student" => Role.Student,
            "Teacher" => Role.Teacher,
            "Administrator" => Role.Administrator,
            "External" => Role.External,
            _ => throw new Exception()
        };
        
        await RoleHelper.AssignRoles(user, role, _context);
        
        return RedirectToAction(nameof(Users));
    }

    public IActionResult Notifications()
    {
        return View(_context.Notifications.ToList());
    }

    public IActionResult EditNotification(int id)
    {
        var not = _context.Notifications.First(x => x.NotificationId == id);

        return View(not);
    }

    [HttpPost]
    public async Task<IActionResult> EditNotification([Bind("Name,Subject,SubjectEng,Text,TextEng,NotificationId")]Notification notification)
    {
        if (_context.Notifications.Any(x =>
                x.NotificationId != notification.NotificationId && x.Name == notification.Name))
        {
            return RedirectToAction(nameof(EditNotification), new {notification.NotificationId});
        }
        
        if(string.IsNullOrEmpty(notification.SubjectEng))
            notification.SubjectEng = notification.Subject;
        if (string.IsNullOrEmpty(notification.Text))
            notification.TextEng = notification.Text;
        
        _context.Notifications.Update(notification);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Notifications));
    }
}