// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System.ComponentModel.DataAnnotations;
using System.DirectoryServices.Protocols;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Xml.Linq;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TOS.Data;
using TOS.Models;

namespace TOS.Areas.Identity.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<LoginModel> _logger;
        private readonly ApplicationDbContext _context;
        private readonly IEmailSender _emailSender;
        
        public LoginModel(SignInManager<ApplicationUser> signInManager, ILogger<LoginModel> logger, ApplicationDbContext context, IEmailSender emailSender, UserManager<ApplicationUser> userManager)
        {
            _signInManager = signInManager;
            _logger = logger;
            _context = context;
            _emailSender = emailSender;
            _userManager = userManager;
        }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [BindProperty]
        public InputModel Input { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public string ReturnUrl { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [TempData]
        public string ErrorMessage { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public class InputModel
        {
            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Required]
            public string Username { get; set; }

            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Required]
            [DataType(DataType.Password)]
            public string Password { get; set; }

            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Display(Name = "Remember me?")]
            public bool RememberMe { get; set; }
        }

        public async Task OnGetAsync(string returnUrl = null)
        {
            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                ModelState.AddModelError(string.Empty, ErrorMessage);
            }

            returnUrl ??= Url.Content("~/");

            // Clear the existing external cookie to ensure a clean login process
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            ReturnUrl = returnUrl;
        }

        private void PrepareInputUsername()
        {
            if (Input.Username.EndsWith("@upol.cz"))
            {
                //User used his email
                var userFromEmailInput = _context.Users.FirstOrDefault(x => x.Email.Equals(Input.Username));
                if (userFromEmailInput != null)
                {
                    if(userFromEmailInput.UserName is null) throw new Exception("User has no username");
                    Input.Username = userFromEmailInput.UserName;
                }
                else
                {
                    //User used portalID + @upol.cz
                    Input.Username = Input.Username.Replace("@upol.cz", "");
                }
            }

            Input.Username = Input.Username.ToLower();
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            
            //Convert input username to lowercase
            Input.Username = Input.Username.ToLower();

            //Set Input.Username to correct format
            PrepareInputUsername();

            var successfulSignIn = false; //result shows if user was authenticated
            var user = _context.Users.FirstOrDefault(x => x.UserName.Equals(Input.Username));

            if (user != null)
            {
                //User is in database ------
                
                //Authenticate user with server database
                if (user.PasswordHash != null)
                {
                    successfulSignIn = await DatabaseSignIn(user, false);
                }
                
                //Authenticate user with LDAP UPOL domain
                if (!successfulSignIn)
                {
                    successfulSignIn = await LdapSignIn(user);
                }
            }
            else
            {
                //User not found in database -> register -> sign in 
                ApplicationUser createdUser;
                if ((createdUser = CreateLdapUser()) != null)
                {
                    successfulSignIn = await LdapSignIn(createdUser);
                }
            }

            //Successful login
            return successfulSignIn ? LocalRedirect(returnUrl) : InvalidLoginAttempt();
        }

        private bool LdapAuthenticate(string username, string password)
        {
            try
            {
                var authType = AuthType.Negotiate;

                if (!OperatingSystem.IsWindows())
                {
                    authType = AuthType.Basic;
                }

                //TODO extract to conf file
                var connection = new LdapConnection(new LdapDirectoryIdentifier("158.194.64.3", 389))
                {
                    AuthType = authType,
                    Credential = new NetworkCredential(username, password)
                };

                connection.SessionOptions.ProtocolVersion = 3;
                //Success
                connection.Bind();
            }
            catch
            {
                return false;
            }
            
            return true;
        }

        private ApplicationUser CreateLdapUser()
        {
            try
            {
                if (!LdapAuthenticate(Input.Username, Input.Password)) return null;

                //Decide if user is a teacher or student and get their userStagId
                string userStagId;
                const string stagServicesUrl = "https://stagservices.upol.cz/ws/services";
                
                var studentReq = stagServicesUrl + "/rest2/users/getOsobniCislaByExternalLogin?login=" + Input.Username;
                if ((userStagId = GetUserStagId(studentReq)).Length >= 1) return CreateUser(userStagId, true);
                
                //Student ID not found -> try teacher
                var teacherReq = stagServicesUrl + "/rest2/users/getUcitIdnoByExternalLogin?login=" + Input.Username;
                if ((userStagId = GetUserStagId(teacherReq)).Length >= 1)  return CreateUser(userStagId, false);
                
                //Couldn't find any userStagId
                return null;
            }
            catch
            {
                return null;
            }
        }

        private ApplicationUser CreateUser(string osCislo, bool student)
        {
            var roleString = student ? "Student" : "Teacher";
            var req = student
                ? "https://stagservices.upol.cz/ws/services/rest2/student/getStudentInfo?osCislo=" + osCislo
                : "https://stagservices.upol.cz/ws/services/rest2/ucitel/getUcitelInfo?ucitIdno=" + osCislo;

            var info = GetUserStagInfo(req);

            var lastnameLowered = info.Item2[..1] + info.Item2[1..].ToLower();
            var user = Seed.CreateUser(info.Item1, lastnameLowered, info.Item3, Input.Username,true, null, _context);
            
            var roleToInsert = _context.Roles.FirstOrDefault(x => x.Name.Equals(roleString));
            if (roleToInsert is null) throw new Exception("Role that should exist does not exist: " + roleString);
            
            Seed.CreateUserRole(user, roleToInsert, _context);

            return user;
        }

        private Tuple<string, string, string> GetUserStagInfo(string reqString)
        {
            try
            {
                var client = new HttpClient();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
                    Convert.ToBase64String(Encoding.GetEncoding("ISO-8859-1")
                        .GetBytes(Input.Username + ":" + Input.Password)));

                client.BaseAddress = new Uri(reqString);

                var response = client.GetAsync(reqString).Result;
                var responseString = response.Content.ReadAsStringAsync().Result;

                var xml = XDocument.Parse(responseString);

                var firstname = xml.Descendants("jmeno").First().Value;
                var lastname = xml.Descendants("prijmeni").First().Value;
                var email = xml.Descendants("email").First().Value;

                return new Tuple<string, string, string>(firstname, lastname, email);
            }
            catch
            {
                throw new Exception("STAGAPI connection failed");
            }
        }

        private string GetUserStagId(string reqString)
        {
            try
            {
                var client = new HttpClient();
                client.BaseAddress = new Uri(reqString);

                var response = client.GetAsync(reqString).Result;
                var responseString = response.Content.ReadAsStringAsync().Result;

                var xml = XDocument.Parse(responseString);

                return xml.Descendants("osCislo").First().Value;
            }
            catch
            {
                throw new Exception("STAGAPI connection failed");
            }
        }
        
        private async Task<bool> DatabaseSignIn(ApplicationUser user, bool lockoutOnFailure)
        {
            if (user.UserName is null)
                throw new Exception("User does not have a username");

            var result =
                await _signInManager.PasswordSignInAsync(user.UserName, Input.Password, Input.RememberMe,
                    lockoutOnFailure);
            
            return result.Succeeded;
        }

        private async Task<bool> LdapSignIn(ApplicationUser user)
        {
            if (!LdapAuthenticate(user.UserName, Input.Password)) return false;
            
            await _signInManager.SignInAsync(user, Input.RememberMe);
            return true;
        }

        private IActionResult InvalidLoginAttempt()
        {
            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            return Page();
        }
    }
}
