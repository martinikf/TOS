using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Localization;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TOS.Data;
using TOS.Models;
using TOS.Resources;
using TOS.Services;

namespace TOS.Controllers
{
    public class TopicController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IHtmlLocalizer<SharedResource> _sharedLocalizer;
        private readonly IWebHostEnvironment _env;
        private readonly INotificationManager _notificationManager;

        public TopicController(ApplicationDbContext context, IHtmlLocalizer<SharedResource> sharedLocalizer, IWebHostEnvironment env, INotificationManager notificationManager)
        {
            _context = context;
            _sharedLocalizer = sharedLocalizer;
            _env = env;
            _notificationManager = notificationManager;
        }

        // GET: Topic
        public async Task<IActionResult> Index(string groupName = "MyTopics", string programmeName = "",
            string searchString = "", bool showTakenTopics = false, string orderBy = "Supervisor",
            bool showHidden = false, bool showProposed = false)
        {
            ViewData["topicsIndexGroupName"] = groupName;
            ViewData["showTakenTopics"] = showTakenTopics;
            ViewData["selectedProgramme"] = programmeName;
            ViewData["searchString"] = searchString;
            ViewData["showProposed"] = showProposed;
            if (showProposed)
                orderBy = "Name";
            ViewData["orderBy"] = orderBy;
            ViewData["showHidden"] = showHidden;

            var group = await _context.Groups.FirstAsync(x => x.NameEng.Equals(groupName));
            var topicsToShow = await _context.Topics.Where(x => x.Group.Equals(group) && x.Type==TopicType.Thesis).ToListAsync();

            //Shows only topics with visible = true, based on parameter
            if (!showHidden && !showProposed || !(User.IsInRole("Group") || User.IsInRole("AnyGroup")))
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
                    topicsToShow.Where(x => x.TopicRecommendedPrograms.Any(y => y.Programme.Equals(programme)))
                        .ToList();

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
                    topicsToShow = topicsToShow.OrderBy(x => x.Supervisor?.LastName)
                        .ThenBy(x => x.Supervisor?.FirstName).ThenBy(x => x.Name).ToList();
                    break;
                case "Name":
                    topicsToShow = CultureInfo.CurrentCulture.Name.Contains("cz")
                        ? topicsToShow.OrderBy(x => x.Name).ToList()
                        : topicsToShow.OrderBy(x => x.NameEng).ToList();
                    break;
                case "Interest":
                    topicsToShow = topicsToShow.OrderByDescending(x => x.UserInterestedTopics.Count).ToList();
                    break;
            }

            if (showProposed && User.IsInRole("Group") || User.IsInRole("AnyGroup"))
            {
                topicsToShow = topicsToShow.Where(x => x.Proposed).ToList();
            }

