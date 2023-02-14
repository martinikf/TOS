using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TOS.Data;
using TOS.Models;

namespace TOS
{
    public class GroupController : Controller
    {
        private readonly ApplicationDbContext _context;

        public GroupController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Group
        public async Task<IActionResult> Index(string searchString = "", bool showHidden = false)
        {
            var groups = await _context.Groups.Include(x => x.Creator).ToListAsync();

            //Show hidden
            ViewData["showHidden"] = showHidden;
            if (!showHidden)
            {
                groups = groups.Where(x => x.Visible).ToList();
            }

            //Search
            if (searchString.Length > 2)
            {
                ViewData["searchString"] = searchString;
                searchString = searchString.ToLower();
                groups = groups.Where(x =>
                    x.Name.ToLower().Contains(searchString) || x.NameEng.ToLower().Contains(searchString)).ToList();
            }

            //Higlight used groups
            var user = _context.Users.FirstOrDefault(x => User.Identity != null && x.UserName == User.Identity.Name);
            if (user is not null)
            {
                foreach (var group in groups
                             .Where(group =>
                                 user.AssignedTopics.Any(x => x.GroupId == group.GroupId) ||
                                 user.SupervisedTopics.Any(x => x.GroupId == group.GroupId) ||
                                 user.CreatedGroups.Any(x => x.GroupId == group.GroupId) ||
                                 user.CreatedTopics.Any(x => x.GroupId == group.GroupId) ||
                                 group.NameEng is "Bachelor" or "Master" or "Unassigned"))
                {
                    group.Highlight = true;
                }
            }
            
            return View(groups);
        }
        
        // GET: Group/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Group/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("GroupId,Name,NameEng,CreatorId,Selectable,Visible")] Group group)
        {
            if(group.NameEng == "") group.NameEng = group.Name;
            
            group.CreatorId = _context.Users.First(x => User.Identity != null && x.UserName == User.Identity.Name).Id;
            
            _context.Add(group);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: Group/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            var group = await _context.Groups.FindAsync(id);

            return View(group);
        }

        // POST: Group/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("GroupId,Name,CreatorId,Selectable,Visible")] Group group)
        {
            if(group.NameEng == "") group.NameEng = group.Name;
            
            //Why
            group.Creator = _context.Users.First(x => x.Id.Equals(group.CreatorId));
            
            _context.Update(group);
            await _context.SaveChangesAsync();
            
            return RedirectToAction(nameof(Index));
        }

        // GET: Group/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            var group = await _context.Groups.FirstAsync(m => m.GroupId == id);

            var unassigned = _context.Groups.First(x => x.NameEng == "Unassigned");
            
            foreach (var topic in group.Topics)
            {
                topic.Group = unassigned;
                _context.Topics.Update(topic);
            }
            
            await _context.SaveChangesAsync();
            
            _context.Groups.Remove(group);
            
            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }

        // POST: Group/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.Groups == null)
            {
                return Problem("Entity set 'ApplicationDbContext.Groups'  is null.");
            }
            var @group = await _context.Groups.FindAsync(id);
            if (@group != null)
            {
                _context.Groups.Remove(@group);
            }
            
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool GroupExists(int id)
        {
          return (_context.Groups?.Any(e => e.GroupId == id)).GetValueOrDefault();
        }
    }
}
