using System.Net.Http;

namespace OptionDependentHttpclientFactory.Models.Options
{
    public class EndpointOption
    {
        public string Name { get; set; }
        public string Selector { get; set; }
        public string Proxy { get; set; }
        public bool Matches(HttpRequestMessage request)
        {
            if (string.IsNullOrEmpty(Selector))
            {
                return true;
            }
            return request.RequestUri.ToString().Contains(Selector);
        }
    }
}