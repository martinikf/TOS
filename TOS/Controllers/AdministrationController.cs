using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TOS.Data;
using TOS.Models;

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
    public async Task<IActionResult> CreateProgramme(Programme programme)
    {
        await _context.Programmes.AddAsync(programme);

        return RedirectToAction(nameof(Index));
    }
    
    public IActionResult EditProgramme(int? id)
    {
        
    }

    [HttpPost]
    public async Task<IActionResult> EditProgramme(Programme programme)
    {
        
    }
    
    
    public async Task<IActionResult> Users()
    {
        var users = await _context.Users.ToListAsync();
        
        return View(users);
    }
    
    public async Task<IActionResult> EditRoles(int? id)
    {
        
    }
}