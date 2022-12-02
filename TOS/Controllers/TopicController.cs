using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Localization;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TOS.Data;
using TOS.Models;
using TOS.Resources;

namespace TOS.Controllers
{
    public class TopicController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IHtmlLocalizer<SharedResource> _sharedLocalizer;

        public TopicController(ApplicationDbContext context,  IHtmlLocalizer<SharedResource> sharedLocalizer)
        {
            _context = context;
            _sharedLocalizer = sharedLocalizer;
        }

        // GET: Topic
        public async Task<IActionResult> Index(string? groupName)
        {

            Group? group = null;
            ViewData["TopicsIndexGroupId"] = null;
            
            if (groupName != null) group = _context.Groups.FirstOrDefault(x => x.Name.Equals(groupName));
            
            if (group == null)
            {
                ViewData["TopicsIndexHeading"] = _sharedLocalizer["Topics"];

                //If groupId wasn't provided or groupId is invalid -> Display all Bachelor and Master topics
                var applicationDbContext = _context.Topics.Where(x => x.Group.Selectable && x.Group.Visible)
                    .Include(t => t.AssignedStudent).Include(t => t.Creator)
                    .Include(t => t.Group).Include(t => t.Supervisor);
                return View(await applicationDbContext.ToListAsync());
            }
            else
            {
                ViewData["TopicsIndexHeading"] = _sharedLocalizer[groupName];
                
                if (!group.Selectable)
                {
                    ViewData["TopicsIndexGroupId"] = group.GroupId;
                }

                var applicationDbContext = _context.Topics.Where(x => x.Group.Name.Equals(groupName) && x.Visible)
                    .Include(t => t.AssignedStudent).Include(t => t.Creator)
                    .Include(t => t.Group).Include(t => t.Supervisor);
                return View(await applicationDbContext.ToListAsync());
            }
        }

        // GET: Topic/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.Topics == null)
            {
                return NotFound();
            }

            var topic = await _context.Topics
                .Include(t => t.AssignedStudent)
                .Include(t => t.Creator)
                .Include(t => t.Group)
                .Include(t => t.Supervisor)
                .FirstOrDefaultAsync(m => m.TopicId == id);
            if (topic == null)
            {
                return NotFound();
            }

            return View(topic);
        }

        // GET: Topic/Create
        public IActionResult Create()
        {
            ViewData["AssignedId"] = new SelectList(_context.Users, "Id", "Id");
            ViewData["CreatorId"] = new SelectList(_context.Users, "Id", "Id");
            ViewData["GroupId"] = new SelectList(_context.Groups.Where(x=>x.Selectable), "GroupId", "GroupId");
            ViewData["SupervisorId"] = new SelectList(_context.Users, "Id", "Id");
            return View();
        }

        // POST: Topic/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("TopicId,Name,DescriptionShort,DescriptionLong,Visible,CreatorId,SupervisorId,AssignedId,GroupId")] Topic topic)
        {
            if (ModelState.IsValid)
            {
                _context.Add(topic);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["AssignedId"] = new SelectList(_context.Users, "Id", "Id", topic.AssignedId);
            ViewData["CreatorId"] = new SelectList(_context.Users, "Id", "Id", topic.CreatorId);
            ViewData["GroupId"] = new SelectList(_context.Groups.Where(x=>x.Selectable), "GroupId", "GroupId", topic.GroupId);
            ViewData["SupervisorId"] = new SelectList(_context.Users, "Id", "Id", topic.SupervisorId);
            return View(topic);
        }

        // GET: Topic/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.Topics == null)
            {
                return NotFound();
            }

            var topic = await _context.Topics.FindAsync(id);
            if (topic == null)
            {
                return NotFound();
            }
            ViewData["AssignedId"] = new SelectList(_context.Users, "Id", "Id", topic.AssignedId);
            ViewData["CreatorId"] = new SelectList(_context.Users, "Id", "Id", topic.CreatorId);
            ViewData["GroupId"] = new SelectList(_context.Groups.Where(x=>x.Selectable), "GroupId", "GroupId", topic.GroupId);
            ViewData["SupervisorId"] = new SelectList(_context.Users, "Id", "Id", topic.SupervisorId);
            return View(topic);
        }

        // POST: Topic/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("TopicId,Name,DescriptionShort,DescriptionLong,Visible,CreatorId,SupervisorId,AssignedId,GroupId")] Topic topic)
        {
            if (id != topic.TopicId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(topic);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TopicExists(topic.TopicId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["AssignedId"] = new SelectList(_context.Users, "Id", "Id", topic.AssignedId);
            ViewData["CreatorId"] = new SelectList(_context.Users, "Id", "Id", topic.CreatorId);
            ViewData["GroupId"] = new SelectList(_context.Groups.Where(x=>x.Selectable), "GroupId", "GroupId", topic.GroupId);
            ViewData["SupervisorId"] = new SelectList(_context.Users, "Id", "Id", topic.SupervisorId);
            return View(topic);
        }

        // GET: Topic/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.Topics == null)
            {
                return NotFound();
            }

            var topic = await _context.Topics
                .Include(t => t.AssignedStudent)
                .Include(t => t.Creator)
                .Include(t => t.Group)
                .Include(t => t.Supervisor)
                .FirstOrDefaultAsync(m => m.TopicId == id);
            if (topic == null)
            {
                return NotFound();
            }

            return View(topic);
        }

        // POST: Topic/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.Topics == null)
            {
                return Problem("Entity set 'ApplicationDbContext.Topics'  is null.");
            }
            var topic = await _context.Topics.FindAsync(id);
            if (topic != null)
            {
                _context.Topics.Remove(topic);
            }
            
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool TopicExists(int id)
        {
          return (_context.Topics?.Any(e => e.TopicId == id)).GetValueOrDefault();
        }
    }
}
