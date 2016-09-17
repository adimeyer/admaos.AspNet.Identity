using System.Security.Claims;
using System.Web.Mvc;

namespace admaos.AspNet.Identity.RavenDB
{
    // http://stackoverflow.com/questions/19363809/mvc5-claims-version-of-the-authorize-attribute

    public class ClaimsAuthorizeAttribute : AuthorizeAttribute
    {
        private readonly string _claimType;
        private readonly string _claimValue;

        public ClaimsAuthorizeAttribute(string type, string value)
        {
            _claimType = type;
            _claimValue = value;
        }

        public override void OnAuthorization(AuthorizationContext filterContext)
        {
            ClaimsPrincipal user = filterContext.HttpContext.User as ClaimsPrincipal;
            if (user != null && user.HasClaim(_claimType, _claimValue))
            {
                base.OnAuthorization(filterContext);
            }
            else
            {
                HandleUnauthorizedRequest(filterContext);
            }
        }
    }
}