using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TOS.Data;
using TOS.Models;

namespace TOS.Controllers
{
    public class GroupController : Controller
    {
        private readonly ApplicationDbContext _context;

        public GroupController(ApplicationDbContext context)
        {
            _context = context;
        }
        
        public async Task<IActionResult> Index(string searchString = "", bool showHidden = false)
        {
            var groups = _context.Groups.Where(x => !x.Selectable && x.NameEng != "Unassigned");

            //Show hidden
            ViewData["showHidden"] = showHidden;
            if (!showHidden || (!User.IsInRole("Group") && !User.IsInRole("AnyGroup")))
            {
                groups = groups.Where(x => x.Visible);
            }

            //Search
            if (searchString.Length > 2)
            {
                ViewData["searchString"] = searchString;
                searchString = searchString.ToLower();
                groups = groups.Where(x =>
                    x.Name.ToLower().Contains(searchString) || x.NameEng!.ToLower().Contains(searchString));
            }

            //Highlight used groups
            var user = _context.Users.FirstOrDefault(x => User.Identity != null && x.UserName == User.Identity.Name);
            var allGroups = await groups.ToListAsync();
            if (user is not null)
            {
                foreach (var group in allGroups
                             .Where(group =>
                                 user.AssignedTopics.Any(x => x.GroupId == group.GroupId) ||
                                 user.SupervisedTopics.Any(x => x.GroupId == group.GroupId) ||
                                 user.CreatedGroups.Any(x => x.GroupId == group.GroupId) ||
                                 user.CreatedTopics.Any(x => x.GroupId == group.GroupId) ||
                                 user.UserInterestedTopics.Any(x=>x.Topic.GroupId == group.GroupId) ||
                                 group.NameEng == "Bachelor" || group.NameEng == "Master" || group.NameEng == "Unassigned")
                             .ToList())
                {
                    group.Highlight = true;
                }
            }
            
            return View(allGroups);
        }

        [Authorize(Roles ="Group,AnyGroup")]
        public IActionResult Create()
        {
            return View();
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles ="Group,AnyGroup")]
        public async Task<IActionResult> Create([Bind("GroupId,Name,NameEng,Description,DescriptionEng,CreatorId,Selectable,Visible")] Group group)
        {
            if(string.IsNullOrEmpty(group.NameEng)) group.NameEng = group.Name;
            
            group.CreatorId = _context.Users.First(x => User.Identity != null && x.UserName == User.Identity.Name).Id;
            
            await _context.AddAsync(group);
            await _context.SaveChangesAsync();
            
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles ="Group,AnyGroup")]
        public async Task<IActionResult> Edit(int? id)
        {
            var group = await _context.Groups.FindAsync(id);
            
            return View(group);
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles ="Group,AnyGroup")]
        public async Task<IActionResult> Edit(int id, [Bind("GroupId,Name,Selectable,NameEng,Description,DescriptionEng,CreatorId,Visible")] Group group)
        {
            //If user has only Role EditGroup, check if he is the owner of the group
            if (User.IsInRole("Group") && !User.IsInRole("AnyGroup"))
            {
                if (group.CreatorId != _context.Users.First(x => User.Identity != null && x.UserName == User.Identity.Name).Id)
                {
                    return Forbid();
                }
            }

            if(group.NameEng == "") group.NameEng = group.Name;
            
            //Why
            group.Creator = _context.Users.First(x => x.Id.Equals(group.CreatorId));
            
            _context.Update(group);
            await _context.SaveChangesAsync();

            switch (group.NameEng)
            {
                case "Bachelor":
                    case "Master":
                    return RedirectToAction("Index", "Topic", new{ groupName = group.NameEng});
                case "Unassigned":
                    return RedirectToAction("Unassigned", "Topic");
                default:
                    return RedirectToAction("Group", "Topic", new {groupId = group.GroupId});
            }
        }
        
        [Authorize(Roles ="Group,AnyGroup")]
        public async Task<IActionResult> Delete(int? id)
        {
            var group = await _context.Groups.FirstAsync(m => m.GroupId == id);

            //If use has only Role DeleteGroup, check if he is the owner of the group
            if (User.IsInRole("Group") && !User.IsInRole("AnyGroup"))
            {
                if (group.CreatorId != _context.Users.First(x => User.Identity != null && x.UserName == User.Identity.Name).Id)
                {
                    return Forbid();
                }
            }
            
            //Delete group's topics
            _context.Topics.RemoveRange(group.Topics);
            await _context.SaveChangesAsync();
            
            //Delete group
            _context.Groups.Remove(group);
            await _context.SaveChangesAsync();
            
            return RedirectToAction(nameof(Index));
        }
    }
}
