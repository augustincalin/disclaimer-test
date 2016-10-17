using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net;

namespace WebApplication5
{

    public class MyMiddleware
    {
        private readonly RequestDelegate _next;
        private const string DISCLAIMER_PAGE = "/disclaimer.html";
        private const string RETURNURL_PARAM = "r=";
        public MyMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {

            if (ShouldDisplayDisclaimer(context))
            {
                context.Response.StatusCode = 303;
                context.Response.Headers["location"] = DISCLAIMER_PAGE + "?" + RETURNURL_PARAM + context.Request.Path.Value;
                return;
            }
            if (DisclaimerAccepted(context)){

                string originalRequestedPage = GetOriginalPage(context);
                context.Response.StatusCode = 303;
                context.Response.Headers["location"] = originalRequestedPage;
                context.Response.Cookies.Append("Accept", "true", new CookieOptions {Expires=new DateTimeOffset(DateTime.Now.AddYears(30)) });
                return;
            }
            await _next.Invoke(context);

        }

        private bool DisclaimerAccepted(HttpContext context)
        {
            bool disclaimerAccepted = false;
            if (DisclaimerPageRequested(context))
            {
                
                disclaimerAccepted = context.Request.HasFormContentType && context.Request.Form.Any(f => f.Key == "agree");
            }

            return disclaimerAccepted;
        }

        private string GetOriginalPage(HttpContext context)
        {
            string originalPage = null;
            string referer = context.Request.Headers["Referer"].FirstOrDefault();
            if (!string.IsNullOrEmpty(referer))
            {
                originalPage = referer.Substring(referer.LastIndexOf(RETURNURL_PARAM, StringComparison.Ordinal) + RETURNURL_PARAM.Length);
            }
            return originalPage;
        }

        private bool ShouldDisplayDisclaimer(HttpContext context)
        {
            bool shouldDisplay = true;
            if (DisclaimerPageRequested(context))
            {
                shouldDisplay = false;
            }
            else
            {
                var cookie = context.Request.Cookies.SingleOrDefault(c => c.Key == "Accept");
                bool cookieExists = cookie.Key != null;
                if (cookieExists)
                {
                    bool accepted = cookie.Value == "true";
                    shouldDisplay = !accepted;
                }
                else
                {
                    shouldDisplay = VerifyIPAddress(context.Connection.LocalIpAddress);
                }
            }


            return shouldDisplay;
        }

        private static bool DisclaimerPageRequested(HttpContext context)
        {
            return context.Request.Path.Value.EndsWith(DISCLAIMER_PAGE, StringComparison.Ordinal);
        }

        private bool VerifyIPAddress(IPAddress localIpAddress)
        {
            return localIpAddress.Equals(new IPAddress(new byte[] { 127, 0, 0, 1 }));
        }
    }
}
