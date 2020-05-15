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
using System.Threading;

namespace AddvalsApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly IUserRepo _repository;
        private readonly IMapper _mapper;


        public UsersController(IUserRepo repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;

        }

        [HttpGet("/api/user")]
        public ActionResult<IEnumerable<UserReadDto>> GetAllUsers()
        {
            var users = _repository.GetAllUsers();
            return Ok(_mapper.Map<IEnumerable<UserReadDto>>(users));
        }

        [HttpGet("/api/user/{id}", Name = "GetUserById")]
        public ActionResult<UserReadDto> GetUserById(int id)
        {
            var user = _repository.GetUserById(id);

            if (user == null)
            {
                return NotFound();
            }

            return Ok(_mapper.Map<UserReadDto>(user));
        }



        [HttpPost("/api/user/signin")]
        public async Task<ActionResult<string>> UserSignIn(UserCreateDto user)
        {
            SkytapModelToken skytapModelToken = await GetToken(user);
            try
            {

                EnviroVmUrl enviroEtVm1 = await GetEnviroSkytap(user, skytapModelToken.api_token);
                await DeleteEnviroSkytap(user, skytapModelToken.api_token, enviroEtVm1.enviro);
                SkytapDataEnviroModel skytapDataEnviroModel = await CreateEnviroSkytap(user, skytapModelToken.api_token);
                Console.WriteLine("GET/DELETE/CREATE");
            }
            catch (System.Exception)
            {
                SkytapDataEnviroModel skytapDataEnviroModel = await CreateEnviroSkytap(user, skytapModelToken.api_token);
                Console.WriteLine("/CREATE");
            }

            EnviroVmUrl enviroVmUrl = await GetEnviroSkytap(user, skytapModelToken.api_token);

            await UpdateStatsSkytap(user, enviroVmUrl.enviro, skytapModelToken.api_token);

            Console.WriteLine("-----------------------------------------");
            Console.WriteLine("USER : " + user.Login);
            Console.WriteLine("TOKEN : " + skytapModelToken.api_token);
            Console.WriteLine("ENVIRO : " + enviroVmUrl.enviro);
            Console.WriteLine("VM : " + enviroVmUrl.vm);


            string ok = await CreateSharingSkytap(user, skytapModelToken.api_token, enviroVmUrl.enviro);

            SharingUrl sharingUrl = await GetSharing(user, skytapModelToken.api_token, enviroVmUrl.enviro);


            EnviroVmUrl urlFinal = await GetEnviroSkytap(user, skytapModelToken.api_token);


            await UpdateAccesSharingSkytap(user, skytapModelToken.api_token, enviroVmUrl.enviro,enviroVmUrl.vm, sharingUrl.id);

            //string url_final = $"https://cloud.skytap.com/configurations/{enviroVmUrl.enviro}/desktop?vm_id={enviroVmUrl.vm}";
            //https://cloud.skytap.com/vms/58c58bc3223782dea7438ad322c8beae/desktops?vm_id=22043645

            // string pageWeb = await GetWebPage(user, skytapModelToken.api_token);

            Console.WriteLine("-----------------------------------------");
            Console.WriteLine("URL : " + urlFinal.url);
            Console.WriteLine("-----------------------------------------");


            return Ok($"{{\"url\":\"{urlFinal.url}\"}}");

            //return Ok($"{{\"url\":\"{pageWeb}\"}}");

            //////////SkyTap//////////////
            //return CreatedAtRoute(nameof(GetUserById), new {Id = UserReadDto.Id}, UserReadDto); //Renvoi la route pour GetUserById
            //return Ok(UserReadDto);
        }


        [HttpGet]
        public async Task<string> GetWebPage(UserCreateDto user, string token)
        {
            string apiResponse;
            SkytapModelToken skytapModelToken = await GetToken(user);
            EnviroVmUrl enviroVmUrl = await GetEnviroSkytap(user, skytapModelToken.api_token);

            using (var httpClient = new HttpClient())
            {
                var authString2 = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{user.Login}:{token}"));
                httpClient.DefaultRequestHeaders.Clear();
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authString2);


                using (var response = await httpClient.GetAsync($"https://cloud.skytap.com/configurations/{enviroVmUrl.enviro}/desktop?vm_id={enviroVmUrl.vm}"))
                {
                    //Console.WriteLine(response.StatusCode);
                    apiResponse = await response.Content.ReadAsStringAsync();
                    Console.WriteLine(apiResponse);
                    // apiResponse = "<h1>ceci est un text</h1>";
                    //var pageWeb = JsonConvert.DeserializeObject<>(apiResponse);

                }
            }
            return apiResponse;
        }

        [HttpPost("/api/user")]
        public async Task<ActionResult<UserReadDto>> CreateUser(UserCreateDto user)
        {
            var userMap = _mapper.Map<UserModel>(user);

            // Console.WriteLine("password " + user.Password);

            _repository.CreatUser(userMap);

            _repository.SaveChanges();

            var UserReadDto = _mapper.Map<UserReadDto>(userMap);

            //////////SkyTap//////////////
            SkytapModelDeux sky = await addSkytap(user);

            await UpdatePwdSkytap(sky, user.Password);

            return Ok(sky);
            //////////SkyTap//////////////

            //return CreatedAtRoute(nameof(GetUserById), new {Id = UserReadDto.Id}, UserReadDto); //Renvoi la route pour GetUserById

            //return Ok(UserReadDto);
        }

        [HttpPut("/api/user/{id}")]
        public ActionResult UpdateUser(int id, UserUpdateDto user)
        {
            var UserToUpdate = _repository.GetUserById(id);

            if (UserToUpdate == null)
            {
                return NotFound();
            }

            _mapper.Map(user, UserToUpdate);

            _repository.UpdateUser(UserToUpdate);

            _repository.SaveChanges();

            return CreatedAtRoute(nameof(GetUserById), new { Id = UserToUpdate.Id }, UserToUpdate); //Renvoi la route pour GetUserById
        }

        [HttpPatch("/api/user/{id}")]
        public ActionResult PartialUpdateUser(int id, JsonPatchDocument<UserUpdateDto> patchDoc)
        {
            var UserToUpdate = _repository.GetUserById(id);

            if (UserToUpdate == null)
            {
                return NotFound();
            }

            var UserToPatch = _mapper.Map<UserUpdateDto>(UserToUpdate);

            patchDoc.ApplyTo(UserToPatch, ModelState);

            if (!TryValidateModel(UserToPatch))
            {
                return ValidationProblem(ModelState);
            }

            _mapper.Map(UserToPatch, UserToUpdate);

            _repository.UpdateUser(UserToUpdate);

            _repository.SaveChanges();

            return CreatedAtRoute(nameof(GetUserById), new { Id = UserToUpdate.Id }, UserToUpdate); //Renvoi la route pour GetUserById
        }


        [HttpDelete("/api/user/{id}")]
        public ActionResult DeletUserUser(int id)
        {
            var UserToDelete = _repository.GetUserById(id);

            if (UserToDelete == null)
            {
                return NotFound();
            }
            _repository.DeleteUser(UserToDelete);

            _repository.SaveChanges();

            return NoContent();
        }

        ////////////////////////////////////////////////////////////////////////////////////SkyTap//////////////////////////////////////////////////////////////////////////////////////////////////

        [HttpPost]
        public async Task<SkytapModelDeux> addSkytap(UserCreateDto user)
        {
            SkytapModel skytapModel = new SkytapModel
            {
                account_role = "admin",
                login_name = user.Login,
                email = user.Email,
            };

            SkytapModelDeux SkyRep;

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

        [HttpPut]
        public async Task<SkytapModelDeux> UpdateStatsSkytap(UserCreateDto user, String enviro, String token)
        {
            SkytapRunModel skytapRunModel = new SkytapRunModel
            {
                runstate = "running",
            };
            using (var httpClient = new HttpClient())
            {
                var authString = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{user.Login}:{token}"));
                httpClient.DefaultRequestHeaders.Clear();
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authString);
                httpClient.DefaultRequestHeaders.Add("Accept", "application/json");

                StringContent content = new StringContent(JsonConvert.SerializeObject(skytapRunModel), Encoding.UTF8, "application/json");


                using (var response = await httpClient.PutAsync($"https://cloud.skytap.com/configurations/{enviro}.json", content))
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
                var authString = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{user.Login}:{user.Password}"));
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
              {"template_id", 1899007},
            };

            //Console.WriteLine(user.Email + "  " +token);

            using (var httpClient = new HttpClient())
            {
                var authString2 = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{user.Login}:{token}"));
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

        [HttpPost]
        public async Task<string> CreateSharingSkytap(UserCreateDto user, string token, string enviro)
        {
            CreateSharing createSharing = new CreateSharing
            {
                name = $"Machine de {user.Login}",
            };

            using (var httpClient = new HttpClient())
            {
                var authString = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{user.Login}:{token}"));
                httpClient.DefaultRequestHeaders.Clear();
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authString);
                httpClient.DefaultRequestHeaders.Add("Accept", "application/json");

                StringContent content = new StringContent(JsonConvert.SerializeObject(createSharing), Encoding.UTF8, "application/json");

                using (var response = await httpClient.PostAsync($"https://cloud.skytap.com/v2/configurations/{enviro}/publish_sets.json", content))
                {
                    // Console.WriteLine("Create sharing = " + response.StatusCode);

                    string apiResponse = await response.Content.ReadAsStringAsync();
                    //SkyRep = JsonConvert.DeserializeObject<SkytapModelDeux>(apiResponse);

                }
            }
            return "Ok";
        }

        [HttpGet]
        public async Task<SharingUrl> GetSharing(UserCreateDto user, string token, string enviro)
        {
            //SkytapDataSharingModel skytapDataSharingModel;
            SharingUrl sharingUrl = new SharingUrl();

            using (var httpClient = new HttpClient())
            {
                var authString = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{user.Login}:{token}"));
                httpClient.DefaultRequestHeaders.Clear();
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authString);
                httpClient.DefaultRequestHeaders.Add("Accept", "application/json");

                // StringContent content = new StringContent(JsonConvert.SerializeObject(), Encoding.UTF8, "application/json");

                using (var response = await httpClient.GetAsync($"https://cloud.skytap.com/configurations/{enviro}/publish_sets.json"))
                {

                    // Console.WriteLine("get sharing = " + response.StatusCode);
                    string apiResponse = await response.Content.ReadAsStringAsync();
                    //Console.WriteLine("GET SHARING API REPONSE" + apiResponse);
                    var list = JsonConvert.DeserializeObject<List<SkytapDataSharingModel>>(apiResponse);

                    foreach (var sha in list)
                    {
                        sharingUrl.id = sha.id;
                        Console.WriteLine("VM SHARING : "+ sharingUrl.id);
                        foreach (var lis in sha.vms)
                        {
                            sharingUrl.url = lis.desktop_url;
                            break;
                        }
                        break;
                    }
                }
            }
            return sharingUrl;
        }

        [HttpPut]
        public async Task<SkytapModelDeux> UpdateAccesSharingSkytap(UserCreateDto user, string token, string enviro,string vm, string sharingId)
        {

/*             Access access = new Access();
            access.access = "run_and_use";
            Vm_ref vm_ref = new Vm_ref();
            vm_ref.vm_ref = $"https://cloud.skytap.com/vms/{vm}";

            Console.WriteLine($"https://cloud.skytap.com/vms/{vm}");
            UpdateAcces updateAcces = new UpdateAcces
            {
                vms = new Object[] { access, vm_ref }
            }; */

            var vmss = new Dictionary<string, string>
            {
              {"access", "run_and_use"},
              {"vm_ref", $"https://cloud.skytap.com/vms/{vm}"}              
            };

             UpdateAcces updateAcces = new UpdateAcces
            {
                vms = new Dictionary<string, string>[]{vmss}
            };


            using (var httpClient = new HttpClient())
            {
                var authString = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{user.Login}:{token}"));
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authString);
                httpClient.DefaultRequestHeaders.Add("Accept", "application/json");

                StringContent content = new StringContent(JsonConvert.SerializeObject(updateAcces), Encoding.UTF8, "application/json");
               
                using (var response = await httpClient.PutAsync($"https://cloud.skytap.com/configurations/{enviro}/publish_sets/{sharingId}.json", content))
                {
                    //Console.WriteLine(response.StatusCode);
                    string apiResponse = await response.Content.ReadAsStringAsync();
                    //Console.WriteLine(apiResponse);
                    // SkyRep = JsonConvert.DeserializeObject<SkytapModelDeux>(apiResponse);
                }
            }
            return null;
        }
        [HttpDelete]
        public async Task<OkResult> DeleteEnviroSkytap(UserCreateDto user, string token, String enviro)
        {

            using (var httpClient = new HttpClient())
            {
                var authString2 = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{user.Login}:{token}"));
                httpClient.DefaultRequestHeaders.Clear();
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authString2);

                using (var response = await httpClient.DeleteAsync($"https://cloud.skytap.com/configurations/{enviro}"))
                {
                    // Console.WriteLine(response.StatusCode);
                    //Console.WriteLine($"https://cloud.skytap.com/configurations/{enviro}");
                    string apiResponse = await response.Content.ReadAsStringAsync();
                    //var lalistIdEnviro = JsonConvert.DeserializeObject<List<EnviroIdModel>>(apiResponse);
                    //Console.WriteLine(apiResponse);
                }
            }
            return Ok();
        }



        [HttpGet]
        public async Task<EnviroVmUrl> GetEnviroSkytap(UserCreateDto user, string token)
        {
            //SkytapDataEnviroModel skytapDataEnviroModel;          
            EnviroIdModel enviro = new EnviroIdModel();
            String enviroId = "";

            //Console.WriteLine(user.Email + "  " +token);

            using (var httpClient = new HttpClient())
            {
                var authString2 = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{user.Login}:{token}"));
                httpClient.DefaultRequestHeaders.Clear();
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authString2);

                using (var response = await httpClient.GetAsync("https://cloud.skytap.com/configurations.json"))
                {
                    //Console.WriteLine(response.StatusCode);
                    string apiResponse = await response.Content.ReadAsStringAsync();
                    // Console.WriteLine(apiResponse);
                    var lalistIdEnviro = JsonConvert.DeserializeObject<List<EnviroIdModel>>(apiResponse);

                    foreach (var envir in lalistIdEnviro)
                    {
                        enviroId = envir.id;
                        break;
                    }
                }
            }
            EnviroVmUrl enviroVmUrl = await GetVmsSkytap(user, token, enviroId);
            return enviroVmUrl;
        }

        [HttpGet]
        public async Task<EnviroVmUrl> GetVmsSkytap(UserCreateDto user, string token, string enviro)
        {
            SkytapDataEnviroModel skytapDataEnviroModel;
            EnviroVmUrl enviroVmUrl = new EnviroVmUrl();

            //Console.WriteLine(user.Email + "  " +token);

            using (var httpClient = new HttpClient())
            {
                var authString2 = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{user.Login}:{token}"));
                httpClient.DefaultRequestHeaders.Clear();
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authString2);

                using (var response = await httpClient.GetAsync($"https://cloud.skytap.com/configurations/{enviro}.json"))
                {
                    //Console.WriteLine(response.StatusCode);
                    string apiResponse = await response.Content.ReadAsStringAsync();
                    skytapDataEnviroModel = JsonConvert.DeserializeObject<SkytapDataEnviroModel>(apiResponse);

                    //Console.WriteLine("enviro :" + apiResponse);

                    string vm_id = "";
                    string l_url = "";
                    foreach (EnviroIdModel vms in skytapDataEnviroModel.vms)
                    {
                        vm_id = vms.id;
                    }

                    foreach (SharingUrl url in skytapDataEnviroModel.publish_sets)
                    {
                        foreach (SharingUrlModel deskUrl in url.vms)
                        {
                            l_url = $"{deskUrl.desktop_url}?vm_id={deskUrl.id}";
                        }
                    }
                    enviroVmUrl.enviro = enviro;
                    enviroVmUrl.vm = vm_id;
                    enviroVmUrl.url = l_url;

                    //url_final = $"https://cloud.skytap.com/configurations/{enviro}/desktop?vm_id={vm_id}";
                    // Console.WriteLine(url_final);
                }
            }
            return enviroVmUrl;
        }

    }
}