            return View(topicsToShow);
        }

        
        public async Task<IActionResult> Group(string groupName, string searchString = "", bool showTakenTopics = false, bool showHidden = false, bool showProposed = false)
        {
            var group = await _context.Groups.FirstAsync(x => x.NameEng.Equals(groupName));
            
            ViewData["topicsIndexGroupName"] = groupName;
            ViewData["topicsIndexGroupId"] = group.GroupId;
            ViewData["showTakenTopics"] = showTakenTopics;
            ViewData["searchString"] = searchString;
            ViewData["showHidden"] = showHidden;
            ViewData["showProposed"] = showProposed;
            
            var topicsToShow = await _context.Topics.Where(x => x.Group.Equals(group)).ToListAsync();
            
            if (!showHidden && !showProposed || !(User.IsInRole("Topic") || User.IsInRole("AnyTopic")))
            {
                topicsToShow = topicsToShow.Where(x => x.Visible).ToList();
            }
            
            if (searchString.Length > 2)
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
            
            if (showProposed && User.IsInRole("Topic") || User.IsInRole("AnyTopic"))
            {
                topicsToShow = topicsToShow.Where(x => x.Proposed).ToList();
            }
            
            return View(topicsToShow);
        }
        
        public async Task<IActionResult> MyTopics(string searchString = "", bool showHidden = false)
        {
            searchString = searchString.Trim();
            if(searchString.Length < 3)
                searchString = "";
            ViewData["searchString"] = searchString;
               ViewData["showHidden"] = showHidden;
            
            if(User.Identity == null)
                return RedirectToAction("Index", "Home");
            
            var user = await _context.Users.FirstAsync(x => x.UserName!.Equals(User.Identity.Name));

            var topics = await _context.Topics
                .Where(x =>
                    x.CreatorId == user.Id ||
                    x.SupervisorId == user.Id ||
                    x.AssignedId == user.Id ||
                    x.UserInterestedTopics.Any(y => y.UserId == user.Id))
                .Where(x =>
                   (x.Name.ToLower().Contains(searchString.ToLower()) ||
                        x.NameEng.ToLower().Contains(searchString.ToLower()) ||
                        (x.Supervisor != null && (x.Supervisor.FirstName!.ToLower().Contains(searchString.ToLower()) ||
                                                  x.Supervisor.LastName!.ToLower().Contains(searchString.ToLower()))) ||
                        x.DescriptionShort.ToLower().Contains(searchString.ToLower()) ||
                        x.DescriptionShortEng.ToLower().Contains(searchString.ToLower()) ||
                        (x.DescriptionLong != null && x.DescriptionLong.ToLower().Contains(searchString.ToLower())) ||
                        (x.DescriptionLongEng != null && x.DescriptionLongEng.ToLower().Contains(searchString.ToLower())) ||
                        x.TopicRecommendedPrograms.Any(y =>
                            y.Programme.NameEng.ToLower().Contains(searchString.ToLower()) ||
                            y.Programme.Name.ToLower().Contains(searchString.ToLower()))))
                .Where(x=> showHidden? x.Visible : x.Visible || !x.Visible)
                .ToListAsync();

            return View(topics);
        }

        // GET: Topic/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();
            
            var topic = await _context.Topics
                .Include(t => t.AssignedStudent)
                .Include(t => t.Creator)
                .Include(t => t.Group)
                .Include(t => t.Supervisor)
                .FirstAsync(m => m.TopicId == id);
            
            return View(topic);
        }

        [Authorize(Roles = "Topic,AnyTopic")]
        public async Task<IActionResult> Create(string groupName = "Unassigned", TopicType type = TopicType.Thesis)
        {
            var groupProvided = _context.Groups.FirstOrDefault(x => x.NameEng.ToLower().Equals(groupName.ToLower()));

            //For custom groups, allow only the group provided
            if (groupProvided != null && !groupProvided.Selectable && groupName != "Unassigned")
                ViewData["Groups"] = new List<Group> {groupProvided};
            else //Else allow any selectable group
            {
                var gr = await _context.Groups.Where(x => x.Selectable).ToListAsync();
                foreach (var g in gr.Where(x => x.GroupId.Equals(groupProvided?.GroupId)))
                    g.Highlight = true;

                ViewData["Groups"] = gr;
            }
            
            ViewData["TopicType"] = type;

            ViewData["Programmes"] = await _context.Programmes.Where(x => x.Active).ToListAsync();
            ViewData["UsersToAssign"] = new SelectList(GetUsersWithRole("AssignedTopic"), "Id", "Email");

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            ViewData["UsersToSupervise"] = new SelectList(GetUsersWithRole("SuperviseTopic"), "Id", "Email", userId);

            return View();
        }

        // POST: Topic/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Topic,AnyTopic")]
        public async Task<IActionResult> Create([Bind("TopicId,Name,NameEng,DescriptionShort,DescriptionShortEng,DescriptionLong,DescriptionLongEng,Visible,CreatorId,SupervisorId,AssignedId,GroupId,Type")] Topic topic, int[] programmes, List<IFormFile> files)
        {
            await TopicChange(topic, programmes, files, true);
            return RedirectToAction(nameof(Details), new {id = topic.TopicId});
        }

        [Authorize(Roles = "ProposeTopic")]
        public async Task<IActionResult> Propose(string? groupName = "Unassigned", TopicType type = TopicType.Thesis)
        {
            var group = _context.Groups.First(x => x.NameEng.Equals(groupName));
            if (group.Selectable)
            {
                var list = await _context.Groups.Where(x => x.Selectable).ToListAsync();
                foreach (var g in list.Where(g => g.GroupId.Equals(group.GroupId)))
                    g.Highlight = true;

                ViewData["Groups"] = list;
            }
            else
            {
                ViewData["Groups"] = await _context.Groups.Where(x => x.GroupId.Equals(group.GroupId)).ToListAsync();
            }
            
            ViewData["TopicType"] = type;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "ProposeTopic")]
        public async Task<IActionResult> Propose([Bind("TopicId,Name,NameEng,DescriptionShort,DescriptionShortEng,DescriptionLong,DescriptionLongEng,CreatorId,GroupId,Type")] Topic topic, List<IFormFile> files)
        {
            if (topic.NameEng is "" or null) topic.NameEng = topic.Name;
            if (topic.DescriptionShortEng is "" or null) topic.DescriptionShortEng = topic.DescriptionShort;
            if (topic.DescriptionLongEng is "" or null) topic.DescriptionLongEng = topic.DescriptionLong;

            if (topic.GroupId == -1)
            {
                topic.Group = await _context.Groups.FirstAsync(x => x.NameEng.Equals("Unassigned"));
                topic.GroupId = topic.Group.GroupId;
            }

            topic.Creator = await _context.Users.FirstAsync(x => x.UserName!.Equals(User.Identity!.Name));
            topic.Visible = false;
            topic.Proposed = true;

            _context.Add(topic);
            await _context.SaveChangesAsync();

            //Save files
            await CreateFiles(topic, topic.Creator, files);

            return RedirectToAction(nameof(Details), new {id = topic.TopicId});
        }


        [Authorize(Roles = "Topic,AnyTopic,ProposeTopic")]
        public async Task<IActionResult> Edit(int? id)
        {
            Topic? topic;
            if (id == null || (topic = await _context.Topics.FindAsync(id)) == null)
            {
                return NotFound();
            }

            ViewData["UsersToAssign"] = new SelectList(GetUsersWithRole("AssignedTopic"), "Id", "Email", topic.AssignedId);
            if (topic.Group.Selectable || topic.Group.NameEng.Equals("Unassigned"))
            {
                ViewData["Group"] = new SelectList(_context.Groups.Where(x => x.Selectable), "GroupId", "Name",
                    topic.GroupId);
            }
            else
            {
                ViewData["Group"] = new SelectList(new List<Group> {topic.Group}, "GroupId", "Name", topic.GroupId);
            }

            ViewData["UsersToSupervise"] = new SelectList(GetUsersWithRole("SuperviseTopic"), "Id", "Email", topic.SupervisorId);
            
            ViewData["TopicType"] = topic.Type;

            var programmes = new HashSet<Programme>();
            foreach (var programme in _context.Programmes.Where(x => x.Active).ToList())
            {
                if (topic.TopicRecommendedPrograms.Any(x => x.ProgramId.Equals(programme.ProgrammeId)))
                {
                    programme.Selected = true;
                }

                programmes.Add(programme);
            }

            ViewData["Programmes"] = programmes;
            ViewData["OldAssigned"] = topic.AssignedId;


            return View(topic);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Topic,AnyTopic,ProposeTopic")]
        public async Task<IActionResult> Edit(int id, [Bind("TopicId,Name,NameEng,DescriptionShort,DescriptionShortEng,DescriptionLong,DescriptionLongEng,Visible,CreatorId,SupervisorId,AssignedId,GroupId,Type,Proposed")] Topic topic, int[] programmes, List<IFormFile> files, int oldAssigned)
        {
            var user = await _context.Users.FirstAsync(x => User.Identity != null && x.UserName!.Equals(User.Identity.Name));
            
            var canEdit = User.IsInRole("AnyTopic");
            //User can edit topics he created or supervise
            if (User.IsInRole("Topic") && topic.CreatorId == user.Id || topic.SupervisorId == user.Id || topic.Proposed)
                canEdit = true;
            if(User.IsInRole("ProposeTopic") && topic.Proposed && topic.CreatorId == user.Id)
                canEdit = true;
            if (!canEdit)
                return Forbid();

            if (await TopicChange(topic, programmes, files, false, oldAssigned))
            {
                return RedirectToAction(nameof(Details), new {id = topic.TopicId});
            }
            return RedirectToAction("Edit", new {id = topic.TopicId});
        }

        private async Task<bool> TopicChange(Topic topic, IEnumerable<int> programmesId, List<IFormFile> files, bool isNew = false, int oldAssigned = -1)
        {
            var user = await _context.Users.FirstAsync(x =>
                User.Identity != null && x.UserName!.Equals(User.Identity.Name));

            //Set eng fields to czech if not provided
            if (topic.NameEng is "" or null) topic.NameEng = topic.Name;
            if (topic.DescriptionShortEng is "" or null) topic.DescriptionShortEng = topic.DescriptionShort;
            if (topic.DescriptionLongEng is "" or null) topic.DescriptionLongEng = topic.DescriptionLong;

            if (isNew)
            {
                topic.CreatorId = user.Id;
                topic.Creator = user;
                //Create topic
                _context.Add(topic);
                await _context.SaveChangesAsync();
            }
            else
            {
                //Update topic
                _context.Topics.Update(topic);
                await _context.SaveChangesAsync();

                topic = _context.Topics
                    .Include(x => x.UserInterestedTopics)
                    .Include(x=>x.Creator)
                    .Include(x=>x.Supervisor)
                    .Include(x=>x.AssignedStudent)
                    .First(x=>x.TopicId.Equals(topic.TopicId));
                
                //Notify users
                if (topic.Proposed && topic.SupervisorId > 0)
                {
                    if (topic.GroupId == _context.Groups.First(x => x.NameEng.Equals("Unassigned")).GroupId)
                    {
                        //When adopting topic from external, supervisor and group must be changed at once
                        return false;
                    }
                    topic.Proposed = true;
                    await _notificationManager.TopicAdopted(topic, CallbackDetailsUrl(topic.TopicId));
                }
                else if(topic.AssignedId != null && oldAssigned != topic.AssignedId)
                {
                    await _notificationManager.TopicAssigned(topic, user, CallbackDetailsUrl(topic.TopicId));
                }
                else
                {
                    await _notificationManager.TopicEdit(topic, user, CallbackDetailsUrl(topic.TopicId));
                }
            }

            //Delete all recommended programmes
            _context.TopicRecommendedProgrammes.RemoveRange(
                _context.TopicRecommendedProgrammes.Where(x => x.TopicId.Equals(topic.TopicId)));
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

            return true;
        }

        [Authorize(Roles = "Topic,AnyTopic,ProposeTopic")]
        public async Task<IActionResult> Delete(int? id)
        {
            var topic = await _context.Topics.FirstAsync(x => x.TopicId.Equals(id));
            var groupName = topic.Group.NameEng;

            var user = await _context.Users.FirstAsync(x => User.Identity != null && x.UserName!.Equals(User.Identity.Name));
            var canEdit = User.IsInRole("AnyTopic");
            
            //User can edit topics he created or supervise
            if (User.IsInRole("Topic") && topic.CreatorId == user.Id || topic.SupervisorId == user.Id || topic.Proposed)
                canEdit = true;
            if(User.IsInRole("ProposeTopic") && topic.Proposed && topic.CreatorId == user.Id)
                canEdit = true;

            if (!canEdit)
                return Forbid();
            

            _context.Topics.Remove(topic);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index), new { groupName });
        }

        [Authorize(Roles = "InterestTopic")]
        public async Task<JsonResult> Interest(int topicId)
        {
            //Current user
            var user = await _context.Users.FirstAsync(x => x.UserName!.Equals(User.Identity!.Name));

            //Topic
            var topic = await _context.Topics.FirstAsync(x => x.TopicId.Equals(topicId));

            if (await _context.UserInterestedTopics.AnyAsync(x => x.UserId.Equals(user.Id) && x.TopicId.Equals(topic.TopicId)))
            {
                _context.UserInterestedTopics
                    .Remove(_context.UserInterestedTopics
                        .First(x => x.UserId.Equals(user.Id) && x.TopicId.Equals(topic.TopicId)));
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

        [Authorize(Roles = "Attachment,AnyAttachment")]
        public async Task CreateFiles(Topic topic, ApplicationUser user, List<IFormFile> files)
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
        }

        [Authorize(Roles = "Attachment,AnyAttachment")]
        public async Task<IActionResult> DeleteAttachment(int attachmentId, int topicId)
        {
            if (User.IsInRole("Topic") && !User.IsInRole("AnyTopic"))
            {
                var user = await _context.Users.FirstAsync(x =>
                    User.Identity != null && x.UserName!.Equals(User.Identity.Name));
                var topic = await _context.Topics.FirstAsync(x => x.TopicId.Equals(topicId));
                var attachment = await _context.Attachments.FirstAsync(x => x.AttachmentId.Equals(attachmentId));
                if (topic.CreatorId != user.Id && topic.SupervisorId != user.Id && attachment.Creator.Id != user.Id)
                    return Forbid();
            }

            //Delete the file from server
            var file = new FileInfo(Path.Combine(_env.WebRootPath, "files", topicId.ToString(),
                _context.Attachments.First(x => x.AttachmentId.Equals(attachmentId)).Name));
            if (file.Exists)
            {
                file.Delete();
            }

            //Update database
            _context.Attachments.Remove(_context.Attachments.First(x => x.AttachmentId.Equals(attachmentId)));
            await _context.SaveChangesAsync();

            return RedirectToAction("Edit", new {id = topicId});
        }

        [Authorize(Roles = "Comment,AnyComment")]
        public async Task<IActionResult> AddComment(int id, string text, bool anonymous, int? parentId = null)
        {
            //get current user
            var user = await _context.Users.FirstAsync(x => x.UserName!.Equals(User.Identity!.Name));

            var c = new Comment
            {
                TopicId = id,
                Topic = _context.Topics.First(x=>x.TopicId == id),
                AuthorId = user.Id,
                Author = user,
                CreatedAt = DateTime.Now,
                Text = text,
                ParentCommentId = parentId,
                Anonymous = anonymous
            };
            
            _context.Comments.Add(c);
            
            await _context.SaveChangesAsync();

            await _notificationManager.NewComment(c, CallbackDetailsUrl(id));

            return RedirectToAction("Details", new { id });
        }

        [Authorize(Roles = "Comment,AnyComment")]
        public async Task<IActionResult> DeleteComment(int commentId, int topicId)
        {
            var comment = await _context.Comments.FirstOrDefaultAsync(x => x.CommentId.Equals(commentId));

            if (comment != null)
            {
                if (User.IsInRole("DeleteComment") && !User.IsInRole("DeleteAnyComment"))
                {
                    var user = await _context.Users.FirstAsync(x =>
                        User.Identity != null && x.UserName!.Equals(User.Identity.Name));
                    if (comment.AuthorId != user.Id)
                        return Forbid();
                }

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

            return RedirectToAction("Details", new {id = topicId});
        }

        private IEnumerable<ApplicationUser> GetUsersWithRole(string role)
        {
            var roleId = _context.Roles.FirstOrDefault(x => x.Name!.ToLower().Equals(role.ToLower()))!.Id;

            List<ApplicationUser> users = new();

            if (roleId != 0)
                users = _context.Users.Where(x => _context.UserRoles.Any(y => y.UserId == x.Id && y.RoleId == roleId))
                    .ToList();

            return users;
        }

        private string CallbackDetailsUrl(int id)
        {
            var url =  "https://" + HttpContext.Request.Host + $"/Topic/Details/{id}";
            return url;
        }
    }
}