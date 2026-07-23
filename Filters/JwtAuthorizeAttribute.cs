using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using JobOnlineAPI.Services;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;

namespace JobOnlineAPI.Filters
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class JwtAuthorizeAttribute : Attribute, IAsyncActionFilter
    {
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var jwtTokenService = context.HttpContext.RequestServices.GetRequiredService<IJwtTokenService>();
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<JwtAuthorizeAttribute>>();

            string? authHeader = context.HttpContext.Request.Headers.Authorization;
            string token;

            if (!string.IsNullOrWhiteSpace(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                token = authHeader["Bearer ".Length..].Trim();
            }
            else
            {
                var cookieToken = context.HttpContext.Request.Cookies["admin_token"]
                               ?? context.HttpContext.Request.Cookies["auth_token"];
                if (string.IsNullOrWhiteSpace(cookieToken))
                {
                    Reject(context, StatusCodes.Status401Unauthorized);
                    return;
                }
                token = cookieToken;
            }

            try
            {
                var validated = await jwtTokenService.ValidateTokenAsync(token);
                var identity = new ClaimsIdentity(validated.Claims, "jwt");
                var principal = new ClaimsPrincipal(identity);

                // attach to HttpContext.User
                context.HttpContext.User = principal;
            }
            catch (SecurityTokenExpiredException)
            {
                context.HttpContext.Response.Headers["Token-Expired"] = "true";
                Reject(context, StatusCodes.Status401Unauthorized);
                return;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Token validation failed");
                Reject(context, StatusCodes.Status401Unauthorized);
                return;
            }

            await next();
        }

        private void Reject(ActionExecutingContext context, int statusCode)
        {
            context.Result = new ObjectResult(new { message = "Unauthorized" })
            {
                StatusCode = statusCode
            };
        }
    }
}
