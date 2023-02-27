using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TOS.Data;
using TOS.Models;
using TOS.Services;
using TOS.ViewModels;

namespace TOS.Controllers
{
    public class TopicController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly INotificationManager _notificationManager;

        public TopicController(ApplicationDbContext context, IWebHostEnvironment env, INotificationManager notificationManager)
        {
            _context = context;
            _env = env;
            _notificationManager = notificationManager;
        }

        // GET: Topic
        public async Task<IActionResult> Index(string groupName, string programmeName = "",
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
                        x.Name.ToLower().Contains(searchString) || x.NameEng!.ToLower().Contains(searchString) ||
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
                var programme = _context.Programmes.First(x => x.NameEng!.Equals(programmeName));
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
                    //Solved in view
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

            if (showProposed && (User.IsInRole("Group") || User.IsInRole("AnyGroup")))
            {
                topicsToShow = topicsToShow.Where(x => x.Proposed).ToList();
            }

            var vm = new GroupViewModel();
            vm.Topics = topicsToShow;
            vm.Group = group;

            return View(vm);
        }
        
        public async Task<IActionResult> Group(int groupId, string searchString = "", bool showTakenTopics = false, bool showHidden = false, bool showProposed = false)
        {
            var group = await _context.Groups.FirstAsync(x => x.GroupId == groupId);

            ViewData["topicsIndexGroupName"] = group.NameEng;
            ViewData["topicsIndexGroupId"] = group.GroupId;
            ViewData["creatorUsername"] = group.Creator.UserName;
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
                        x.Name.ToLower().Contains(searchString) || x.NameEng!.ToLower().Contains(searchString) ||
                        (x.Supervisor != null && (x.Supervisor.FirstName!.ToLower().Contains(searchString) ||
                                                  x.Supervisor.LastName!.ToLower().Contains(searchString))))
                    .ToList();
            }
            
            if (!showTakenTopics)
            {
                topicsToShow = topicsToShow.Where(x => x.AssignedStudent == null).ToList();
            }
            
            if (showProposed && (User.IsInRole("Topic") || User.IsInRole("AnyTopic")))
            {
                topicsToShow = topicsToShow.Where(x => x.Proposed).ToList();
            }

            var vm = new GroupViewModel();
            vm.Topics = topicsToShow;
            vm.Group = group;
            
            return View(vm);
        }

        public async Task<IActionResult> Unassigned()
        {
            var topics = await _context.Topics.Where(x => x.Group.NameEng == "Unassigned").ToListAsync();
            
            return View(topics);
        }
        
        public async Task<IActionResult> MyTopics(string searchString = "", bool showHidden = false, bool showProposed = false)
        {
            searchString = searchString.Trim();
            if(searchString.Length < 3)
                searchString = "";
            else
                searchString = searchString.ToLower();
            ViewData["searchString"] = searchString;
            ViewData["showHidden"] = showHidden;
            ViewData["showProposed"] = showProposed;
            
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
                   x.Name.ToLower().Contains(searchString) ||
                        x.NameEng!.ToLower().Contains(searchString) ||
                        (x.Supervisor != null && (x.Supervisor.FirstName!.ToLower().Contains(searchString) ||
                                                  x.Supervisor.LastName!.ToLower().Contains(searchString))) ||
                        x.DescriptionShort.ToLower().Contains(searchString) ||
                        x.DescriptionShortEng!.ToLower().Contains(searchString) ||
                        (x.DescriptionLong != null && x.DescriptionLong.ToLower().Contains(searchString)) ||
                        (x.DescriptionLongEng != null && x.DescriptionLongEng.ToLower().Contains(searchString)) ||
                        x.TopicRecommendedPrograms.Any(y =>
                            y.Programme.NameEng!.ToLower().Contains(searchString) ||
                            y.Programme.Name.ToLower().Contains(searchString)))
                .ToListAsync();
            
            topics = topics.Where(x => showHidden ? x.Visible || !x.Visible : x.Visible || x.Proposed).ToList();
            topics = topics.Where(x => showProposed ? x.Proposed || !x.Proposed : !x.Proposed).ToList();

            //If search string is provided, select all topics from groups that match the searchstring
            if (searchString.Length > 2)
            {
                topics.AddRange(_context.Groups.Where(x=>
                        x.NameEng.ToLower().Contains(searchString) || 
                        x.Name.ToLower().Contains(searchString))
                    .Select(y=>y.Topics)
                    .SelectMany(z=>z));
            }

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
            if (User.IsInRole("Topic") && (topic.CreatorId == user.Id || topic.SupervisorId == user.Id || topic.Proposed))
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
                    Topic = topic,
                    DateTime = DateTime.Now
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
        public async Task<IActionResult> AddComment([Bind("CommentId,Text,Anonymous,ParentCommentId,TopicId")] Comment comment)
        {
            //get current user
            var user = await _context.Users.FirstAsync(x => x.UserName!.Equals(User.Identity!.Name));
            comment.AuthorId = user.Id;
            comment.Author = user;
            comment.CreatedAt = DateTime.Now;
            
            _context.Comments.Add(comment);
            
            await _context.SaveChangesAsync();

            comment = _context.Comments.Include("Topic").First(x => x.CommentId == comment.CommentId);

            await _notificationManager.NewComment(comment, CallbackDetailsUrl(comment.TopicId));

            return RedirectToAction("Details", new { id = comment.TopicId });
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
            return "https://" + HttpContext.Request.Host + $"/Topic/Details/{id}";
        }
    }
}