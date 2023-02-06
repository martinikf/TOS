using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TOS.Data;
using TOS.Models;
using TOS.Services;

namespace TOS.Controllers;

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
    
   
    public IActionResult CreateProgramme()
    {
        return View();
    }
    
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateProgramme([Bind("ProgrammeId,Name,NameEng,Active,Type")] Programme programme, string typeDropdown)
    {
        programme.Type = typeDropdown.Equals("Bachelor") ? ProgramType.Bachelor : ProgramType.Master;
        
        await _context.Programmes.AddAsync(programme);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }
    
    public async Task<IActionResult> EditProgramme(int? id)
    {
        var p =  await _context.Programmes.FirstOrDefaultAsync(x => x.ProgrammeId == id);

        if (p is null) return NotFound();

        ViewData["IsMaster"] = (bool)(p.Type == ProgramType.Master);
        
        return View(p);
    }

    [HttpPost]
    public async Task<IActionResult> EditProgramme([Bind("ProgrammeId,Name,NameEng,Active,Type")]Programme programme, string typeDropdown)
    {
        programme.Type = typeDropdown.Equals("Bachelor") ? ProgramType.Bachelor : ProgramType.Master;
        
        _context.Update(programme);
        await _context.SaveChangesAsync();
        
        return RedirectToAction(nameof(Index));
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
    
    public async Task<IActionResult> Users()
    {
        var users = await _context.Users.ToListAsync();
        
        return View(users);
    }
    
    public async Task<IActionResult> EditRoles(int? id)
    {
        var user = await _context.Users.FirstAsync(x => x.Id.Equals(id));
        ViewData["Roles"] = new List<string> {"Student", "Teacher", "Administrator", "External"};
        
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