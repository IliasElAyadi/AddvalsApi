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
using Microsoft.EntityFrameworkCore;
using MailKit.Net.Smtp;
using MimeKit;
using Microsoft.AspNetCore.Authorization;
using WebApi.Services;

namespace AddvalsApi.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]

    public class UsersController : ControllerBase
    {
        private readonly IUserRepo _repository;
        private readonly IMapper _mapper;
        private IUserService _userService;

        public UsersController(IUserRepo repository, IMapper mapper, IUserService userService)
        {
            _repository = repository;
            _mapper = mapper;
            _userService = userService;
        }

        [AllowAnonymous]
        [HttpPost("/api/user/authenticate")]
        public async Task<UserModel> Authenticate([FromBody] AuthenticateModel model)
        {
            var user = _userService.Authenticate(model.Email, model.password);

            if (user == null)
            {
                //return BadRequest(new { message = "Username or password is incorrect" });
            }
            return user;
        }


        [AllowAnonymous]
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

        [HttpGet]
        public async Task<UserModel> GetUserDb(string email)
        {
            var user = _repository.GetUserByEmail(email);


            if (user == null)
            {

                return null;
            }

            return user;

        }

        [HttpGet]
        public async Task<SkytapModelToken> GetTokenDb(string email)
        {
            var user = _repository.GetUserByEmail(email);


            if (user == null)
            {

                return null;
            }

            SkytapModelToken SkytapModelToken = new SkytapModelToken();
            SkytapModelToken.api_token = user.TokenSkytap;
            return SkytapModelToken;
        }

        [AllowAnonymous]
        [HttpPost("/api/user/signin")]
        public async Task<ActionResult<string>> UserSignIn(UserCreateDto user)
        {
            Console.WriteLine(user.Email);

            AuthenticateModel authenticateModel = new AuthenticateModel
            {
                Email = user.Email,
                password = user.Password
            };

            UserModel userComplet = await Authenticate(authenticateModel);


            SkytapModelToken skytapModelToken = await GetTokenDb(user.Email);
            user.Email = user.Email + "addvals";
            user.Login = user.Email;
            Console.WriteLine(user.Email);
            Console.WriteLine(skytapModelToken.api_token);

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
            await shutdown_on_idle(user, enviroVmUrl.enviro, skytapModelToken.api_token);
            await UpdateStatsSkytap(user, enviroVmUrl.enviro, skytapModelToken.api_token);

            Console.WriteLine("-----------------------------------------");
            Console.WriteLine("USER : " + user.Email);
            Console.WriteLine("TOKEN : " + skytapModelToken.api_token);
            Console.WriteLine("ENVIRO : " + enviroVmUrl.enviro);
            Console.WriteLine("VM : " + enviroVmUrl.vm);


            string ok = await CreateSharingSkytap(user, skytapModelToken.api_token, enviroVmUrl.enviro);

            SharingUrl sharingUrl = await GetSharing(user, skytapModelToken.api_token, enviroVmUrl.enviro);


            EnviroVmUrl urlFinal = await GetEnviroSkytap(user, skytapModelToken.api_token);


            await UpdateAccesSharingSkytap(user, skytapModelToken.api_token, enviroVmUrl.enviro, enviroVmUrl.vm, sharingUrl.id);

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
            SkytapModelToken skytapModelToken = await GetTokenDb(user.Email);
            EnviroVmUrl enviroVmUrl = await GetEnviroSkytap(user, skytapModelToken.api_token);

            using (var httpClient = new HttpClient())
            {
                var authString2 = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{user.Email}:{token}"));
                httpClient.DefaultRequestHeaders.Clear();
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authString2);


                using (var response = await httpClient.GetAsync($"https://cloud.skytap.com/configurations/{enviroVmUrl.enviro}/desktop?vm_id={enviroVmUrl.vm}"))
                {
                    //Console.WriteLine(response.StatusCode);
                    apiResponse = await response.Content.ReadAsStringAsync();
                    //Console.WriteLine(apiResponse);
                    // apiResponse = "<h1>ceci est un text</h1>";
                    //var pageWeb = JsonConvert.DeserializeObject<>(apiResponse);

                }
            }
            return apiResponse;
        }

        [AllowAnonymous]
        [HttpPost("/api/user")]
        public async Task<ActionResult<UserReadDto>> CreateUser(UserCreateDto user)
        {
            UserModel userModel = new UserModel();
            userModel.Password = user.Password;
            userModel.FirstName = user.FirstName;
            userModel.LastName = user.LastName;

            Console.WriteLine("test4");
            var userMap = _mapper.Map<UserModel>(user);

            Console.WriteLine("test5");
            // Console.WriteLine("password " + user.Password);
            _repository.CreatUser(userMap);

            Console.WriteLine("test6");
            _repository.SaveChanges();

            Console.WriteLine("test7");

            var UserReadDto = _mapper.Map<UserReadDto>(userMap);

            //tokenApi
            AuthenticateModel authenticateModel = new AuthenticateModel
            {
                Email = user.Email,
                password = user.Password
            };

            UserModel userComplet = await Authenticate(authenticateModel);

            //tokenApi

            //////////SkyTap//////////////
            SkytapModelDeux sky = await addSkytap(user);
            Console.WriteLine("test1");
            await UpdatePwdSkytap(sky.id, userModel);

            Console.WriteLine("test2");
            SkytapModelToken skytapModelToken = await GetToken(user);

            Console.WriteLine("test3");
            userComplet.TokenSkytap = skytapModelToken.api_token;
            Console.WriteLine(" TOKEN SKYTAP1 " + skytapModelToken.api_token);
            userComplet.idSkytap = sky.id;

            Console.WriteLine("TOKEN :" + user.TokenSkytap);
            //////////SkyTap//////////////
            Console.WriteLine("  TOKEN SKYTAP2 " + userComplet.TokenSkytap);
            await UpdateUser(userComplet);
            //UserModel currentUser = await GetUserDb(user.Email);

            String UrlChangePassword = $"http://localhost:4200/profil/{user.Email}";
            ActionResult actionResult = SendEmail(UrlChangePassword, user);

            Console.WriteLine("Fin");

            return Ok(sky);

            //return CreatedAtRoute(nameof(GetUserById), new {Id = UserReadDto.Id}, UserReadDto); //Renvoi la route pour GetUserById
            //return Ok(UserReadDto);
        }
        [HttpPut("/api/user")]
        public async Task<ActionResult> UpdateUser(UserModel userUpdater)
        {
            UserModel userToUpdate = _repository.GetUserByEmail(userUpdater.Email);

            if (userToUpdate == null)
            {
                return NotFound();
            }

            UserModel newUser = userToUpdate;
            newUser.Password = userUpdater.Password;
            Console.WriteLine("New UpdateUser : " + newUser.Password);
            _mapper.Map(newUser, userToUpdate);

            _repository.UpdateUser(userToUpdate);

            _repository.SaveChanges();

            // await UpdatePwdSkytap(newUser.idSkytap, newUser.Password);

            return Ok();

            //return CreatedAtRoute(nameof(GetUserById), new { Id = userToUpdate.Id }, userToUpdate); //Renvoi la route pour GetUserById
        }

        [AllowAnonymous]
        [HttpPut("/api/user/profil")]
        public async Task<ActionResult> UpdatePasswordFinal(UserModel userChange)
        {
            Console.WriteLine("1");
            UserModel userToUpdate = _repository.GetUserByEmail(userChange.Email);
            Console.WriteLine("2");

            if (userToUpdate == null)
            {
                return NotFound();
            }
            Console.WriteLine("3");

            UserModel newUser = userToUpdate;
            newUser.Password = userChange.Password;
            newUser.FirstName = userChange.FirstName;
            newUser.LastName = userChange.LastName;
            Console.WriteLine("New " + newUser.Password);
            _mapper.Map(newUser, userToUpdate);
            Console.WriteLine("4");
            _repository.UpdateUser(userToUpdate);

            _repository.SaveChanges();

            await UpdatePwdSkytap(newUser.idSkytap, newUser);

            return Ok();

            //return CreatedAtRoute(nameof(GetUserById), new { Id = userToUpdate.Id }, userToUpdate); //Renvoi la route pour GetUserById
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

        [AllowAnonymous]
        public async Task<ActionResult<HttpResponseMessage>> DeleteUserSkytap(UserModel user)
        {

            var payload1 = new Dictionary<string, int>
            {
              {"transfer_user_id",499549},
            };

            HttpResponseMessage response;
            using (var httpClient = new HttpClient())
            {
                var authString2 = Convert.ToBase64String(Encoding.UTF8.GetBytes("elayadi.ilias@gmail.com:3e6d13cc03d0276a48619440f61f6269cf238ed0"));
                httpClient.DefaultRequestHeaders.Clear();
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authString2);

                var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Delete,
                    RequestUri = new Uri($"https://cloud.skytap.com/users/{user.idSkytap}.json"),
                    Content = new StringContent(JsonConvert.SerializeObject(payload1), Encoding.UTF8, "application/json")
                };
                response = await httpClient.SendAsync(request);
            }
            //Console.WriteLine(response);

            return Ok(response);
        }

        [AllowAnonymous]
        [HttpDelete("/api/user/{id}")]
        public async Task<ActionResult> DeletUserUser(int id)
        {
            var UserToDelete = _repository.GetUserById(id);

            await DeleteUserSkytap(UserToDelete);

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
                login_name = user.Email + "Addvals",
                email = "fakeemail@gmail.com",
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
        public async Task<SkytapModelDeux> UpdatePwdSkytap(string id, UserModel user)
        {
            using (var httpClient = new HttpClient())
            {
                var authString = Convert.ToBase64String(Encoding.UTF8.GetBytes("elayadi.ilias@gmail.com:3e6d13cc03d0276a48619440f61f6269cf238ed0"));
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authString);

                httpClient.DefaultRequestHeaders.Add("Accept", "application/json");

                //Console.WriteLine($"https://cloud.skytap.com/users/{SkyRep.id}?password={password}");
                using (var response = await httpClient.PutAsync($"https://cloud.skytap.com/users/{id}?password={user.Password}", null))
                {
                    //Console.WriteLine(response.StatusCode);
                    string apiResponse = await response.Content.ReadAsStringAsync();
                    //Console.WriteLine(apiResponse);           
                    // SkyRep = JsonConvert.DeserializeObject<SkytapModelDeux>(apiResponse);
                }
                using (var response = await httpClient.PutAsync($"https://cloud.skytap.com/users/{id}?first_name={user.Email}", null))
                {
                    //Console.WriteLine(response.StatusCode);
                    string apiResponse = await response.Content.ReadAsStringAsync();
                    //Console.WriteLine(apiResponse);           
                    // SkyRep = JsonConvert.DeserializeObject<SkytapModelDeux>(apiResponse);
                }
                using (var response = await httpClient.PutAsync($"https://cloud.skytap.com/users/{id}?password={user.Email}", null))
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
                var authString = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{user.Email}:{token}"));
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

        [AllowAnonymous]
        [HttpGet]
        public async Task<SkytapModelToken> GetToken(UserCreateDto user)
        {
            //  SkytapModelDeux SkyRep;
            SkytapModelToken skytapModelToken;

            using (var httpClient = new HttpClient())
            {
                var authString = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{user.Email}addvals:{user.Password}"));
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
                var authString2 = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{user.Email}:{token}"));
                httpClient.DefaultRequestHeaders.Clear();
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authString2);

                StringContent content1 = new StringContent(JsonConvert.SerializeObject(payload1), Encoding.UTF8, "application/json");

                using (var response = await httpClient.PostAsync("https://cloud.skytap.com/configurations.json", content1))
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
                name = $"Machine de {user.Email}",
            };

            using (var httpClient = new HttpClient())
            {
                var authString = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{user.Email}:{token}"));
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
                var authString = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{user.Email}:{token}"));
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
                        Console.WriteLine("VM SHARING : " + sharingUrl.id);
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
        public async Task<SkytapModelDeux> UpdateAccesSharingSkytap(UserCreateDto user, string token, string enviro, string vm, string sharingId)
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
                vms = new Dictionary<string, string>[] { vmss }
            };


            using (var httpClient = new HttpClient())
            {
                var authString = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{user.Email}:{token}"));
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
                var authString2 = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{user.Email}:{token}"));
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

            //Console.WriteLine(user.Email + " TOKEN " + token);

            using (var httpClient = new HttpClient())
            {
                var authString2 = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{user.Email}:{token}"));
                httpClient.DefaultRequestHeaders.Clear();
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authString2);

                using (var response = await httpClient.GetAsync("https://cloud.skytap.com/configurations.json"))
                {
                    //Console.WriteLine(response.StatusCode);
                    string apiResponse = await response.Content.ReadAsStringAsync();
                    //Console.WriteLine(apiResponse);
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




        [HttpPut]
        public async Task<SkytapModelDeux> shutdown_on_idle(UserCreateDto user, String enviro, String token)
        {
            string dat1 = new DateTime().ToString();
            var payload1 = new Dictionary<string, int>
            {
               {"shutdown_on_idle",2000 }, 
             //{"shutdown_at_time", "2020/05/29 13:25:00" },
              
            };

            using (var httpClient = new HttpClient())
            {
                var authString = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{user.Email}:{token}"));
                httpClient.DefaultRequestHeaders.Clear();
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authString);
                httpClient.DefaultRequestHeaders.Add("Accept", "application/json");

                StringContent content = new StringContent(JsonConvert.SerializeObject(payload1), Encoding.UTF8, "application/json");

                using (var response = await httpClient.PutAsync($"https://cloud.skytap.com/configurations/{enviro}.json", content))
                {
                    Console.WriteLine("atttenttionsdqsd = " + response.StatusCode);
                    string apiResponse = await response.Content.ReadAsStringAsync();
                    // skytapDataEnviroModel = JsonConvert.DeserializeObject<SkytapDataEnviroModel>(apiResponse);
                    //Console.WriteLine("enviro :" + apiResponse);
                }
            }
            return null;
        }

        [HttpGet]
        public async Task<EnviroVmUrl> GetVmsSkytap(UserCreateDto user, string token, string enviro)
        {
            SkytapDataEnviroModel skytapDataEnviroModel;
            EnviroVmUrl enviroVmUrl = new EnviroVmUrl();

            //Console.WriteLine(user.Email + "  " +token);

            using (var httpClient = new HttpClient())
            {
                var authString2 = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{user.Email}:{token}"));
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



        [HttpPost]
        public ActionResult SendEmail(string emailData, UserCreateDto user)
        {
            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress("AddvalsVM profil", "AddvalsVM"));
                message.To.Add(new MailboxAddress("client", user.Email));
                message.Subject = "Création de votre profil";
                message.Body = new TextPart("plain")
                {
                    Text = $@"Hey {user.Email}
                    
                    Votre nom d'utilisateur est {user.Email}

                    Pour crée votre mot de passe suivez ce lien : {emailData}

                    -- Addvals
                    "

                };

                using (var client = new MailKit.Net.Smtp.SmtpClient())
                {

                    client.Connect("smtp.gmail.com", 587, false);

                    //SMTP server authentication if needed
                    client.Authenticate("amphibibox2.0@gmail.com", "karim1302");

                    client.Send(message);

                    client.Disconnect(true);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return StatusCode(500, "Error occured");
            }

            return Ok();
        }
    }
}