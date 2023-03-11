using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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

        public async Task<IActionResult> Topics(int groupId)
        {
            var group = await _context.Groups.FindAsync(groupId);
            if (group is null) return RedirectToAction("Index", "Home");
            
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
        
        public async Task<IActionResult> Index(string groupName ="Bachelor", string programmeName = "",
            string searchString = "", bool showTakenTopics = false, string orderBy = "Supervisor",
            bool showHidden = false, bool showOnlyProposed = false)
        {
            if (groupName != "Bachelor" && groupName != "Master")
                return Forbid();

            var group = await GetGroup(groupName);
            if (showOnlyProposed)
                orderBy = "Name";
            
            var vm = new IndexViewModel
            {
                ShowTakenTopics = showTakenTopics,
                ShowProposedTopics = showOnlyProposed,
                ShowHiddenTopics = showHidden,
                SelectedProgramme = programmeName,
                SearchString = searchString,
                OrderBy = orderBy,
                Group = group,
                Programmes = await _context.Programmes.Where(x => x.Type == (ProgramType) Enum.Parse(typeof(ProgramType), groupName)).ToListAsync()
            };

            var topicsToShow = _context.Topics.Where(x => x.Group.Equals(group) && x.Type == TopicType.Thesis);

            topicsToShow = ApplyShowHidden(topicsToShow, showHidden, showOnlyProposed);
            topicsToShow = ApplySearch(topicsToShow, searchString);
            topicsToShow = ApplyShowTaken(topicsToShow, showTakenTopics);
            topicsToShow = ApplyProgramme(topicsToShow, programmeName);
            topicsToShow = ApplyShowOnlyProposed(topicsToShow, showOnlyProposed);
            topicsToShow = ApplySort(topicsToShow, orderBy);

            vm.Topics = await topicsToShow.ToListAsync();
            return View(vm);
        }
        
        public async Task<IActionResult> Group(int groupId, string searchString = "", bool showTakenTopics = false, bool showHidden = false, bool showProposed = false)
        {
            var group = await GetGroup(groupId);
            if (group.NameEng == "Bachelor" || group.NameEng == "Master" || group.NameEng == "Unassigned")
                return Forbid();

            var vm = new GroupViewModel
            {
                ShowTakenTopics = showTakenTopics,
                SearchString = searchString,
                ShowHiddenTopics = showHidden,
                ShowProposedTopics = showProposed,
                Group = group
            };

            var topicsToShow = _context.Topics.Where(x => x.Group.Equals(group));

            topicsToShow = ApplyShowHidden(topicsToShow, showHidden, showProposed);
            topicsToShow = ApplySearch(topicsToShow, searchString);
            topicsToShow = ApplyShowTaken(topicsToShow, showTakenTopics);
            topicsToShow = ApplyShowOnlyProposed(topicsToShow, showProposed);

            vm.Topics = await topicsToShow.ToListAsync();
            var user = await GetUserOrNull();
            
            if (group.Visible || User.IsInRole("Group") || User.IsInRole("AnyGroup") || (user != null && group.CreatorId == user.Id))
                return View(vm);
            
            return Forbid();
        }

        [Authorize(Roles = "Topic,AnyTopic")]
        public async Task<IActionResult> Unassigned()
        {
            var topics = await _context.Topics.Where(x => x.Group.NameEng == "Unassigned").ToListAsync();
            
            return View(topics);
        }
        
        public async Task<IActionResult> MyTopics(string searchString = "", bool showHidden = false, bool showProposed = false)
        {
            var user = await GetUserOrNull();
            if (user == null)
                return RedirectToPage("Login");
            
            var vm = new MyTopicsViewModel
            {
                SearchString = searchString,
                ShowHiddenTopics = showHidden,
                ShowProposedTopics = showProposed
            };

            searchString = searchString.Trim();
            if(searchString.Length < 3)
                searchString = "";
            else
                searchString = searchString.ToLower();

            var topics = _context.Topics
                .Where(x => x.CreatorId == user.Id || x.SupervisorId == user.Id ||
                            x.AssignedId == user.Id || x.UserInterestedTopics.Any(y => y.UserId == user.Id));
            
            topics = ApplySearch(topics, searchString);

            topics = topics.Where(x => showHidden ? x.Visible || !x.Visible : x.Visible || x.Proposed);
            if(showProposed == false) topics = topics.Where(x => !x.Proposed);

            //If search string is provided, select all topics from groups that match the search string
            if (searchString.Length > 2)
            {
                topics = topics.Concat(_context.Groups
                    .Where(x=> x.NameEng!.ToLower().Contains(searchString) || x.Name.ToLower().Contains(searchString))
                     .Select(y=>y.Topics).SelectMany(z=>z));
            }
 
            vm.Topics = await topics.ToListAsync();
            return View(vm);
        }
        
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

            var user = await GetUserOrNull();

            if (User.IsInRole("AnyTopic") || User.IsInRole("Topic") || topic.Visible || (user != null && (topic.SupervisorId == user.Id || topic.CreatorId == user.Id || topic.AssignedId == user.Id)))
                return View(topic);
            
            return Forbid();
        }

        [Authorize(Roles = "Topic,AnyTopic")]
        public async Task<IActionResult> Create(string groupName = "Unassigned", TopicType type = TopicType.Thesis)
        {
            var groupProvided = await GetGroup(groupName);
            
            //For custom groups, allow only the group provided
            if (!groupProvided.Selectable && groupName != "Unassigned")
                ViewData["Groups"] = new List<Group> {groupProvided};
            else //Else allow any selectable group
            {
                var groups = await _context.Groups.Where(x => x.Selectable).ToListAsync();
                foreach (var g in groups.Where(x => x.GroupId.Equals(groupProvided.GroupId)))
                    g.Highlight = true;

                ViewData["Groups"] = groups;
            }

            //Used in storno button
            ViewData["ReturnGroup"] = _context.Groups.FirstAsync(x => x.NameEng == groupName).Result.GroupId;
            
            ViewData["TopicType"] = type;

            ViewData["Programmes"] = 
                await _context.Programmes.Where(x => x.Active).ToListAsync();
            ViewData["UsersToAssign"] = 
                await _context.Users.Where(x => x.UserRoles.Any(y => y.Role.Name == "AssignedTopic")).ToListAsync();
            ViewData["UsersToSupervise"] = 
                await _context.Users.Where(x => x.UserRoles.Any(y => y.Role.Name == "SuperviseTopic")).ToListAsync();
                
            return View();
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Topic,AnyTopic")]
        public async Task<IActionResult> Create([Bind("TopicId,Name,NameEng,DescriptionShort,DescriptionShortEng,DescriptionLong,DescriptionLongEng,Visible,CreatorId,SupervisorId,AssignedId,GroupId,Type")] Topic topic, int[] programmes, List<IFormFile> files)
        {
            await TopicChange(topic, programmes, files, true);
            return RedirectToAction(nameof(Details), new {id = topic.TopicId});
        }

        [Authorize(Roles = "ProposeTopic")]
        public async Task<IActionResult> Propose(string groupName = "Unassigned", TopicType type = TopicType.Thesis)
        {
            var group = await GetGroup(groupName);
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
            
            //Used in storno button
            ViewData["ReturnGroup"] = _context.Groups.FirstAsync(x => x.NameEng == groupName).Result.GroupId;
            
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
                topic.Group = await _context.Groups.FirstAsync(x => x.NameEng!.Equals("Unassigned"));
                topic.GroupId = topic.Group.GroupId;
            }

            var user = await GetUser();
            
            topic.Creator = user;
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
            var topic = await _context.Topics.FindAsync(id);
            if (topic == null)
            {
                return NotFound();
            }

            if (topic.Group.NameEng!.Equals("Unassigned"))
            {
                ViewData["Groups"] = await _context.Groups.Where(x => x.Selectable).OrderBy(x=>x.Name).ToListAsync();
            }
            else if (topic.Group.Selectable)
            {
                var groups = await _context.Groups.Where(x => x.Selectable).ToListAsync();
                groups.First(x=>x.GroupId.Equals(topic.GroupId)).Highlight = true;
                ViewData["Groups"] = groups;
            }
            else
            {
                ViewData["Groups"] = new List<Group> {topic.Group};
            }

            ViewData["UsersToAssign"] = 
                await _context.Users.Where(x => x.UserRoles.Any(y => y.Role.Name == "AssignedTopic")).ToListAsync();
            ViewData["UsersToSupervise"] = 
                await _context.Users.Where(x => x.UserRoles.Any(y => y.Role.Name == "SuperviseTopic")).ToListAsync();
            
            ViewData["TopicType"] = topic.Type;

            var programmes = new HashSet<Programme>();
            foreach (var programme in await _context.Programmes.Where(x => x.Active).ToListAsync())
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
            var user = await GetUser();
            
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
            var user = await GetUser();

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
                var proposed = topic.Proposed;
                if (topic.Proposed && (topic.SupervisorId > 0 || topic.Visible))
                {
                    topic.Proposed = false;
                }
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
                if (proposed && topic.SupervisorId > 0)
                {
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
            //TODO delete files
            var topic = await _context.Topics.FirstAsync(x => x.TopicId.Equals(id));
            var group = topic.Group;

            var user = await GetUser();

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

            return await Topics(group.GroupId);
        }

        [Authorize(Roles = "InterestTopic")]
        public async Task<JsonResult> Interest(int topicId)
        {
            //Current user
            var user = await GetUser();

            //Topic
            var topic = await _context.Topics
                .Include(x=>x.Supervisor)
                .Include(x=>x.Creator)
                .Include(x=>x.UserInterestedTopics)
                .FirstAsync(x => x.TopicId.Equals(topicId));

            if (await _context.UserInterestedTopics.AnyAsync(x => x.UserId.Equals(user.Id) && x.TopicId.Equals(topic.TopicId)))
            {
                _context.UserInterestedTopics
                    .Remove(await _context.UserInterestedTopics
                        .FirstAsync(x => x.UserId.Equals(user.Id) && x.TopicId.Equals(topic.TopicId)));
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

            //TODO remove await?
            await _notificationManager.NewInterest(topic, user, CallbackDetailsUrl(topic.TopicId));
            
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
                    if (await _context.Attachments.AnyAsync(x => x.TopicId.Equals(topic.TopicId) && x.Name.Equals(file.FileName)))
                    {
                        //If file with same name already exists -> ignore and skip new file
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
        public async Task<JsonResult> DeleteAttachment(int id)
        {
            var attachment = await _context.Attachments.FirstAsync(x => x.AttachmentId.Equals(id));
            var topicId = attachment.TopicId;

            //Delete the file from server
            var a = await _context.Attachments.FirstAsync(x => x.AttachmentId.Equals(id));
            var file = new FileInfo(Path.Combine(_env.WebRootPath, "files", topicId.ToString(), a.Name));
            if (file.Exists)
            {
                file.Delete();
            }

            //Update database
            _context.Attachments.Remove(_context.Attachments.First(x => x.AttachmentId.Equals(id)));
            await _context.SaveChangesAsync();

            return Json(true);
        }
        
        [Authorize(Roles = "Comment,AnyComment")]
        public async Task<IActionResult> AddComment([Bind("CommentId,Text,Anonymous,ParentCommentId,TopicId")] Comment comment)
        {
            //get current user
            var user = await GetUser();
            comment.AuthorId = user.Id;
            comment.Author = user;
            comment.CreatedAt = DateTime.Now;
            
            _context.Comments.Add(comment);
            
            await _context.SaveChangesAsync();

            comment = await _context.Comments.Include("Topic").FirstAsync(x => x.CommentId == comment.CommentId);

            await _notificationManager.NewComment(comment, CallbackDetailsUrl(comment.TopicId));

            return RedirectToAction("Details", new { id = comment.TopicId });
        }
        
        [Authorize(Roles = "Comment,AnyComment")]
        public async Task<IActionResult> DeleteComment(int commentId)
        {
            var comment = await _context.Comments.FirstOrDefaultAsync(x => x.CommentId.Equals(commentId));
            if (comment is null) return RedirectToAction(nameof(Index));
            var topicId = comment.TopicId;
            
            if (User.IsInRole("DeleteComment") && !User.IsInRole("DeleteAnyComment"))
            {
                var user = await GetUser();
                if (comment.AuthorId != user.Id)
                    return Forbid();
            }
            
            var parent = comment.ParentComment;
            
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
            
            if (parent != null && parent.Text == "Deleted comment")
            {
                return await DeleteComment(parent.CommentId);
            }
            
            return RedirectToAction("Details", new {id = topicId});
        }

        private string CallbackDetailsUrl(int id)
        {
            return "https://" + HttpContext.Request.Host + $"/Topic/Details/{id}";
        }
        
        private async Task<Group> GetGroup(string groupName)
        {
            var group = await _context.Groups.FirstOrDefaultAsync(x => x.NameEng!.Equals(groupName));
            
            return group ?? throw new Exception("Group not found");
        }

        private async Task<Group> GetGroup(int groupId)
        {
            var group = await _context.Groups.FirstOrDefaultAsync(x => x.GroupId.Equals(groupId));
            
            return group ?? throw new Exception("Group not found");
        }

        private async Task<ApplicationUser?> GetUserOrNull()
        {
            var user = await _context.Users.FirstOrDefaultAsync(x => x.UserName!.Equals(User.Identity!.Name));
            return user ?? null;
        }

        private async Task<ApplicationUser> GetUser()
        {
            var user = await _context.Users.FirstOrDefaultAsync(x => x.UserName!.Equals(User.Identity!.Name));
            return user ?? throw new UnauthorizedAccessException("User is not signed in");
        }

        private IQueryable<Topic> ApplyShowHidden(IQueryable<Topic> topics, bool showHidden, bool showOnlyProposed)
        {
            //Shows only topics with visible = true, based on parameter
            if (!showHidden && !showOnlyProposed)
            {
                topics = topics.Where(x => x.Visible);
            }

            return topics;
        }
        
        private IQueryable<Topic> ApplySearch(IQueryable<Topic> topics, string searchString)
        {
            searchString = searchString.Trim();
            if (searchString.Length > 2)
            {
                searchString = searchString.ToLower();
                topics = topics.Where(x =>
                    x.Name.ToLower().Contains(searchString) || 
                    x.NameEng!.ToLower().Contains(searchString) ||
                    (x.Supervisor != null && (x.Supervisor.FirstName!.ToLower().Contains(searchString) ||
                                              x.Supervisor.LastName!.ToLower().Contains(searchString))));
            }
            return topics;
        }

        private IQueryable<Topic> ApplyShowTaken(IQueryable<Topic> topics, bool showTaken)
        {
            if (!showTaken)
            {
                topics = topics.Where(x => x.AssignedStudent == null);
            }

            return topics;
        }

        private IQueryable<Topic> ApplyShowOnlyProposed(IQueryable<Topic> topics, bool showProposed)
        {
            if (showProposed && (User.IsInRole("Topic") || User.IsInRole("AnyTopic")))
            {
                topics = topics.Where(x => x.Proposed);
            }

            return topics;
        }
        
        private IQueryable<Topic> ApplyProgramme(IQueryable<Topic> topics, string programme)
        {
            if (programme.Length > 0)
            {
                topics = topics.Where(x => x.TopicRecommendedPrograms
                    .Any(y => y.Programme.NameEng!.Equals(programme)));
            }

            return topics;
        }

        private IQueryable<Topic> ApplySort(IQueryable<Topic> topics, string orderBy)
        {
            switch (orderBy)
            {
                case "Name":
                    topics = CultureInfo.CurrentCulture.Name.Contains("cz") ? topics.OrderBy(x => x.Name.ToLower()) : topics.OrderBy(x => x.NameEng!.ToLower());
                    break;
                case "Interest":
                    topics = topics.OrderByDescending(x => x.UserInterestedTopics.Count);
                    break;
            }

            return topics;
        }
    }
}