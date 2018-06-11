using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SocialNetwork.Web.Models;
using System.Net.Http;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using IdentityModel.Client;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace SocialNetwork.Web.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        [Authorize]
        public IActionResult About()
        {
            ViewData["Message"] = "Your application description page.";

            return View();
        }

        public IActionResult Contact()
        {
            ViewData["Message"] = "Your contact page.";

            return View();
        }

        [Authorize]
        public async Task<IActionResult> Shouts()
        {
            await RefreshTokens();

            var token = await AuthenticationHttpContextExtensions.GetTokenAsync(HttpContext, "access_token");

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                var shoutsResponse = await (await client.GetAsync($"http://localhost:33918/api/shouts")).Content.ReadAsStringAsync();

                var shouts = JsonConvert.DeserializeObject<Shout[]>(shoutsResponse);

                return View(shouts);
            }
        }

        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        public async Task Logout()
        {
            await AuthenticationHttpContextExtensions.SignOutAsync(HttpContext,"Cookies");
            await AuthenticationHttpContextExtensions.SignOutAsync(HttpContext, "oidc");
        }

        private async Task RefreshTokens()
        {
            var authorizationServerInformation = await DiscoveryClient.GetAsync("http://localhost:59418");
            var client = new TokenClient(authorizationServerInformation.TokenEndpoint, "socialnetwork_code", "secret");

            var refreshToken = await AuthenticationHttpContextExtensions.GetTokenAsync(HttpContext, "refresh_token");
            var tokenResponse = await client.RequestRefreshTokenAsync(refreshToken);

            var identityToken = await AuthenticationHttpContextExtensions.GetTokenAsync(HttpContext, "id_token");
            var expiresAt = DateTime.UtcNow + TimeSpan.FromSeconds(tokenResponse.ExpiresIn);

            var tokens = new[] {
                new AuthenticationToken
                {
                    Name = OpenIdConnectParameterNames.IdToken,
                    Value = identityToken
                },
                new AuthenticationToken
                {
                    Name = OpenIdConnectParameterNames.AccessToken,
                    Value = tokenResponse.AccessToken
                },
                new AuthenticationToken
                {
                    Name = OpenIdConnectParameterNames.RefreshToken,
                    Value = tokenResponse.RefreshToken
                },
                new AuthenticationToken
                {
                    Name = "expires_at",
                    Value = expiresAt.ToString("o",System.Globalization.CultureInfo.InvariantCulture)
                }
            };
            var authenticationInformation = await AuthenticationHttpContextExtensions.AuthenticateAsync(HttpContext, "Cookies");
            authenticationInformation.Properties.StoreTokens(tokens);

            await AuthenticationHttpContextExtensions.SignInAsync(HttpContext,"Cookies", 
                authenticationInformation.Principal,authenticationInformation.Properties);

        }

    }
}
