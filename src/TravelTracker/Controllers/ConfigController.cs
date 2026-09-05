using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Security.Claims;
using TravelTracker.Helpers;
using TravelTracker.Services;
using RouteAttribute = Microsoft.AspNetCore.Mvc.RouteAttribute;

namespace TravelTracker.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]

public class ConfigController(ILocationService locationService, IAuthenticationService authenticationService, ILogger<LocationsController> logger, IConfiguration config) : ControllerBase
{
    private readonly ILocationService _locationService = locationService;
    private readonly IAuthenticationService _authenticationService = authenticationService;
    private readonly ILogger<LocationsController> _logger = logger;
    private readonly IConfiguration _config = config;
    private readonly IHttpContextAccessor? context = null;

    /// <summary>
    /// Echoes configuration settings into the log for an admin to verify...
    /// </summary>
    /// <returns>User Name</returns>
    [HttpGet]
    public string Get()
    {
        string userName = "Unknown";
        bool isAdmin = false;
        try
        {
            userName = GetUserName();
            isAdmin = IsAdmin();
            Console.WriteLine($"User {userName} called config api. IsAdmin: {isAdmin}");
        }
        catch (Exception ex)
        {
            var msg = Utilities.GetExceptionMessage(ex);
            Console.WriteLine($"Error in ConfigController.Get - UserInfo: {msg}");
        }

        try
        {
            var sqlDefaultConnection = _config["AppSettings:DefaultConnection"];
            Console.WriteLine($"AppSettings.DefaultConnection={Utilities.SanitizeConnection(sqlDefaultConnection ?? string.Empty)}");
            var environmentName = _config["AppSettings:EnvironmentName"];
            Console.WriteLine($"AppSettings.EnvironmentName={environmentName}");
        }
        catch (Exception ex)
        {
            var msg = Utilities.GetExceptionMessage(ex);
            Console.WriteLine($"Error in ConfigController.Get - AppSettings: {msg}");
        }

        try
        {
            var openaiEndpoint = _config["AppSettings:AzureOpenAI:Chat:Endpoint"];
            var openaiDeploymentName = _config["AppSettings:AzureOpenAI:Chat:DeploymentName"];
            var openaiApiKey = _config["AppSettings:AzureOpenAI:Chat:ApiKey"];
            var openaiApiKeyMask = string.IsNullOrEmpty(openaiApiKey)
                ? "(0 bytes)"
                : $"{(openaiApiKey.Length > 3 ? openaiApiKey[..3] : openaiApiKey)}... (~{openaiApiKey.Length} bytes)";
            var openaiMaxTokens = int.TryParse(_config["AppSettings:AzureOpenAI:Chat:MaxTokens"], out var parsedMaxTokens) ? parsedMaxTokens : 300;
            var openaiTemperature = float.TryParse(_config["AppSettings:AzureOpenAI:Chat:Temperature"], out var parsedTemperature) ? parsedTemperature : 0.7f;
            var openaiTopP = float.TryParse(_config["AppSettings:AzureOpenAI:Chat:TopP"], out var topP) ? topP : 0.95f;
            Console.WriteLine($"AppSettings:AzureOpenAI:Chat:Endpoint={openaiEndpoint}");
            Console.WriteLine($"AppSettings:AzureOpenAI:Chat:DeploymentName={openaiDeploymentName}");
            Console.WriteLine($"AppSettings:AzureOpenAI:Chat:ApiKey={openaiApiKeyMask}");
            Console.WriteLine($"AppSettings:AzureOpenAI:Chat:MaxTokens={openaiMaxTokens}");
            Console.WriteLine($"AppSettings:AzureOpenAI:Chat:Temperature={openaiTemperature}");
            Console.WriteLine($"AppSettings:AzureOpenAI:Chat:TopP={openaiTopP}");

            var openaiImageEndpoint = _config["AppSettings:AzureOpenAI:Image:Endpoint"];
            var openaiImageDeploymentName = _config["AppSettings:AzureOpenAI:Image:DeploymentName"];
            var openaiImageApiKey = _config["AppSettings:AzureOpenAI:Image:ApiKey"];
            var openaiImageApiKeyMask = string.IsNullOrEmpty(openaiImageApiKey)
                ? "(0 bytes)"
                : $"{(openaiImageApiKey.Length > 3 ? openaiImageApiKey[..3] : openaiImageApiKey)}... (~{openaiImageApiKey.Length} bytes)";
            Console.WriteLine($"AppSettings:AzureOpenAI:Image:Endpoint={openaiImageEndpoint}");
            Console.WriteLine($"AppSettings:AzureOpenAI:Image:DeploymentName={openaiImageDeploymentName}");
            Console.WriteLine($"AppSettings:AzureOpenAI:Image:ApiKey={openaiImageApiKeyMask}");
        }
        catch (Exception ex)
        {
            var msg = Utilities.GetExceptionMessage(ex);
            Console.WriteLine($"Error in ConfigController.Get - AzureOpenAI: {msg}");
        }

        try
        {
            var assemblyDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? AppContext.BaseDirectory;
            var buildInfoFile = Path.Combine(assemblyDirectory, "buildinfo.json");
            if (System.IO.File.Exists(buildInfoFile))
            {
                using var r = new StreamReader(buildInfoFile);
                var buildInfoData = r.ReadToEnd();
                var buildInfoObject = JsonConvert.DeserializeObject<BuildInfo>(buildInfoData);
                if (buildInfoObject is not null)
                {
                    Console.WriteLine($"build.BranchName={buildInfoObject.BranchName}");
                    Console.WriteLine($"build.BuildDate={buildInfoObject.BuildDate}");
                    Console.WriteLine($"build.BuildId={buildInfoObject.BuildId}");
                    Console.WriteLine($"build.BuildNumber={buildInfoObject.BuildNumber}");
                    Console.WriteLine($"build.BuildCommitHashNumber={buildInfoObject.CommitHash}");
                }
            }
            else
            {
                Console.WriteLine($"{buildInfoFile} not found...!");
            }
        }
        catch (Exception ex)
        {
            var msg = Utilities.GetExceptionMessage(ex);
            Console.WriteLine($"Error in ConfigController.Get - BuildInfo: {msg}");
        }

        return userName;
    }
    #region Auth Helpers
    /// <summary>
    /// Returns User Name if logged on, UNKNOWN if not
    /// </summary>
    /// <returns>User Name</returns>
    protected string GetUserName()
    {
        var currentName = context?.HttpContext?.User?.Identity?.Name ?? "UNKNOWN";
        if (currentName == "UNKNOWN")
        {
            currentName = "BOGUS"; //  "lyle@luppes.com";
        }
        var domainIndicator = currentName.IndexOf("#");
        if (domainIndicator > 0)
        {
            currentName = currentName.Substring(domainIndicator + 1, currentName.Length - domainIndicator - 1);
        }
        if ((currentName.StartsWith("lyleluppes", StringComparison.CurrentCultureIgnoreCase) || currentName.StartsWith("lyle.luppes", StringComparison.CurrentCultureIgnoreCase) || currentName.StartsWith("lyle@lyleluppes", StringComparison.CurrentCultureIgnoreCase))
         && (currentName.EndsWith("gmail.com", StringComparison.CurrentCultureIgnoreCase) || currentName.EndsWith("microsoft.com", StringComparison.CurrentCultureIgnoreCase)))
        {
            currentName = "lyle@luppes.com";
        }
        return currentName;
    }

