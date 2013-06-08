using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.SelfHost;
using NDC13.WebAPI.Common;

namespace NDC13.WebAPI.SelfHost
{

    public class ProtectedResourceController : ApiController
    {
        [Authorize]
        public HttpResponseMessage Get()
        {
            var identity = User.Identity as ClaimsIdentity;
            return new HttpResponseMessage
                       {
                           Content =
                               new StringContent("Hello " +
                                                 identity.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value)
                       };
        }
    }

    public class WhatIpAmIController : ApiController
    {
        public HttpResponseMessage Get(IPAddress ip)
        {
            return new HttpResponseMessage
                   {
                       Content = new StringContent("You are " + ip)
                   };
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var config = new HttpSelfHostConfiguration("http://localhost:8080");
            config.Routes.MapHttpRoute(
                "ApiDefault",
                "api/{controller}/{id}",
                new { id = RouteParameter.Optional }
                );

            config.MessageHandlers.Add(new BasicAuthenticationHandler("Web API book", (user, pw) => user == pw));

            config.ParameterBindingRules.Add(prm =>
                prm.ParameterType == typeof(IPAddress) ?
                    new IPAddressParameterBinding(prm)
                    : null);

            var server = new HttpSelfHostServer(config);
            server.OpenAsync().Wait();
            Console.WriteLine("Server is opened, press any key to continue");
            Console.ReadKey();
            server.CloseAsync().Wait();
        }
    }
}
