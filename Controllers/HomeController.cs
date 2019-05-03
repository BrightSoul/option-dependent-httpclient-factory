using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using OptionDependentHttpclientFactory.Models;
using OptionDependentHttpclientFactory.Models.Services.Infrastructure;

namespace OptionDependentHttpclientFactory.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            ViewBag.Url = "http://example.org/";
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> Index([FromForm] string url, [FromServices] IRequestSender requestSender)
        {
            var request = new HttpRequestMessage();
            request.RequestUri = new Uri(url);
            request.Method = HttpMethod.Get;

            try {
                HttpResponseMessage response = await requestSender.SendRequest(request);
                ViewBag.Message = $"La richiesta si è conclusa con lo status code: {response.StatusCode}";
            } catch (Exception exc) {
                ViewBag.Message = $"La richiesta ha prodotto un errore: {exc.Message}";
            }
            
            ViewBag.Url = url;
            return View();
        }
    }
}
