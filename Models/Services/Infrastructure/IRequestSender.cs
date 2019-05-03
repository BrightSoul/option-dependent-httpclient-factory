using System.Net.Http;
using System.Threading.Tasks;

namespace OptionDependentHttpclientFactory.Models.Services.Infrastructure
{
    public interface IRequestSender
    {
        Task<HttpResponseMessage> SendRequest(HttpRequestMessage request);
    }
}