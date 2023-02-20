using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TOS.Data;
using TOS.Models;
using TOS.Services;

namespace TOS.Controllers;

[Authorize(Roles ="Administrator")]
public class AdministrationController : Controller
{
    
    private readonly ApplicationDbContext _context;

    public AdministrationController(ApplicationDbContext context)
    {
        _context = context;
    }
    
    public IActionResult Index()
    {
        return View();
    }

    public async Task<IActionResult> Programmes()
    {
        var programmes = await _context.Programmes.ToListAsync();
        
        return View(programmes);
    }

    public IActionResult CreateProgramme(string? error)
    {
        ViewData["Error"] = error;
        return View();
    }
    
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateProgramme([Bind("ProgrammeId,Name,NameEng,Active,Type")] Programme programme, string typeDropdown)
    {
        programme.Type = typeDropdown.Equals("Bachelor") ? ProgramType.Bachelor : ProgramType.Master;

        if (_context.Programmes.Any(x => (x.Name.Equals(programme.Name) || x.NameEng.Equals(programme.NameEng)) && x.Type.Equals(programme.Type)))
        {
            return RedirectToAction("CreateProgramme", new{error="ALREADY_EXISTS"});
        }
        
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
    public async Task<IActionResult> EditProgramme([Bind("ProgrammeId,Name,NameEng,Active,Type")]Programme programme, string typeDropdown)
    {
        programme.Type = typeDropdown.Equals("Bachelor") ? ProgramType.Bachelor : ProgramType.Master;
        
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
    
    public async Task<IActionResult> EditRoles(int? id)
    {
        var user = await _context.Users.FirstAsync(x => x.Id.Equals(id));
        ViewData["Roles"] = new List<string> {"Student", "Teacher", "Administrator", "External"};

        ViewData["Student"] = await _context.UserRoles.AnyAsync(x => x.UserId.Equals(user.Id) && x.RoleId.Equals(_context.Roles.First(y=>y.Name == "Student").Id));
        ViewData["Teacher"] = await _context.UserRoles.AnyAsync(x => x.UserId.Equals(user.Id) && x.RoleId.Equals(_context.Roles.First(y=>y.Name =="Teacher").Id));
        ViewData["Administrator"] = await _context.UserRoles.AnyAsync(x => x.UserId.Equals(user.Id) && x.RoleId.Equals(_context.Roles.First(y=>y.Name =="Administrator").Id));
        ViewData["External"] = await _context.UserRoles.AnyAsync(x => x.UserId.Equals(user.Id) && x.RoleId.Equals(_context.Roles.First(y=>y.Name =="External").Id));

        return View(user);
    }
    
    [HttpPost]
    public async Task<IActionResult> EditRoles(int id, string roleGroup)
    {
        var user = await _context.Users.FirstAsync(x => x.Id.Equals(id));
        
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
}