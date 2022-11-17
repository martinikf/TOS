// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.DirectoryServices.Protocols;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using NuGet.Common;
using TOS.Data;
using TOS.Models;

namespace TOS.Areas.Identity.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger<LoginModel> _logger;
        private readonly ApplicationDbContext _context;

        public LoginModel(SignInManager<ApplicationUser> signInManager, ILogger<LoginModel> logger, ApplicationDbContext context)
        {
            _signInManager = signInManager;
            _logger = logger;
            _context = context;
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
                    _logger.LogInformation("User logged in.");
                    return LocalRedirect(returnUrl);
                }
                if (result.RequiresTwoFactor)
                {
                    return RedirectToPage("./LoginWith2fa", new { ReturnUrl = returnUrl, RememberMe = Input.RememberMe });
                }
                if (result.IsLockedOut)
                {
                    _logger.LogWarning("User account locked out.");
                    return RedirectToPage("./Lockout");
                }
                else
                {
                    //Check if username exists in database
                    var user = _context.Users.FirstOrDefault(u => u.UserName == Input.Username);
                    if (user != null)
                    {
                        if (LDAPAuthenticate(Input.Username, Input.Password))
                        {
                            //AD credentials are valid, update password
                            //User changed his AD password
                            var passwordHasher = new PasswordHasher<ApplicationUser>();
                            user.PasswordHash = passwordHasher.HashPassword(user, Input.Password);
                            _context.Users.Update(user);
                            
                            //Sign in user
                            _signInManager.PasswordSignInAsync(Input.Username, Input.Password, Input.RememberMe,
                                lockoutOnFailure: false);
                            return LocalRedirect(returnUrl);
                        }
                    }
                    //If user is not in database, check if AD credentials are valid -> CreateUser
                    if (CreateADUser())
                    {
                        _logger.LogInformation("User logged in.");
                        _signInManager.PasswordSignInAsync(Input.Username, Input.Password, Input.RememberMe,
                            lockoutOnFailure: false);
                        return LocalRedirect(returnUrl);
                    }
                    
                    ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                    return Page();
                }
            }

            // If we got this far, something failed, redisplay form
            return Page();
        }


        private bool LDAPAuthenticate(string username, string password)
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
                    Credential = new NetworkCredential(Input.Username, Input.Password)
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

        private bool CreateADUser()
        {
            try
            {
                //If AD authentication fails -> return false
                if (!LDAPAuthenticate(Input.Username, Input.Password)) return false;

                var student = true;
                
                //Decide if user is a teacher or student and get OsCislo
                string osCislo = String.Empty;
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
                        //Teacher osCislo not found -> return false
                        return false;
                    }
                }

                //Create user to database
                CreateUser(osCislo, student);
            }
            catch
            {
                return false;
            }

            return true;
        }

        private void CreateUser(string osCislo, bool student)
        {
            var role = student ? "Student" : "Teacher";
            var req = student
                ? "https://stagservices.upol.cz/ws/services/rest2/student/getStudentInfo?osCislo=" + osCislo
                : "https://stagservices.upol.cz/ws/services/rest2/ucitel/getUcitelInfo?ucitIdno=" + osCislo;

            var info = GetInfo(req);
            var user = Seed.CreateUser(info.Item1, info.Item2, info.Item3, Input.Username,false, Input.Password, _context);
            Seed.CreateUserRole(user, _context.Roles.FirstOrDefault(x => x.Name.Equals(role)), _context);
        }

        private Tuple<string, string, string> GetInfo(string reqString)
        {
            var req = (HttpWebRequest) WebRequest.Create(reqString);
            req.Method = "GET";
            req.ContentType = "application/xml";
            req.Headers.Add("Authorization", "Basic " + Convert.ToBase64String(Encoding.GetEncoding("ISO-8859-1").GetBytes(Input.Username + ":" + Input.Password)));
            
            var response = req.GetResponse();

            //response to string
            var responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();
        
            //get jmeno, prijmeni, email from xml
            var xml = XDocument.Parse(responseString);
            
            var firstname = xml.Descendants("jmeno").First().Value;
            var lastname = xml.Descendants("prijmeni").First().Value;
            var email = xml.Descendants("email").First().Value;
            
            return new Tuple<string, string, string>(firstname, lastname, email);
        }

        private string GetOsCislo(string reqString)
        {
            var req = (HttpWebRequest) WebRequest.Create(reqString);
            req.Method = "GET";
            req.ContentType = "application/xml";
            var response = req.GetResponse();

            var responseString =
                new StreamReader(response.GetResponseStream() ?? throw new Exception("No response from STAGAPI"))
                    .ReadToEnd();
            var xml = XDocument.Parse(responseString);
            
            string osCislo = String.Empty;
            osCislo = xml.Descendants("osCislo").First().Value;
            return osCislo;
        }
    }
}
