using System.Net.Http.Headers;
using System.Text;
using System.Xml.Linq;
using Microsoft.AspNetCore.Identity;
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

    public UpolAuthentication(ApplicationDbContext context, SignInManager<ApplicationUser> signInManager,
        IConfiguration configuration)
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

    //If user logs in for the first time and has different STAG and AD password -> STAG password must be used
    public async Task<UpolAuthenticationResponse> Authenticate(string username, string passwordActiveDirectory, string? passwordStag, bool rememberMe)
    {
        username = NormalizeUsername(username);

        //Determines if user is logging in for the first time
        var user = _context.Users.FirstOrDefault(x => x.UserName!.Equals(username));

        if (user != null)
        {
            return await SignInUser(user, passwordActiveDirectory, rememberMe);
        }

        return await UnknownUser(username, passwordActiveDirectory, passwordStag, rememberMe);
    }

    private async Task<UpolAuthenticationResponse> SignInUser(ApplicationUser user, string passwordActiveDirectory, bool rememberMe)
    {
        //Determines if user has local account or upol account
        if (user.PasswordHash != null)
        {
            if (await LocalSignIn(user.UserName!, passwordActiveDirectory, rememberMe))
            {
                return UpolAuthenticationResponse.Success;
            }
            return UpolAuthenticationResponse.WrongCredentialsLocal;
        }
       
        if (UpolActiveDirectoryAuthenticate(user.UserName!, passwordActiveDirectory))
        {
            await _signInManager.SignInAsync(user, rememberMe);
            return UpolAuthenticationResponse.Success;
        }
        return UpolAuthenticationResponse.WrongCredentialsActiveDirectory;
    }
    
    private async Task<bool> LocalSignIn(string username, string passwordActiveDirectory, bool rememberMe)
    {
        return (await _signInManager.PasswordSignInAsync(username, passwordActiveDirectory, rememberMe, false)).Succeeded;
    }

    private bool UpolActiveDirectoryAuthenticate(string username, string passwordActiveDirectory)
    {
        //Works only under windows, ssl 
        try
        {
            var endpoint = new LdapDirectoryIdentifier(_configuration["UpolActiveDirectory:IP"] ?? string.Empty,
                int.Parse(_configuration["UpolActiveDirectory:Port"] ?? string.Empty), true, false);
            using var ldap = new LdapConnection(endpoint,
                new NetworkCredential(_configuration["UpolActiveDirectory:Dn"] + "\\" + username, passwordActiveDirectory))
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
    
    private async Task<UpolAuthenticationResponse> UnknownUser(string username, string passwordActiveDirectory, string? passwordStag, bool rememberMe)
    {
        if (UpolActiveDirectoryAuthenticate(username, passwordActiveDirectory))
        {
            var user = await CreateUpolUser(username, passwordStag ?? passwordActiveDirectory);
            
            //User has different stag and ad password and stag password was wrong or not provided, or stag is down
            if (user is null)
                return UpolAuthenticationResponse.WrongCredentialsStag;
            
            await _signInManager.SignInAsync(user, rememberMe);
            return UpolAuthenticationResponse.Success;
        }
        //User used wrong credentials or UPOL AD is down
        return UpolAuthenticationResponse.WrongCredentialsActiveDirectory;
    }
    
    private async Task<ApplicationUser?> CreateUpolUser(string username, string passwordStag)
    {
        string stagId;
        stagId = GetStudentStagId(username);
        if (stagId.Length > 0)
            return await CreateStudent(username, passwordStag, stagId);
        
        stagId = GetTeacherStagId(username);
        if (stagId.Length > 0)
            return await CreateTeacher(username, passwordStag, stagId);
        
        //Username is not in stag
        return null;
    }

    private async Task<ApplicationUser?> CreateStudent(string username, string password, string stagId)
    {
        var request = _stagServicesStudentInfoByOsCislo + stagId;
        var user = await CreateUser(request, username, password);
        if (user is null) return null;
        
        await RoleHelper.AssignRoles(user, Role.Student, _context);
        return user;
    }

    private async Task<ApplicationUser?> CreateTeacher(string username, string password, string stagId)
    {
        var request = _stagServicesTeacherInfoByIdno + stagId;
        var user = await CreateUser(request, username, password);
        if (user is null) return user;
        
        await RoleHelper.AssignRoles(user, Role.Teacher, _context);
        return user;
    }

    private async Task<ApplicationUser?> CreateUser(string request, string username, string password)
    {
        if (!GetStagInfo(request, username, password, out var firstname, out var lastname, out var email))
            return null; //Couldn't get user info from stag

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
            //If response is null here, stag credentials are wrong
            
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
}

public interface IAuthentication
{
    public Task<UpolAuthenticationResponse> Authenticate(string username, string passwordActiveDirectory, string? passwordStag, bool rememberMe);
}

public enum UpolAuthenticationResponse
{
    Success,
    WrongCredentialsLocal,
    WrongCredentialsActiveDirectory,
    WrongCredentialsStag,
}