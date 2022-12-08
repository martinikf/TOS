using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
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
            //Used for showing edit button for not selectable groups (teacher created groups)
            ViewData["TopicsIndexGroupId"] = null;
            
            if (groupName != null) group = await _context.Groups.FirstOrDefaultAsync(x => x.Name.Equals(groupName));
            
            if (group == null)
            {
                if (groupName != null && groupName.Equals("MyTopics"))
                {
                    ViewData["TopicsIndexHeading"] = _sharedLocalizer["My topics"];
                    ViewData["TopicsIndexGroupName"] = null;
                    
                    //get logged in user as ApplicationUser
                    var user = await _context.Users.FirstOrDefaultAsync(x => x.UserName.Equals(User.Identity.Name));

                    if (user is null) throw new Exception();
                    
                    List<Topic> topics = new();

                    topics.AddRange(user.AssignedTopics);
                    topics.AddRange(user.CreatedTopics);
                    topics.AddRange(user.SupervisedTopics);
                    topics.AddRange(user.UserInterestedTopics.Select(t => t.Topic));

                    return View(topics);

                }
                else
                {
                    ViewData["TopicsIndexHeading"] = _sharedLocalizer["Topics"];
                    ViewData["TopicsIndexGroupName"] = null;

                    //If groupId wasn't provided or groupId is invalid -> Display all Bachelor and Master topics
                    var applicationDbContext = _context.Topics.Where(x => x.Group.Selectable && x.Group.Visible)
                        .Include(t => t.AssignedStudent).Include(t => t.Creator)
                        .Include(t => t.Group).Include(t => t.Supervisor);
                    return View(await applicationDbContext.ToListAsync());
                }
            }
            else
            {
                ViewData["TopicsIndexHeading"] = _sharedLocalizer["Index heading for: " + groupName];
                
                //Shows edit button only for not selectable groups
                if (!group.Selectable)
                {
                    ViewData["TopicsIndexGroupId"] = group.GroupId;
                    ViewData["TopicsIndexHeading"] = _sharedLocalizer["Group of topics for:"].Value + " " + groupName;
                }

                ViewData["TopicsIndexGroupName"] = group.Name;
                
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
            
            //Get cuurent user
            var user = await _context.Users.FirstOrDefaultAsync(x => x.UserName.Equals(User.Identity.Name));
            @ViewData["UserInterestHide"] = null;
            
            if (user is null)
            {
                //User is not logged in -> don't show interest button
                @ViewData["UserInterestHide"] = true;
            }
            else if (await _context.UserInterestedTopics.AnyAsync(x => x.UserId.Equals(user.Id) && x.TopicId.Equals(id)))
            {
                @ViewData["UserInterestClass"] = "interested";
                @ViewData["UserInterestString"] = _sharedLocalizer["Remove interest"];
            }
            else
            {

                @ViewData["UserInterestClass"] = "not-interested";
                @ViewData["UserInterestString"] = _sharedLocalizer["Add interest"];
            }

            return View(topic);
        }

        // GET: Topic/Create
        public async Task<IActionResult> Create(string? groupName = null)
        {
            SelectList? groupSelectList = null;
            
            if (groupName == null)
            {
                ViewData["TopicCreateDisplayProgrammes"] = false;
                groupSelectList = new SelectList(_context.Groups.Where(x => x.Selectable), "GroupId", "Name");
            }
            else
            {
                //Creates selectList for groups when a valid groupName is provided
                var groupProvided = _context.Groups.First(x => x.Name.ToLower().Equals(groupName.ToLower()));
                if (!groupProvided.Selectable || groupProvided.Name.Equals("Unassigned"))
                {
                    ViewData["TopicCreateDisplayProgrammes"] = false;
                    groupSelectList = new SelectList(_context.Groups.Where(x => x.Selectable || x.Equals(groupProvided)),
                        "GroupId", "Name", groupProvided.GroupId);
                }
                else
                {
                    //Set ViewData for recommended progammes
                    ViewData["TopicCreateDisplayProgrammes"] = true;
                    if (groupProvided.Name.Equals("Bachelor"))
                    {
                        ViewData["TopicCreateProgrammes"] =
                            await _context.Programmes.Where(x => x.Type == ProgramType.Bachelor).ToListAsync();
                    }
                    else
                    {
                        ViewData["TopicCreateProgrammes"] =
                            await _context.Programmes.Where(x => x.Type == ProgramType.Master).ToListAsync();
                    }

                    groupSelectList = new SelectList(_context.Groups.Where(x => x.Selectable), 
                        "GroupId", "Name", groupProvided.GroupId);
                }
            }

            ViewData["Groups"] = groupSelectList;
            var studentRole = _context.Roles.First(x => x.Name != null && x.Name.Equals("Student"));
            ViewData["Students"] =
                new SelectList(
                    _context.Users.Where(user =>
                        _context.UserRoles.Any(ur => ur.UserId == user.Id && ur.RoleId == studentRole.Id)), "Id",
                    "Email");
            
            //Selects all users with role Teacher
            var user = _context.Users.First(x => User.Identity != null && x.UserName!.Equals(User.Identity.Name));
            var teacherRole = _context.Roles.First(r => r.Name != null && r.Name.Equals("Teacher"));
            ViewData["Supervisors"] =
                new SelectList(
                    _context.Users.Where(x =>
                        _context.UserRoles.Any(y => y.UserId == x.Id && y.RoleId == teacherRole.Id)), "Id", "Email", user.Id);
            
            return View();
        }

        // POST: Topic/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("TopicId,Name,DescriptionShort,DescriptionLong,Visible,CreatorId,SupervisorId,AssignedId,GroupId")] Topic topic, string[] programmes)
        {
            
            topic.CreatorId = _context.Users.First(x => User.Identity != null && x.Email != null && x.Email.Equals(User.Identity.Name)).Id;
            _context.Add(topic);
            await _context.SaveChangesAsync();
            
            //Logic for recommended programmes
            List<Programme>? programmeObjects = null;
            //Get group from topic.groupId
            var group = await _context.Groups.FirstAsync(x => x.GroupId.Equals(topic.GroupId));
            
            if (group.Name.Equals("Bachelor"))
            {
                 programmeObjects =
                    await _context.Programmes.Where(x => x.Type == ProgramType.Bachelor).ToListAsync();
            }
            else if(group.Name.Equals("Master"))
            {
                 programmeObjects =
                    await _context.Programmes.Where(x => x.Type == ProgramType.Master).ToListAsync();
            }

            if (programmeObjects != null)
            {
                foreach (var programme in programmes)
                {
                    _context.TopicRecommendedProgrammes.Add(new()
                    {
                        TopicId = topic.TopicId,
                        ProgramId = programmeObjects.First(x => x.Name.Equals(programme)).ProgrammeId
                    });
                }
            }

            await _context.SaveChangesAsync();
           
            return RedirectToAction(nameof(Index));
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
            ViewData["AssignedId"] = new SelectList(_context.Users, "Id", "Email", topic.AssignedId);
            ViewData["CreatorId"] = new SelectList(_context.Users, "Id", "Email", topic.CreatorId);
            ViewData["GroupId"] = new SelectList(_context.Groups.Where(x=>x.Selectable || topic.GroupId.Equals(x.GroupId)), "GroupId", "Name", topic.GroupId);
            ViewData["SupervisorId"] = new SelectList(_context.Users, "Id", "Email", topic.SupervisorId);
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

        public async Task<JsonResult> Interest(int? topicId)
        {
            if (topicId == null) throw new Exception("topicId should be provided");
            
            //Current user
            var user = await _context.Users.FirstOrDefaultAsync(x => x.UserName.Equals(User.Identity.Name));
            
            //Topic
            var topic = await _context.Topics.FirstAsync(x => x.TopicId.Equals(topicId));

            if (await _context.UserInterestedTopics.AnyAsync(x => x.UserId.Equals(user.Id) && x.TopicId.Equals(topic.TopicId)))
            {
                _context.UserInterestedTopics.Remove(_context.UserInterestedTopics.First(x => x.UserId.Equals(user.Id) && x.TopicId.Equals(topic.TopicId)));
            }
            else
            {
                _context.UserInterestedTopics.Add(new UserInterestedTopic()
                {
                    User = user,
                    Topic = topic
                });
            }
            
            await _context.SaveChangesAsync();
            return Json(true);
        }

        public async Task<IActionResult> AddComment(int id, string text, bool anonymous, int? parentId = null)
        {
            //get current user
            var user = await _context.Users.FirstOrDefaultAsync(x => x.UserName.Equals(User.Identity.Name));
            
            var c = new Comment();
            c.TopicId = id;
            c.Author = user;
            c.CreatedAt = DateTime.Now;
            c.Text = text;
            c.ParentCommentId = parentId;
            c.Anonymous = anonymous;
            _context.Comments.Add(c);
            await _context.SaveChangesAsync();

            return RedirectToAction("Details", new { id = id });
        }
    }
}
