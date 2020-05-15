using System.Collections.Generic;
using AddvalsApi.Data;
using AddvalsApi.Dtos;
using AddvalsApi.Model;
using AutoMapper;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;

using Microsoft.AspNetCore.Http;
using System.Net.Http;
using Newtonsoft.Json;
using System.Text;
using System;
using System.Threading.Tasks;
using System.Net;

namespace AddvalsApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SkyTapController : ControllerBase
    {
        [HttpPost]
        public async Task<SkytapModelDeux> addSkytap(UserCreateDto user)
        {
            Console.WriteLine("1TESTTTSTSTSTSTSTSTESTTTSTSTSTSTSTS");
            SkytapModel skytapModel = new SkytapModel
            {
                account_role = "admin",
                login_name = user.Login,
                email = user.Email,
            };
            Console.WriteLine("2TESTTTSTSTSTSTSTSTESTTTSTSTSTSTSTS");

            SkytapModelDeux SkyRep;
            Console.WriteLine("3TESTTTSTSTSTSTSTSTESTTTSTSTSTSTSTS");

            using (var httpClient = new HttpClient())
            {
                var authString = Convert.ToBase64String(Encoding.UTF8.GetBytes("elayadi.ilias@gmail.com:3e6d13cc03d0276a48619440f61f6269cf238ed0"));
                httpClient.DefaultRequestHeaders.Clear();
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authString);
                httpClient.DefaultRequestHeaders.Add("Accept", "application/json");

                StringContent content = new StringContent(JsonConvert.SerializeObject(skytapModel), Encoding.UTF8, "application/json");

                using (var response = await httpClient.PostAsync("https://cloud.skytap.com/users.json", content))
                {
                    string apiResponse = await response.Content.ReadAsStringAsync();
                    SkyRep = JsonConvert.DeserializeObject<SkytapModelDeux>(apiResponse);
                }
            }
            return SkyRep;
        }


        [HttpPut]
        public async Task<SkytapModelDeux> UpdatePwdSkytap(SkytapModelDeux SkyRep, String password)
        {
            using (var httpClient = new HttpClient())
            {
                var authString = Convert.ToBase64String(Encoding.UTF8.GetBytes("elayadi.ilias@gmail.com:3e6d13cc03d0276a48619440f61f6269cf238ed0"));
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authString);

                httpClient.DefaultRequestHeaders.Add("Accept", "application/json");

                //Console.WriteLine($"https://cloud.skytap.com/users/{SkyRep.id}?password={password}");
                using (var response = await httpClient.PutAsync($"https://cloud.skytap.com/users/{SkyRep.id}?password={password}", null))
                {
                    //Console.WriteLine(response.StatusCode);
                    string apiResponse = await response.Content.ReadAsStringAsync();
                    //Console.WriteLine(apiResponse);           
                    // SkyRep = JsonConvert.DeserializeObject<SkytapModelDeux>(apiResponse);
                }
            }
            return null;
        }


        [HttpGet]
        public async Task<SkytapModelToken> GetToken(UserCreateDto user)
        {
            //  SkytapModelDeux SkyRep;
            SkytapModelToken skytapModelToken;

            using (var httpClient = new HttpClient())
            {
                var authString = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{user.Email}:{user.Password}"));
                httpClient.DefaultRequestHeaders.Clear();
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authString);
                httpClient.DefaultRequestHeaders.Add("Accept", "application/json");

                // StringContent content = new StringContent(JsonConvert.SerializeObject(), Encoding.UTF8, "application/json");

                using (var response = await httpClient.GetAsync("https://cloud.skytap.com/account/api_token"))
                {
                    //Console.WriteLine(response.StatusCode);
                    string apiResponse = await response.Content.ReadAsStringAsync();
                    skytapModelToken = JsonConvert.DeserializeObject<SkytapModelToken>(apiResponse);
                    //Console.WriteLine("token :" + apiResponse);
                }
            }
            return skytapModelToken;
        }


        [HttpPost]
        public async Task<SkytapDataEnviroModel> CreateEnviroSkytap(UserCreateDto user, string token)
        {
            SkytapDataEnviroModel skytapDataEnviroModel;

            var payload1 = new Dictionary<string, int>
            {
              {"template_id", 1888623},
            };

            //Console.WriteLine(user.Email + "  " +token);

            using (var httpClient = new HttpClient())
            {
                var authString2 = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{user.Email}:{token}"));
                httpClient.DefaultRequestHeaders.Clear();
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authString2);

                StringContent content = new StringContent(JsonConvert.SerializeObject(payload1), Encoding.UTF8, "application/json");

                using (var response = await httpClient.PostAsync("https://cloud.skytap.com/configurations.json", content))
                {
                    //Console.WriteLine(response.StatusCode);
                    string apiResponse = await response.Content.ReadAsStringAsync();
                    skytapDataEnviroModel = JsonConvert.DeserializeObject<SkytapDataEnviroModel>(apiResponse);
                    //Console.WriteLine("enviro :" + apiResponse);
                }
            }
            return skytapDataEnviroModel;
        }

    }
}