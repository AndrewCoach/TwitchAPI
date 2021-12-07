﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using TwitchAPI.Models;
using System.Net.Http.Json;
using TwitchAPI.ViewModels;
using TwitchAPI.Data;
using Microsoft.AspNetCore.Http;

namespace TwitchAPI.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IHttpClientFactory _clientFactory;
        private readonly ITwitchRepository _repository;
        private readonly App _app;

        public HomeController(ILogger<HomeController> logger, IHttpClientFactory clientFactory, ITwitchRepository repository)
        {
            _logger = logger;
            _clientFactory = clientFactory;
            _repository = repository;
            _app = _repository.GetApp();
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Registration(IFormCollection collection)
        {
            try
            {
                var container = new ScopeContainer();

                container.ScopesFormatted = Request.Form["CategoryIds"];

                if (string.IsNullOrEmpty(container.ScopesFormatted))
                {
                    // Return invalid view back
                    ModelState.AddModelError("ScopeList", "Please select at least one!!!");
                    return View("Login", container);
                }

                // Build the link here
                var redirectUrl = "https://id.twitch.tv/oauth2/authorize"
                    + "?response_type=code" +
                    "&client_id=" + _app.ClientId +
                    "&redirect_uri=" + _app.RedirectURI +
                    "&scope=" + container.ScopesFormatted.Replace(",", "%20") +
                    "&state= " + _app.Token;

                return new RedirectResult(redirectUrl);
            }
            catch
            {
                return View("Failure", "Something did not go quite right with the form submition. Wanna try it again?");
            }
        }

        public IActionResult Login()
        {
            var model = new ScopeContainer();
            return View(model);
        }

        public async Task<IActionResult> Redirection(string code)
        {
            // Get OAUTH2 token for user
            var request = new HttpRequestMessage(HttpMethod.Post, "https://id.twitch.tv/oauth2/token"
                + "?client_id=" + _app.ClientId
                + "&client_secret=" + _app.ClientSecret
                + "&code=" + code
                + "&grant_type=authorization_code"
                + "&redirect_uri=" + _app.RedirectURI);

            var client = _clientFactory.CreateClient();

            var response = await client.SendAsync(request);

            var newUser = new User();
            newUser.OAuthCode = code;

            if (response.IsSuccessStatusCode)
            {
                var userTokenObject = await response.Content.ReadFromJsonAsync<UserToken>();

                newUser.UserToken = userTokenObject.AccessToken;
                newUser.RefreshToken = userTokenObject.RefreshToken;
                newUser.ExpiresIn = TimeSpan.FromSeconds(userTokenObject.ExpiresIn);

                // Get user ID
                var userIDrequest = new HttpRequestMessage(HttpMethod.Get,
                "https://api.twitch.tv/helix/users");
                userIDrequest.Headers.Add("Authorization", "Bearer " + newUser.UserToken);
                userIDrequest.Headers.Add("Client-Id", _app.ClientId);

                var userResponse = await client.SendAsync(userIDrequest);

                var users = await userResponse.Content.ReadFromJsonAsync<UserInfos>();
                var user = users.UserInfoList.FirstOrDefault();
                if (user != null)
                {
                    newUser.UserId = user.Id;

                    // User is already in the database
                    var dbUser = _repository.GetUserByUserId(newUser.UserId);
                    if (dbUser != null)
                    {
                        dbUser.ExpiresIn = newUser.ExpiresIn;
                        dbUser.OAuthCode = newUser.OAuthCode;
                        dbUser.UserToken = newUser.UserToken;
                        dbUser.UserId = newUser.UserId;
                        //dbUser.Scopes = userTokenObject.Scope.ConvertAll(conv => (Scope)Enum.Parse(enumType: typeof(Scope), conv.Replace(':', ' ')));
                        _repository.SaveChanges();
                    }
                    else
                    {
                        // dbUser is NULL
                        //newUser.Scopes = userTokenObject.Scope.ConvertAll(conv => (Scope)Enum.Parse(enumType: typeof(Scope), conv.Replace(':', ' ')));
                        _repository.CreateUser(newUser);
                        _repository.SaveChanges();
                    }
                }
                else
                {
                    // Getting a user ID for this user failed!
                    return View("Failure",
                        "User ID retrieval for this user failed! Try to change available scopes.");
                }
            }
            else
            {
                // OAUTH2 validation failed!
                return View("Failure", "OAUTH2 token retrieval for this user failed! Try to validate this app again.");
            }

            return View("Index");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
