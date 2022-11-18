// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System.ComponentModel.DataAnnotations;
using System.DirectoryServices.Protocols;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Encodings.Web;
using System.Xml.Linq;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
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

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");

            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            //Else external login
            if (ModelState.IsValid)
            {
                // This doesn't count login failures towards account lockout
                // To enable password failures to trigger account lockout, set lockoutOnFailure: true
                var result = await _signInManager.PasswordSignInAsync(Input.Username, Input.Password, Input.RememberMe, lockoutOnFailure: false);
                
                if (result.Succeeded)
                {
                    //Used also for AD users with local password
                    _logger.LogInformation("User logged in");
                    return LocalRedirect(returnUrl);
                }
                if (result.IsLockedOut)
                {
                    _logger.LogWarning("User account locked out");
                    return RedirectToPage("./Lockout");
                }
                else
                {
                    //For AD users

                    //Login for AD users with account in database
                    //Check if username exists in database
                    var user = _context.Users.FirstOrDefault(u => u.UserName == Input.Username);

                    if (user is null)
                    {
                        //AD user in database used an email instead of portal login username
                        //Happens if User is already in database and entered full email instead of username
                        var adUserByEmail =
                            _context.Users.FirstOrDefault(u => u.Email.ToLower().Equals(Input.Username.ToLower()));
                        if (adUserByEmail != null)
                        {
                            if (adUserByEmail.UserName == null) throw new Exception("User doesn't have username");
                            
                            //Local password check
                            result = await _signInManager.PasswordSignInAsync(adUserByEmail.UserName, Input.Password,
                                Input.RememberMe, lockoutOnFailure: false);
                            if (result.Succeeded)
                            {
                                return LocalRedirect(returnUrl);
                            }

                            //Check for AD auth
                            if (LdapAuthenticate(adUserByEmail.UserName, Input.Password))
                            {
                                //Sign in user
                                await _signInManager.SignInAsync(adUserByEmail, Input.RememberMe);
                                return LocalRedirect(returnUrl);
                            }
                            
                            //Failed login
                            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                            return Page();
                        }
                        
                        //Happens if user used username + @upol.cz
                        if ( Input.Username.EndsWith("@upol.cz"))
                        {
                            //Trim the @upol.cz part
                            Input.Username = Input.Username.Replace("@upol.cz", "");
                            
                            //set the user with trimmed username
                            user = _context.Users.FirstOrDefault(u => u.UserName.Equals(Input.Username));
                            
                            //Have to check again for local password login
                            //Check if user tried to logged in with local password
                            result = await _signInManager.PasswordSignInAsync(Input.Username, Input.Password,
                                Input.RememberMe, lockoutOnFailure: false);
                            if (result.Succeeded)
                            {
                                return LocalRedirect(returnUrl);
                            }
                            //Check for AD login later.

                        }
                    }

                    if (user != null)
                    {
                        if (LdapAuthenticate(Input.Username, Input.Password))
                        {
                            //Sign in user
                            await _signInManager.SignInAsync(user, Input.RememberMe);
                            return LocalRedirect(returnUrl);
                        }
                    }
                    
                    //If user is not in database, check if AD credentials are valid -> CreateUser
                    ApplicationUser createdUser;
                    if ((createdUser = CreateLdapUser()) != null)
                    {
                        //Sign in new user
                        await _signInManager.SignInAsync(createdUser, Input.RememberMe);

                        //Generate email confirmation
                        var code = await _userManager.GenerateEmailConfirmationTokenAsync(createdUser);
                        code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                        var callbackUrl = Url.Page(
                            "/Account/ConfirmEmail",
                            pageHandler: null,
                            values: new { area = "Identity", userId = createdUser.Id, code, returnUrl = "/"},
                            protocol: Request.Scheme);

                        if(callbackUrl is null) throw new Exception("CallbackUrl is null");

                        if (createdUser.Email is null) throw new Exception("Email is null, STAGAPI fetch failed");
                        
                        //Send email confirmation
                        await _emailSender.SendEmailAsync(createdUser.Email, "Confirm your email",
                            $"Please confirm your account by <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicking here</a>.");
                        
                        _logger.LogInformation("User logged in");
                        return LocalRedirect(returnUrl);
                    }
                    
                    ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                    return Page();
                }
            }

            // If we got this far, something failed, redisplay form
            return Page();
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
                //If AD authentication fails -> return false
                if (!LdapAuthenticate(Input.Username, Input.Password)) return null;

                var student = true;
                
                //Decide if user is a teacher or student and get OsCislo
                string osCislo;
                var studentReq =
                    "https://stagservices.upol.cz/ws/services/rest2/users/getOsobniCislaByExternalLogin?login=" + Input.Username;
                
                if ((osCislo = GetOsCislo(studentReq)).Length < 1)
                {
                    //Student osCislo not found -> try teacher
                    student = false;
                    string teacherReq =
                        "https://stagservices.upol.cz/ws/services/rest2/users/getUcitIdnoByExternalLogin?login=" +
                        Input.Username;

                    if ((osCislo = GetOsCislo(teacherReq)).Length < 1)
                    {
                        //Teacher osCislo not found -> return null
                        return null;
                    }
                }

                //Create user to database
                return CreateUser(osCislo, student);
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

            var info = GetInfo(req);
            
            //If info values are null, stagapi connection failed

            var lastnameLowered = info.Item2[..1] + info.Item2[1..].ToLower();
            var user = Seed.CreateUser(info.Item1, lastnameLowered, info.Item3, Input.Username,false, null, _context);
            
            var roleToInsert = _context.Roles.FirstOrDefault(x => x.Name.Equals(roleString));
            if (roleToInsert is null) throw new Exception("Role that should exist does not exist: " + roleString);
            
            Seed.CreateUserRole(user, roleToInsert, _context);

            return user;
        }

        private Tuple<string, string, string> GetInfo(string reqString)
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
                _logger.LogInformation("Stag api connection failed");
                ModelState.AddModelError(string.Empty, "Connection to STAGAPI failed.");
                //return to login page
                RedirectToAction("OnGetAsync");
                throw new Exception("STAGAPI connection failed");
            }
        }

        private string GetOsCislo(string reqString)
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
                //Stag api connection failed
                _logger.LogInformation("Stag api connection failed");
                ModelState.AddModelError(string.Empty, "Connection to STAGAPI failed.");
                //return to login page
                RedirectToAction("OnGetAsync");
                throw new Exception("STAGAPI connection failed");
            }
        }
    }
}
