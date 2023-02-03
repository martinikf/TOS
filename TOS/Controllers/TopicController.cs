using System;
using System.Collections.Generic;
using System.Globalization;
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
            
            if (groupName != null) group = await _context.Groups.FirstOrDefaultAsync(x => x.NameEng.Equals(groupName));
            
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

                ViewData["TopicsIndexGroupName"] = group.NameEng;
                
                var applicationDbContext = _context.Topics.Where(x => x.Group.NameEng.Equals(groupName) && x.Visible)
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
            
            //If culture is not cz -> display english group names; Used in SelectList
            string nameString = "NameEng";
            if (CultureInfo.CurrentCulture.Name.Contains("cz"))
            {
                nameString = "Name";
            }
            
            if (groupName == null)
            {
                groupSelectList = new SelectList(_context.Groups.Where(x => x.Selectable), "GroupId", nameString);
            }
            else
            {
                //Creates selectList for groups when a valid groupName is provided
                var groupProvided = _context.Groups.FirstOrDefault(x => x.NameEng.ToLower().Equals(groupName.ToLower()));
                if (groupProvided == null) throw new Exception();
                
                if (!groupProvided.Selectable)
                {
                    //Block for subject
                    groupSelectList = new SelectList(_context.Groups.Where(x => x.Equals(groupProvided)),
                        "GroupId", nameString, groupProvided.GroupId);
                }
                else
                {
                    groupSelectList = new SelectList(_context.Groups.Where(x => x.Selectable), 
                        "GroupId", nameString, groupProvided.GroupId);
                   
                }
            }

            ViewData["Programmes"] = await _context.Programmes.Where(x => x.Active).ToListAsync();
            ViewData["Groups"] = groupSelectList;
            var studentRole = await _context.Roles.FirstAsync(x => x.Name != null && x.Name.Equals("Student"));
            ViewData["Students"] =
                new SelectList(
                    _context.Users.Where(user =>
                        _context.UserRoles.Any(ur => ur.UserId == user.Id && ur.RoleId == studentRole.Id)), "Id",
                    "Email");
            
            //Selects all users with role Teacher
            var user = await _context.Users.FirstAsync(x => User.Identity != null && x.UserName!.Equals(User.Identity.Name));
            var teacherRole = await _context.Roles.FirstAsync(r => r.Name != null && r.Name.Equals("Teacher"));
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
        public async Task<IActionResult> Create([Bind("TopicId,Name,NameEng,DescriptionShort,DescriptionShortEng,DescriptionLong,DescriptionLongEng,Visible,CreatorId,SupervisorId,AssignedId,GroupId")] Topic topic, int[] programmes)
        {
            //Set eng fields to czech if not provided
            if(topic.NameEng == "") topic.NameEng = topic.Name;
            if(topic.DescriptionShortEng == "") topic.DescriptionShortEng = topic.DescriptionShort;
            if(topic.DescriptionLongEng == "") topic.DescriptionLongEng = topic.DescriptionLong;
            
            topic.CreatorId = _context.Users.First(x => User.Identity != null && x.Email != null && x.Email.Equals(User.Identity.Name)).Id;
            _context.Add(topic);
            await _context.SaveChangesAsync();
            
            //Delete all recommended programmes
            _context.TopicRecommendedProgrammes.RemoveRange(_context.TopicRecommendedProgrammes.Where(x=>x.TopicId.Equals(topic.TopicId)));
            //Re-add new recommended programmes
            foreach (var programme in programmes)
            {
                _context.TopicRecommendedProgrammes.Add(new()
                {
                    TopicId = topic.TopicId,
                    ProgramId = programme
                });
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


            ViewData["Programmes"] = await _context.Programmes
                .Where(x => x.Active)
                .Select(p => new Programme
                {
                    ProgrammeId = p.ProgrammeId,
                    Name = p.Name,
                    Active = p.Active,
                    Type = p.Type,
                    NameEng = p.NameEng,
                    Selected = _context.TopicRecommendedProgrammes
                        .Any(x => x.TopicId.Equals(topic.TopicId) && x.ProgramId.Equals(p.ProgrammeId))
                })
                .ToListAsync();


            return View(topic);
        }

        // POST: Topic/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("TopicId,Name,DescriptionShort,DescriptionLong,Visible,CreatorId,SupervisorId,AssignedId,GroupId")] Topic topic, int[] programmes)
        {
            if (id != topic.TopicId)
            {
                return NotFound();
            }
            
            if(topic.NameEng == "") topic.NameEng = topic.Name;
            if(topic.DescriptionShortEng == "") topic.DescriptionShortEng = topic.DescriptionShort;
            if(topic.DescriptionLongEng == "") topic.DescriptionLongEng = topic.DescriptionLong;
      
            _context.Update(topic);
            await _context.SaveChangesAsync();
          
            //Delete all recommended programmes
            _context.TopicRecommendedProgrammes.RemoveRange(_context.TopicRecommendedProgrammes.Where(x=>x.TopicId.Equals(topic.TopicId)));
            //Re-add new recommended programmes
            foreach (var programme in programmes)
            {
                _context.TopicRecommendedProgrammes.Add(new()
                {
                    TopicId = topic.TopicId,
                    ProgramId = programme
                });
            }

            await _context.SaveChangesAsync();
            
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
            c.AuthorId = user.Id;
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
