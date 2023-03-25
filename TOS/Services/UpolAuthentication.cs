using System.Net.Http.Headers;
using System.Text;
using System.Xml.Linq;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.DirectoryServices.Protocols;
using System.Net;
using TOS.Data;
using TOS.Models;

namespace TOS.Services;

public class UpolAuthentication : IAuthentication
{
    private readonly ApplicationDbContext _context;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IConfiguration _configuration;

    private readonly string _stagServicesUrl;
    private readonly string _stagServicesStudentOsCisloByExternal;
    private readonly string _stagServicesTeacherIdnoByExternal;
    private readonly string _stagServicesStudentInfoByOsCislo;
    private readonly string _stagServicesTeacherInfoByIdno;
    
    public UpolAuthentication(ApplicationDbContext context, SignInManager<ApplicationUser> signInManager, IConfiguration configuration)
    {
        _context = context;
        _signInManager = signInManager;
        _configuration = configuration;
        
        _stagServicesUrl = _configuration["StagServices:URL"] ?? string.Empty;
        _stagServicesStudentOsCisloByExternal = _configuration["StagServices:StudentOsCisloByExternal"] ?? string.Empty;
        _stagServicesTeacherIdnoByExternal = _configuration["StagServices:TeacherIdnoByExternal"] ?? string.Empty;
        _stagServicesStudentInfoByOsCislo = _configuration["StagServices:StudentInfoByOsCislo"] ?? string.Empty;
        _stagServicesTeacherInfoByIdno = _configuration["StagServices:TeacherInfoByIdno"] ?? string.Empty;
    }

    public async Task<bool> Authenticate(string username, string password, bool rememberMe)
    {
        username = NormalizeUsername(username);

        var user = await _context.Users.FirstOrDefaultAsync(x => x.UserName!.ToLower().Equals(username));

        //User has signed in the past
        if (user is not null)
        {
            return await SignIn(username, password, rememberMe, user);
        }
        
        //User doesn't exists in local DB, check if login is in UPOL AD
        return await UnknownUser(username, password, rememberMe);
    }

    private string NormalizeUsername(string username)
    {
        username = username.Trim().ToLower();
        if (username.EndsWith("@upol.cz"))
        {
            //Check if user used his email long_email@upol.cz; This doesn't work for very first sign in
            var userFromEmailInput = _context.Users.FirstOrDefault(x => x.Email!.ToLower().Equals(username));
            if (userFromEmailInput != null)
            {
                username = userFromEmailInput.UserName!;
            }
            else
            {
                //User used portalID + @upol.cz
                username = username.Replace("@upol.cz", "");
            }
        }

        return username;
    }

    private async Task<bool> SignIn(string username, string password, bool rememberMe, ApplicationUser user)
    {
        //User uses UPOL AD
        if (user.PasswordHash == null)
        {
            return await UpolSignIn(username, password, rememberMe, user);
        }

        //User is local
        return await LocalSignIn(username, password, rememberMe);
    }

    private async Task<bool> LocalSignIn(string username, string password, bool rememberMe)
    {
        //Try tu sign in user with credentials in local DB and return true if login was successful
        return (await _signInManager.PasswordSignInAsync(username, password, rememberMe, false)).Succeeded;
    }

    private async Task<bool> UpolSignIn(string username, string password, bool rememberMe, ApplicationUser user)
    {
        //Try to authenticate the user against UPOL AD, if successful, sign in the user
        if (UpolActiveDirectoryAuthenticate(username, password))
        {
            await _signInManager.SignInAsync(user, rememberMe);
            return true;
        }
        return false;
    }

    private bool UpolActiveDirectoryAuthenticate(string username, string password)
    {
        try
        {
            //Works only under windows, ssl 
            var endpoint = new LdapDirectoryIdentifier(_configuration["UpolActiveDirectory:IP"] ?? string.Empty,  int.Parse(_configuration["UpolActiveDirectory:Port"] ?? string.Empty), true, false);
            using var ldap = new LdapConnection(endpoint, new NetworkCredential(_configuration["UpolActiveDirectory:Dn"] + "\\" + username, password))
            {
                AuthType = AuthType.Basic
            };
            ldap.SessionOptions.SecureSocketLayer = true;
            ldap.SessionOptions.ProtocolVersion = 3;
            //Allow self signed certificates
            ldap.SessionOptions.VerifyServerCertificate = (_, _) => true;
            ldap.Timeout = TimeSpan.FromMinutes(1);
            
            ldap.Bind();
            
            return true;
        }
        catch
        {
            return false;
        }
    }
    
