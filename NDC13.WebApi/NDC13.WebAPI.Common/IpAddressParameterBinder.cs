using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Metadata;

namespace NDC13.WebAPI.Common
{
    public class IPAddressParameterBinding : HttpParameterBinding
    {
        public IPAddressParameterBinding(HttpParameterDescriptor descriptor)
            : base(descriptor)
        {
        }

        public override Task ExecuteBindingAsync(ModelMetadataProvider metadataProvider, HttpActionContext actionContext,
                                                 CancellationToken cancellationToken)
        {
            var request = actionContext.Request;
            var ipString = GetClientIpAddressFrom(request);
            if (ipString != null)
            {
                SetValue(actionContext, IPAddress.Parse(ipString));
                return Task.FromResult<object>(null);
            }
            var tcs = new TaskCompletionSource<object>();
            tcs.SetException(new HttpResponseException(HttpStatusCode.Forbidden));
            return tcs.Task;
        }

        // http://www.strathweb.com/2013/05/retrieving-the-clients-ip-address-in-asp-net-web-api/ 
        private string GetClientIpAddressFrom(HttpRequestMessage request)
        {
            const string HttpContext = "MS_HttpContext";
            const string RemoteEndpointMessage = "System.ServiceModel.Channels.RemoteEndpointMessageProperty";

            if (request.Properties.ContainsKey(HttpContext))
            {
                dynamic ctx = request.Properties[HttpContext];
                if (ctx != null)
                {
                    return ctx.Request.UserHostAddress;
                }
            }

            if (request.Properties.ContainsKey(RemoteEndpointMessage))
            {
                dynamic remoteEndpoint = request.Properties[RemoteEndpointMessage];
                if (remoteEndpoint != null)
                {
                    return remoteEndpoint.Address;
                }
            }

            return null;
        }
    }
}
