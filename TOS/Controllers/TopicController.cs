using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Localization;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using NuGet.Packaging;
using TOS.Data;
using TOS.Models;
using TOS.Resources;

namespace TOS.Controllers
{
    public class TopicController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IHtmlLocalizer<SharedResource> _sharedLocalizer;
        private readonly IWebHostEnvironment _env;

        public TopicController(ApplicationDbContext context,  IHtmlLocalizer<SharedResource> sharedLocalizer, IWebHostEnvironment env)
        {
            _context = context;
            _sharedLocalizer = sharedLocalizer;
            _env = env;
        }

        // GET: Topic
        public async Task<IActionResult> Index(string groupName = "MyTopics", string programmeName = "", string searchString = "", bool showTakenTopics = false, string orderBy = "Supervisor", bool showHidden = false)
        {
            var user = await _context.Users.FirstOrDefaultAsync(x => x.UserName!.Equals(User.Identity!.Name));
            //If user is not logged in -> show bachelor topics as default
            if (groupName is "MyTopics" && user is null) groupName = "Bachelor";
            
            var topicsToShow = new List<Topic>();
            Group? group = null;
            
            ViewData["customGroup"] = true;
            if(groupName is "Bachelor"  or "Master" or "Unassigned")
                ViewData["customGroup"] = false;
           
            ViewData["topicsIndexGroupName"] = groupName;
            ViewData["showTakenTopics"] = showTakenTopics;
            ViewData["selectedProgramme"] = programmeName;
            ViewData["searchString"] = searchString;
            ViewData["orderBy"] = orderBy;
            ViewData["showHidden"] = showHidden;
      
            
            if (groupName is not "MyTopics")
            {
                group = await _context.Groups.FirstAsync(x => x.NameEng.Equals(groupName));
                topicsToShow = await _context.Topics.Where(x => x.Group.Equals(group)).ToListAsync();
                
                //Used for showing edit button for custom groups
                if(groupName != "Bachelor" && groupName != "Master")
                    ViewData["topicsIndexGroupId"] = group.GroupId;
               
            }
            else if (groupName is "MyTopics")
            {
                topicsToShow = await _context.Topics.Where(x=>
                        x.Creator.Equals(user) || 
                        ( x.Supervisor != null && x.Supervisor.Equals(user)) ||
                        ( x.AssignedStudent != null && x.AssignedStudent.Equals(user)) ||
                        x.UserInterestedTopics.Any(y => y.User.Equals(user)))
                    .ToListAsync();
            }

            //Shows only topics with visible = true, based on parameter
            if (!showHidden)
            {
                topicsToShow = topicsToShow.Where(x => x.Visible).ToList();
            }
            
            if (searchString.Length > 3)
            {
                searchString = searchString.ToLower();
                topicsToShow = topicsToShow.Where(x =>
                    x.Name.ToLower().Contains(searchString) || x.NameEng.ToLower().Contains(searchString) ||
                    (x.Supervisor != null && (x.Supervisor.FirstName!.ToLower().Contains(searchString) ||
                                              x.Supervisor.LastName!.ToLower().Contains(searchString))))
                    .ToList();
            }
            
            if (!showTakenTopics)
            {
                topicsToShow = topicsToShow.Where(x => x.AssignedStudent == null).ToList();
            }

            if (programmeName.Length > 0)
            {
                var programme = _context.Programmes.First(x => x.NameEng.Equals(programmeName));
                topicsToShow =
                    topicsToShow.Where(x => x.TopicRecommendedPrograms.Any(y => y.Programme.Equals(programme))).ToList();
                
                ViewData["SelectedProgramme"] = programmeName;
            }

            ViewData["programmes"] = groupName switch
            {
                "Bachelor" => _context.Programmes.Where(x => x.Type == ProgramType.Bachelor).ToList(),
                "Master" => _context.Programmes.Where(x => x.Type == ProgramType.Master).ToList(),
                _ => new List<Programme>()
            };

            switch (orderBy)
            {
                case "Supervisor":
                    topicsToShow = topicsToShow.OrderBy(x => x.Supervisor?.LastName).ThenBy(x => x.Supervisor?.FirstName).ThenBy(x=>x.Name).ToList();
                    break;
                case "Name":
                    topicsToShow = CultureInfo.CurrentCulture.Name.Contains("cz") ? topicsToShow.OrderBy(x => x.Name).ToList() : topicsToShow.OrderBy(x => x.NameEng).ToList();
                    break;
                case "Interest":
                    topicsToShow = topicsToShow.OrderByDescending(x => x.UserInterestedTopics.Count).ToList();
                    break;
            }

            return View(topicsToShow);
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
            var groupProvided = _context.Groups.FirstOrDefault(x => x.NameEng.ToLower().Equals(groupName.ToLower()));

            //For custom groups, allow only the group provided
            if (groupProvided != null && !groupProvided.Selectable)
                ViewData["Groups"] = new List<Group> {groupProvided};
            else //Else allow any selectable group
                ViewData["Groups"] = await _context.Groups.Where(x => x.Selectable).ToListAsync();
            
            ViewData["Programmes"] = await _context.Programmes.Where(x => x.Active).ToListAsync();
            
            ViewData["UsersToAssign"] = new SelectList(GetUsersWithRole("AssignedToTopic"), "Id", "Email");
            
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            ViewData["UsersToSupervise"] = new SelectList(GetUsersWithRole("SupervisorToTopic"), "Id", "Email", userId);
            
            return View();
        }

