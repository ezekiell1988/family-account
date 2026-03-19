using System.Text;
using Hangfire.Dashboard;

namespace FamilyAccountApi.Hangfire;

public sealed class HangfireBasicAuthFilter : IDashboardAuthorizationFilter
{
    private const string AdminUsername = "admin";
    private const string AdminPassword = "12345";

    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        var authHeader = httpContext.Request.Headers.Authorization.ToString();

        if (!authHeader.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
        {
            Challenge(httpContext);
            return false;
        }

        string credentials;
        try
        {
            credentials = Encoding.UTF8.GetString(
                Convert.FromBase64String(authHeader["Basic ".Length..].Trim()));
        }
        catch
        {
            Challenge(httpContext);
            return false;
        }

        var parts = credentials.Split(':', 2);
        if (parts.Length != 2)
        {
            Challenge(httpContext);
            return false;
        }

        var isValid = parts[0] == AdminUsername && parts[1] == AdminPassword;
        if (!isValid) Challenge(httpContext);

        return isValid;
    }

    private static void Challenge(HttpContext ctx)
    {
        ctx.Response.StatusCode = 401;
        ctx.Response.Headers.WWWAuthenticate = "Basic realm=\"Hangfire Dashboard\"";
    }
}
