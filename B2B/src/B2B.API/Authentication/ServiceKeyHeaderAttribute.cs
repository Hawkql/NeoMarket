using Microsoft.AspNetCore.Mvc.ActionConstraints;

namespace B2B.Api.Authentication
{
    public sealed class RequireServiceKeyHeaderAttribute : Attribute, IActionConstraint
    {
        private readonly bool _mustBePresent;
        public RequireServiceKeyHeaderAttribute(bool mustBePresent) => _mustBePresent = mustBePresent;

        public int Order => 0;

        public bool Accept(ActionConstraintContext context)
        {
            var hasHeader = context.RouteContext.HttpContext
                .Request.Headers.ContainsKey("X-Service-Key");
            return _mustBePresent ? hasHeader : !hasHeader;
        }
    }
}