    /// <summary>
    /// Returns UserId if logged on, empty if not
    /// </summary>
    /// <returns>UserId</returns>
    protected string GetUserId()
    {
        return context?.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
    }

    /// <summary>
    /// Returns true if user is in Admin Role, false if not or not logged in
    /// </summary>
    /// <returns>Is Admin</returns>
    protected bool IsAdmin()
    {
        var userName = context?.HttpContext?.User?.Identity?.Name;
        if (string.IsNullOrEmpty(userName) ||
            userName.Equals("UNKNOWN", StringComparison.OrdinalIgnoreCase) ||
            userName.Equals("UNDEFINED", StringComparison.OrdinalIgnoreCase))
        {
            return false;
            // return true;
        }
        else
        {
            return userName.Contains("luppes", StringComparison.Ordinal);
        }
        //if (currentUser != null)
        //{
        //    var isAdmin = currentUser.IsInRole("Admin");
        //    if (!isAdmin)
        //    {
        //        isAdmin = currentUser.HasClaim("groups", AppSettingsValues.AdminGroupId);
        //    }
        //    if (!isAdmin)
        //    {
        //        isAdmin = context.HttpContext.User.Identity.Name.ToLower().Contains("lyle@luppes.com");
        //    }
        //    return isAdmin;
        //}
        //return false;
    }
    #endregion

}