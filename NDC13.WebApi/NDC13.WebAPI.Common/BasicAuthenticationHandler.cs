using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace NDC13.WebAPI.Common
{
    public class BasicAuthenticationHandler : DelegatingHandler
    {
        private readonly string _realm;
        private readonly Func<string, string, bool> _validate;

        public BasicAuthenticationHandler(string realm, Func<string, string, bool> validate)
        {
            _realm = realm;
            _validate = validate;
        }

        async protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (HasAuthorizationHeaderWithBasicScheme(request))
            {
                var username = TryExtractAndValidateCredentials(
                                    request.Headers.Authorization.Parameter);
                if (username != null)
                    SetPrincipal(request, username);
                else
                    return UnauthorizedResponseMessage(_realm);
            }
            var response = await base.SendAsync(request, cancellationToken);
            if (response.StatusCode == HttpStatusCode.Unauthorized)
                response.Headers.WwwAuthenticate.Add(
                    new AuthenticationHeaderValue("Basic", string.Format("realm={0}", _realm)));
            return response;
        }

        private static HttpResponseMessage UnauthorizedResponseMessage(string realm)
        {
            var resp = new HttpResponseMessage(HttpStatusCode.Unauthorized);
            resp.Headers.WwwAuthenticate.Add(
                new AuthenticationHeaderValue("Basic", string.Format("realm={0}", realm)));
            return resp;
        }

        private void SetPrincipal(HttpRequestMessage request, string username)
        {
            var principal = new ClaimsPrincipal(
                new ClaimsIdentity(new Claim[]{
                    new Claim(ClaimTypes.NameIdentifier,username)
                }, "basic"));
            Thread.CurrentPrincipal = principal;
            if (HttpContext.Current != null)
            {
                HttpContext.Current.User = principal;
            }
        }

        private bool HasAuthorizationHeaderWithBasicScheme(HttpRequestMessage request)
        {
            return request.Headers.Authorization != null
                   && request.Headers.Authorization.Scheme.Equals("Basic", StringComparison.InvariantCultureIgnoreCase);
        }

        private string TryExtractAndValidateCredentials(string basicCredentials)
        {
            string pair;
            try
            {
                pair = Encoding.ASCII.GetString(Convert.FromBase64String(basicCredentials));
            }
            catch (FormatException)
            {
                return null;
            }
            catch (ArgumentException)
            {
                return null;
            }
            var ix = pair.IndexOf(':');
            if (ix == -1) return null;
            var username = pair.Substring(0, ix);
            var pw = pair.Substring(ix + 1);
            return _validate(username, pw) ? username : null;
        }
    }
}