        // POST: Topic/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("TopicId,Name,NameEng,DescriptionShort,DescriptionShortEng,DescriptionLong,DescriptionLongEng,Visible,CreatorId,SupervisorId,AssignedId,GroupId")] Topic topic, int[] programmes, List<IFormFile> files)
        {
            return await TopicChange(topic, programmes, files, true);
        }

        // GET: Topic/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            Topic? topic;
            if (id == null || (topic = await _context.Topics.FindAsync(id)) == null)
            {
                return NotFound();
            }
            
            ViewData["UsersToAssign"] = new SelectList(GetUsersWithRole("AssignedToTopic"), "Id", "Email", topic.AssignedId);
            if (topic.Group.Selectable)
            {
                ViewData["Group"] = new SelectList(_context.Groups.Where(x=>x.Selectable), "GroupId", "Name", topic.GroupId);
            }
            else
            {
                ViewData["Group"] = new SelectList(new List<Group> {topic.Group}, "GroupId", "Name", topic.GroupId);
            }
            
            ViewData["UsersToSupervise"] = new SelectList(GetUsersWithRole("SupervisorToTopic"), "Id", "Email", topic.SupervisorId);

            var programmes = new HashSet<Programme>();
            foreach (var programme in _context.Programmes.Where(x=>x.Active).ToList())
            {
                if (topic.TopicRecommendedPrograms.Any(x => x.ProgramId.Equals(programme.ProgrammeId)))
                {
                    programme.Selected = true;
                }
                programmes.Add(programme);
            }
            ViewData["Programmes"] = programmes;