    private async Task<bool> UnknownUser(string username, string password, bool rememberMe)
    {
        //Check credentials against UPOL AD
        if (UpolActiveDirectoryAuthenticate(username, password))
        {
            //User used UPOL AD credentials, create new user in local DB
            var user = await CreateUpolUser(username, password);
            return await UpolSignIn(username, password, rememberMe, user);
        }
        //User used wrong credentials or UPOL AD is down
        return false;
    }

    private async Task<ApplicationUser> CreateUpolUser(string username, string password)
    {
        string stagId;
        //Get user stag ID
        stagId = GetStudentStagId(username);
        if (stagId.Length > 0)
        {
            return await CreateStudent(username, password, stagId);
        }
        
        stagId = GetTeacherStagId(username);
        if (stagId.Length > 0)
        {
            return await CreateTeacher(username, password, stagId);
        }
        
        //User is not in STAG
        throw new Exception("User authenticated in UPOL AD, but is not in STAG. Possible cause: STAG API is down; user has different AD and STAG login");
    }
    
    private async Task<ApplicationUser> CreateStudent(string username, string password, string stagId)
    {
        var request = _stagServicesStudentInfoByOsCislo + stagId;
        var user = await CreateUser(request, username, password);
        await RoleHelper.AssignRoles(user, Role.Student, _context);
        return user;
    }
    
    private async Task<ApplicationUser> CreateTeacher(string username, string password, string stagId)
    {
        var request = _stagServicesTeacherInfoByIdno + stagId;
        var user = await CreateUser(request, username, password);
        await RoleHelper.AssignRoles(user, Role.Teacher, _context);
        return user;
    }

    private async Task<ApplicationUser> CreateUser(string request, string username, string password)
    {
        if (!GetStagInfo(request, username, password, out var firstname, out var lastname, out var email))
            throw new Exception("Stag API failed");
        
        //Lower case lastname except first letter
        lastname = lastname[..1] + lastname[1..].ToLower();

        var user = new ApplicationUser()
        {
            FirstName = firstname,
            LastName = lastname,
            Email = email,
            NormalizedEmail = email.ToUpper(),
            UserName = username,
            NormalizedUserName = username.ToUpper(),
            EmailConfirmed = true,
            PasswordHash = null
        };
        user.SecurityStamp = Guid.NewGuid().ToString("D");

        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        user = _context.Users.FirstOrDefault(x => x.UserName!.Equals(username));
        if (user is null) throw new Exception("User was not created");

        return user;
    }

    private bool GetStagInfo(string request, string username, string password, out string firstname, out string lastname, out string email)
    {
        try
        {
            var response = StagRequest(request, username, password);
            var xml = XDocument.Parse(response);

            firstname = xml.Descendants("jmeno").First().Value;
            lastname = xml.Descendants("prijmeni").First().Value;
            email = xml.Descendants("email").First().Value;
            return true;
        }
        catch
        {
            firstname = "";
            lastname = "";
            email = "";
            return false;
        }
    }

    private string GetStudentStagId(string username)
    {
        try
        {
            var request = _stagServicesStudentOsCisloByExternal + username;
            var response = StagRequest(request, null, null);
            var xml = XDocument.Parse(response);
       
            var result = xml.Descendants("osCislo").First().Value;
            return result;
        }
        catch
        {
            return string.Empty;
        }
}
    
    private string GetTeacherStagId(string username)
    {
        var request = _stagServicesTeacherIdnoByExternal + username;
        return StagRequest(request, null, null);
    }

    private string StagRequest(string request, string? username, string? password)
    {
        request = _stagServicesUrl + request;
        try
        {
            var client = new HttpClient();
            if (username != null && password != null)
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
                    Convert.ToBase64String(Encoding.GetEncoding("ISO-8859-1")
                        .GetBytes(username + ":" + password)));

            client.BaseAddress = new Uri(request);
            return client.GetAsync(request).Result.Content.ReadAsStringAsync().Result;
        }
        catch
        {
            return string.Empty;
        }
    }
}

public interface IAuthentication
{
    public Task<bool> Authenticate(string username, string password, bool rememberMe);
}