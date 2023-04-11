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

        public async Task<IActionResult> Topics(int? groupId)
        {
            var group = await _context.Groups.FindAsync(groupId);
            if (groupId is null || group is null) return RedirectToAction("Index", "Home");
            
            if(group.NameEng.Equals("Bachelor") || group.NameEng.Equals("Master"))
                return RedirectToAction("Index", "Topic", new{ groupName = group.NameEng});
            
            if(group.NameEng.Equals("Unassigned") && (User.IsInRole("Topic") || User.IsInRole("AnyTopic")))
                return RedirectToAction("Unassigned", "Topic");
            
            return RedirectToAction("Group", "Topic", new {groupId = group.GroupId});
        }
        
        public async Task<IActionResult> Index(string groupName = "Bachelor", int programmeName = -1,
            string searchString = "", bool showTakenTopics = false, string orderBy = "Supervisor",
            bool showHidden = false, bool showOnlyProposed = false)
        {
            if (groupName != "Bachelor" && groupName != "Master")
                return Forbid();

            var group = await GetGroup(groupName);
            if (group is null) return NotFound();
            
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
                Programmes = await _context.Programmes.Where(x => x.Active && x.Type == (ProgramType) Enum.Parse(typeof(ProgramType), groupName)).ToListAsync()
            };

            var topicsToShow = _context.Topics.Where(x => x.Group.Equals(group) && x.Type == TopicType.Thesis);

            topicsToShow = ApplyShowHidden(topicsToShow, showHidden, showOnlyProposed);
            topicsToShow = ApplySearch(topicsToShow, searchString);
            topicsToShow = ApplyShowTaken(topicsToShow, showTakenTopics || showOnlyProposed);
            topicsToShow = ApplyProgramme(topicsToShow, programmeName);
            topicsToShow = ApplyShowOnlyProposed(topicsToShow, showOnlyProposed);
            topicsToShow = ApplySort(topicsToShow, orderBy);

            vm.Topics = await topicsToShow.ToListAsync();
            return View(vm);
        }
        
        public async Task<IActionResult> Group(int? groupId, string searchString = "", bool showTakenTopics = false, bool showHidden = false, bool showProposed = false)
        {
            if (groupId is null) return NotFound();

            var group = await GetGroup(groupId);
            if (group is null) return NotFound();
            
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
            topicsToShow = ApplyShowTaken(topicsToShow, showProposed || showTakenTopics);
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
                    .Where(x=> x.NameEng.ToLower().Contains(searchString) || x.Name.ToLower().Contains(searchString))
                     .Select(y=>y.Topics).SelectMany(z=>z));
            }
 
            vm.Topics = await topics.ToListAsync();
            return View(vm);
        }
        
        public async Task<IActionResult> Details(int? id)
        {
            if (id is null) return NotFound();
            
            var topic = await _context.Topics
                .Include(t => t.AssignedStudent)
                .Include(t => t.Creator)
                .Include(t => t.Group)
                .Include(t => t.Supervisor)
                .FirstOrDefaultAsync(m => m.TopicId == id);

            if (topic is null) return NotFound();
            
            var user = await GetUserOrNull();

            if (User.IsInRole("AnyTopic") || User.IsInRole("Topic") || topic.Visible || (user != null && (topic.SupervisorId == user.Id || topic.CreatorId == user.Id || topic.AssignedId == user.Id)))
                return View(topic);
            
            return Forbid();
        }

        [Authorize(Roles = "Topic,AnyTopic,Group,AnyGroup")]
        public async Task<IActionResult> Create(string groupName = "Unassigned", TopicType type = TopicType.Thesis)
        {
            var groupProvided = await GetGroup(groupName);
            if (groupProvided is null) return NotFound();
            
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
        [Authorize(Roles = "Topic,AnyTopic,Group,AnyGroup")]
        public async Task<IActionResult> Create([Bind("TopicId,Name,NameEng,DescriptionShort,DescriptionShortEng,DescriptionLong,DescriptionLongEng,Visible,CreatorId,SupervisorId,AssignedId,GroupId,Type")] Topic topic, int[] programmes, List<IFormFile> files)
        {
            if (User.IsInRole("Group") && (!User.IsInRole("Topic") || !User.IsInRole("AnyTopic") || !User.IsInRole("AnyGroup")))
            {
                var user = await GetUser();
                if (!user.CreatedGroups.Any(x => x.GroupId == topic.GroupId))
                {
                    return Forbid();
                }
            }
            
            if (await TopicChange(topic, programmes, files, true))
            {
                return RedirectToAction(nameof(Details), new {id = topic.TopicId});
            }
            return View(topic);
        }

        [Authorize(Roles = "ProposeTopic")]
        public async Task<IActionResult> Propose(string groupName = "Unassigned", TopicType type = TopicType.Thesis)
        {
            var group = await GetGroup(groupName);
            if (group is null) return NotFound();
            
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
            if (topic.GroupId == -1)
            {
                topic.Group = await _context.Groups.FirstAsync(x => x.NameEng.Equals("Unassigned"));
                topic.GroupId = topic.Group.GroupId;
            }

            var user = await GetUser();
            
            topic.Creator = user;
            if (User.IsInRole("Student"))
            {
                topic.AssignedId = user.Id;
                topic.AssignedStudent = user;
            }

            topic.Visible = false;
            topic.Proposed = true;

            if(!IsValid()) return View(topic);
                
            _context.Add(topic);
            await _context.SaveChangesAsync();

            //Save files
            await CreateFiles(topic, topic.Creator, files);

            return RedirectToAction(nameof(Details), new {id = topic.TopicId});
        }
        
        [Authorize(Roles = "Topic,AnyTopic,ProposeTopic,Group,AnyGroup")]
        public async Task<IActionResult> Edit(int? id)
        {
            var topic = await _context.Topics.FindAsync(id);
            if (topic == null)
            {
                return NotFound();
            }

            if (topic.Group.NameEng.Equals("Unassigned"))
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
                if (topic.TopicRecommendedPrograms.Any(x => x.ProgrammeId.Equals(programme.ProgrammeId)))
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
        [Authorize(Roles = "Topic,AnyTopic,ProposeTopic,Group,AnyGroup")]
        public async Task<IActionResult> Edit([Bind("TopicId,Name,NameEng,DescriptionShort,DescriptionShortEng,DescriptionLong,DescriptionLongEng,Visible,CreatorId,SupervisorId,AssignedId,GroupId,Type,Proposed")] Topic topic, int[] programmes, List<IFormFile> files, int oldAssigned)
        {
            var user = await GetUser();

            var canEdit = User.IsInRole("AnyTopic") ||
                          await _context.Groups.AnyAsync(x => x.GroupId == topic.GroupId && x.CreatorId == user.Id);
            //User can edit topics he created or supervise
            if (User.IsInRole("Topic") && (topic.CreatorId == user.Id || topic.SupervisorId == user.Id || topic.Proposed || topic.SupervisorId == null))
                canEdit = true;
            else if(User.IsInRole("ProposeTopic") && topic.Proposed && topic.CreatorId == user.Id)
                canEdit = true;
            else if (User.IsInRole("Group") && (!User.IsInRole("Topic") || !User.IsInRole("AnyTopic") || !User.IsInRole("AnyGroup")))
                if (user.CreatedGroups.Any(x => x.GroupId == topic.GroupId))
                    canEdit = true;
            
            if (!canEdit)
                return Forbid();

            if (await TopicChange(topic, programmes, files, false, oldAssigned))
            {
                return RedirectToAction(nameof(Details), new {id = topic.TopicId});
            }

            return View(topic);
        }

        private async Task<bool> TopicChange(Topic topic, IEnumerable<int> programmesId, List<IFormFile> files, bool isNew = false, int oldAssigned = -1)
        {
            var user = await GetUser();

            if (isNew)
            {
                topic.CreatorId = user.Id;
                topic.Creator = user;
                //Create topic
                if (!IsValid()) return false;
                _context.Add(topic);
                await _context.SaveChangesAsync();
                
                if(topic.AssignedId != null)
                    await _notificationManager.TopicAssigned(topic, user, CallbackDetailsUrl(topic.TopicId));
            }
            else
            {
                var proposed = topic.Proposed;
                if (topic.Proposed && (topic.SupervisorId > 0 || topic.Visible))
                {
                    topic.Proposed = false;
                }
                if (!IsValid()) return false;
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
                if (proposed && (topic.SupervisorId > 0 || topic.Visible))
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
                    ProgrammeId = programme
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
            var topic = await _context.Topics.Include(x=>x.Attachments)
                .Include(x=>x.Comments)
                .Include(x=>x.UserInterestedTopics).Include(x=>x.TopicRecommendedPrograms)
                .FirstOrDefaultAsync(x=>x.TopicId == id);
            
            if (id is null || topic is null) return NotFound();
            
            var group = topic.Group;
            var user = await GetUser();

            var canEdit = User.IsInRole("AnyTopic") ||
                          await _context.Groups.AnyAsync(x => x.GroupId == topic.GroupId && x.CreatorId == user.Id);
            //User can edit topics he created or supervise
            if (User.IsInRole("Topic") && topic.CreatorId == user.Id || topic.SupervisorId == user.Id || topic.Proposed)
                canEdit = true;
            if(User.IsInRole("ProposeTopic") && topic.Proposed && topic.CreatorId == user.Id)
                canEdit = true;
            if (!canEdit)
                return Forbid();
            
           
            foreach (var at in topic.Attachments)
            {
                await DeleteAttachment(at);
            }
            _context.Comments.RemoveRange(topic.Comments);
            _context.UserInterestedTopics.RemoveRange(topic.UserInterestedTopics);
            _context.TopicRecommendedProgrammes.RemoveRange(topic.TopicRecommendedPrograms);
            _context.Topics.Remove(topic);
            await _context.SaveChangesAsync();

            return await Topics(group.GroupId);
        }

        [Authorize(Roles = "InterestTopic")]
        public async Task<JsonResult> Interest(int? topicId)
        {
            var user = await GetUser();
            var topic = await _context.Topics
                .Include(x=>x.Supervisor)
                .Include(x=>x.Creator)
                .Include(x=>x.UserInterestedTopics)
                .FirstOrDefaultAsync(x => x.TopicId.Equals(topicId));

            if (topicId is null || topic is null) return Json(false);
            
            var interested = topic.UserInterestedTopics.FirstOrDefault(x=>x.UserId.Equals(user.Id));
            if (interested != null)
            {
                _context.UserInterestedTopics.Remove(interested);
                await _context.SaveChangesAsync();
            }
            else
            {
                _context.UserInterestedTopics.Add(new UserInterestedTopic()
                {
                    User = user,
                    Topic = topic,
                    DateTime = DateTime.Now
                });
                await _context.SaveChangesAsync();
                await _notificationManager.NewInterest(topic, user, CallbackDetailsUrl(topic.TopicId));
            }

            return Json(true);
        }

        [Authorize(Roles = "Attachment")]
        private async Task<JsonResult> DeleteAttachment(Attachment? attachment)
        {
            if(attachment is null) return Json(false);
            
            var topicId = attachment.TopicId;
            
            var file = new FileInfo(Path.Combine(_env.WebRootPath, "files", topicId.ToString(), attachment.Name));
            if (file.Exists)
            {
                file.Delete();
            }

            //Update database
            _context.Attachments.Remove(attachment);
            await _context.SaveChangesAsync();

            return Json(true);
        }
        
        [Authorize(Roles = "Attachment")]
        public async Task<JsonResult> DeleteAttachment(int? id)
        {
            var attachment = await _context.Attachments.FindAsync(id);
            if (id is null || attachment is null) return Json(false);
            
            return await DeleteAttachment(attachment);
        }
        
        [Authorize(Roles = "Comment,AnyComment")]
        public async Task<IActionResult> AddComment([Bind("CommentId,Text,Anonymous,ParentCommentId,TopicId")] Comment comment)
        {
            var user = await GetUserOrNull();
            if (user is null) return Forbid();
            
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
        public async Task<IActionResult> DeleteComment(int? commentId)
        {
            var comment = await _context.Comments.FirstOrDefaultAsync(x => x.CommentId.Equals(commentId));
            if (commentId is null || comment is null) return NotFound();
            var topicId = comment.TopicId;
            
            if (User.IsInRole("DeleteComment") && !User.IsInRole("DeleteAnyComment"))
            {
                var user = await GetUser();
                if (comment.AuthorId != user.Id)
                    return Forbid();
            }
            
            await CommentHelper.DeleteComment(comment, comment.Author, _context);

            return RedirectToAction("Details", new {id = topicId});
        }
        
        [Authorize(Roles = "Attachment")]
        private async Task CreateFiles(Topic topic, ApplicationUser user, List<IFormFile> files)
        {
            foreach (var file in files)
            {
                var filename = file.FileName;
                if (filename.Length > 64)
                {
                    filename = filename.Substring(filename.Length - 64);
                }
                
                var filePath = Path.Combine(_env.WebRootPath, "files", topic.TopicId.ToString(), filename);
                var fileInfo = new FileInfo(filePath);

                //If topic has attachment with same file name -> skip current file
                if (fileInfo.Exists)
                {
                    if (await _context.Attachments.AnyAsync(x => x.TopicId.Equals(topic.TopicId) && x.Name.Equals(filename)))
                    {
                        //If file with same name already exists -> ignore and skip new file
                        continue;
                    }

                    fileInfo.Delete();
                }

                try
                {
                    //Create missing directories
                    Directory.CreateDirectory(Path.GetDirectoryName(filePath) ?? string.Empty);

                    //Create file
                    await using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }
                }
                catch(Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }

                //Add record to database
                _context.Attachments.Add(new Attachment()
                {
                    CreatorId = user.Id,
                    TopicId = topic.TopicId,
                    Name = filename
                });
            }

            await _context.SaveChangesAsync();
        }

        private string CallbackDetailsUrl(int id)
        {
            return "https://" + HttpContext.Request.Host + $"/Topic/Details/{id}";
        }
        
        private async Task<Group?> GetGroup(string groupName)
        {
            return await _context.Groups.FirstOrDefaultAsync(x => x.NameEng.Equals(groupName));
        }

        private async Task<Group?> GetGroup(int? groupId)
        {
            return await _context.Groups.FindAsync(groupId);
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
            if (!showHidden && !showOnlyProposed || (!User.IsInRole("Topic") && !User.IsInRole("AnyTopic")))
            {
                topics = topics.Where(x => x.Visible);
            }

            return topics;
        }
        
        private IQueryable<Topic> ApplySearch(IQueryable<Topic> topics, string searchString)
        {
            searchString = searchString.Trim();
            if (searchString.Length > 1)
            {
                searchString = searchString.ToLower();
                topics = topics.Where(x =>
                    x.Name.ToLower().Contains(searchString) || 
                    x.NameEng!.ToLower().Contains(searchString) ||
                    (x.Supervisor != null && (x.Supervisor.FirstName.ToLower().Contains(searchString) ||
                                              x.Supervisor.LastName.ToLower().Contains(searchString))));
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
        
        private IQueryable<Topic> ApplyProgramme(IQueryable<Topic> topics, int programme)
        {
            if (programme > 0)
            {
                topics = topics.Where(x => x.TopicRecommendedPrograms
                    .Any(y => y.Programme.ProgrammeId.Equals(programme)));
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

        private bool IsValid()
        {
            ModelState.Remove("Creator");
            ModelState.Remove("Assigned");
            ModelState.Remove("AssignedId");
            ModelState.Remove("Supervisor");
            ModelState.Remove("SupervisorId");
            ModelState.Remove("Group");
            ModelState.Remove("oldAssigned");
            

            return TryValidateModel("Topic");
        }
        
    }
}