            return View(topic);
        }
        
        // POST: Topic/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("TopicId,Name,DescriptionShort,DescriptionLong,Visible,CreatorId,SupervisorId,AssignedId,GroupId")] Topic topic, int[] programmes, List<IFormFile> files)
        {
            return await TopicChange(topic, programmes, files);
        }

        private async Task<IActionResult> TopicChange(Topic topic, IEnumerable<int> programmesId, List<IFormFile> files, bool isNew = false)
        {
            var user = await _context.Users.FirstAsync(x => User.Identity != null && x.UserName!.Equals(User.Identity.Name));

            //Set eng fields to czech if not provided
            if(topic.NameEng is "" or null) topic.NameEng = topic.Name;
            if(topic.DescriptionShortEng is "" or null) topic.DescriptionShortEng = topic.DescriptionShort;
            if (topic.DescriptionLongEng is "" or null) topic.DescriptionLongEng = topic.DescriptionLong;
            
            if (isNew)
            {
                topic.CreatorId = user.Id;
                topic.Creator = user;
                //Create topic
                _context.Add(topic);
            }
            else
            {
                //Why is this needed?
                topic.Creator = await _context.Users.FirstAsync(x => x.Id.Equals(topic.CreatorId));
                //Update topic
                _context.Update(topic);
            }
            await _context.SaveChangesAsync();
          
            //Delete all recommended programmes
            _context.TopicRecommendedProgrammes.RemoveRange(_context.TopicRecommendedProgrammes.Where(x=>x.TopicId.Equals(topic.TopicId)));
            //Re-add new recommended programmes
            foreach (var programme in programmesId)
            {
                _context.TopicRecommendedProgrammes.Add(new TopicRecommendedProgramme
                {
                    TopicId = topic.TopicId,
                    ProgramId = programme
                });
            }
            await _context.SaveChangesAsync();

            //Create uploaded files
            await CreateFiles(topic, user, files);
            
            return RedirectToAction(nameof(Index));
        }
       
        public async Task<IActionResult> Delete(int? id)
        {
            var topic = await _context.Topics.FirstAsync(x => x.TopicId.Equals(id));

            _context.Topics.Remove(topic);
            await _context.SaveChangesAsync();
            
            return RedirectToAction("Index");
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

        public async Task<bool> CreateFiles(Topic topic, ApplicationUser user, List<IFormFile> files)
        {
            foreach (var file in files)
            {
                var filePath = Path.Combine(_env.WebRootPath, "files", topic.TopicId.ToString(), file.FileName);
                var fileInfo = new FileInfo(filePath);
                
                //If topic has attachment with same file name -> skip current file
                if (fileInfo.Exists)
                {
                    if (_context.Attachments.Any(x => x.TopicId.Equals(topic.TopicId) && x.Name.Equals(file.FileName)))
                    {
                        //TODO: Show pop-up message, that file with same name already exists
                        continue;
                    }
                    fileInfo.Delete();
                }
                
                //Create missing directories
                Directory.CreateDirectory(Path.GetDirectoryName(filePath) ?? string.Empty);

                //Create file
                await using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }
                
                //Add record to database
                _context.Attachments.Add(new Attachment()
                {
                    CreatorId = user.Id,
                    TopicId = topic.TopicId,
                    Name = file.FileName
                });
            }
            
            await _context.SaveChangesAsync();
            
            return true;
        }
        
        public async Task<IActionResult> DeleteAttachment(int attachmentId, int topicId)
        {
            //Delete the file from server
            var file = new FileInfo(Path.Combine(_env.WebRootPath, "files", topicId.ToString(), _context.Attachments.First(x => x.AttachmentId.Equals(attachmentId)).Name));
            if (file.Exists)
            {
                file.Delete();
            }
            
            //Update database
            _context.Attachments.Remove(_context.Attachments.FirstOrDefault(x => x.AttachmentId.Equals(attachmentId)));
            await _context.SaveChangesAsync();

            return RedirectToAction("Edit", new { id = topicId });
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
        public async Task<IActionResult> DeleteComment(int commentId, int topicId)
        {
            var comment = await _context.Comments.FirstOrDefaultAsync(x => x.CommentId.Equals(commentId));

            if (comment != null)
            {
                if (comment.Replies.Count > 0)
                {
                    comment.Text = "Deleted comment";
                    comment.Anonymous = true;
                    _context.Comments.Update(comment);
                }
                else
                {
                    _context.Comments.Remove(comment);
                }

                await _context.SaveChangesAsync();
            }
            
            return RedirectToAction("Details", new { id = topicId });
        }

        private List<ApplicationUser> GetUsersWithRole(string role)
        {
            var roleId = _context.Roles.FirstOrDefault(x => x.Name!.ToLower().Equals(role.ToLower()))!.Id;

            List<ApplicationUser> users = new();

            if (roleId != 0)
                users = _context.Users.Where(x => _context.UserRoles.Any(y => y.UserId == x.Id && y.RoleId == roleId))
                    .ToList();

            return users;
        }
    }
}
