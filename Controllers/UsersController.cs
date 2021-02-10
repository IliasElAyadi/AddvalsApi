using System.Collections.Generic;
using AddvalsApi.Data;
using AddvalsApi.Dtos;
using AddvalsApi.Model;
using AutoMapper;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
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
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;

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
        private readonly AppSettings _appSettings;


        public UsersController(IUserRepo repository, IMapper mapper, IUserService userService, IOptions<AppSettings> appSettings)
        {
            _repository = repository;
            _mapper = mapper;
            _userService = userService;
            _appSettings = appSettings.Value;

        }

        /* Authentication */
        [AllowAnonymous]
        [HttpPost("/api/user/authenticate")]
        public async Task<IActionResult> AuthenticateAsync([FromBody] AuthenticateModel model)
        {
            UserModel user = _userService.Authenticate(model.Email, model.password);
            if (user == null)
            {
                return BadRequest(new { message = "Username or password is incorrect" });
            }

            if (model.Email == "Adminaddvals1" && model.password == "Adminaddvals1")
            {
                // authentication successful so generate jwt token
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.ASCII.GetBytes(_appSettings.Secret);
                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Email, user.Email.ToString()),
                    new Claim(ClaimTypes.Name, user.FirstName.ToString() +" "+ user.LastName.ToString()),
                    new Claim(ClaimTypes.Role, "Admin"),
                    }),
                    Expires = DateTime.UtcNow.AddHours(24),
                    SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
                };
                var token = tokenHandler.CreateToken(tokenDescriptor);
                user.TokenApi = tokenHandler.WriteToken(token);
                //Console.WriteLine(user.TokenApi);
                UserModel user1 = await GetUserDb(user.Email);
                UserCreateDto userCreateDto = new UserCreateDto();
                userCreateDto.Password = user1.Password;
                userCreateDto.FirstName = user1.FirstName;
                userCreateDto.LastName = user1.LastName;
                userCreateDto.Company = user1.Company;
                userCreateDto.Group = user1.Group;
                userCreateDto.Email = user1.Email;
                userCreateDto.TokenApi = user1.TokenApi;
                userCreateDto.idSkytap = user1.idSkytap;
                
                await UpdateUser(userCreateDto);

                //await UserSignIn(userCreateDto);

                return Ok(new { Token = user.TokenApi });
            }
            else
            {
                // authentication successful so generate jwt token
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.ASCII.GetBytes(_appSettings.Secret);
                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Email, user.Email.ToString()),
                    new Claim(ClaimTypes.Name, user.FirstName.ToString() +" "+ user.LastName.ToString()),
                    new Claim(ClaimTypes.Role, "User"),
                    }),
                    Expires = DateTime.UtcNow.AddHours(24),
                    SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
                };
                var token = tokenHandler.CreateToken(tokenDescriptor);

                user.TokenApi = tokenHandler.WriteToken(token);
                //Console.WriteLine(user.TokenApi);
                UserModel user1 = await GetUserDb(user.Email);
                UserCreateDto userCreateDto = new UserCreateDto();
                userCreateDto.Password = user1.Password;
                userCreateDto.FirstName = user1.FirstName;
                userCreateDto.LastName = user1.LastName;
                userCreateDto.Company = user1.Company;
                userCreateDto.Group = user1.Group;
                userCreateDto.Email = user1.Email;
                userCreateDto.TokenApi = user1.TokenApi;
                userCreateDto.idSkytap = user1.idSkytap;

                await UpdateUser(userCreateDto);

                string urlFinal = await UserSignIn(userCreateDto);

                return Ok(new { Token = user.TokenApi, urlFinal });
                //return Ok($"{{\"url\":\"{urlFinal}\"}}");
            }

        }

        /* Récupère tous les utilisateurs */
        [Authorize(Roles = "Admin")]
        [HttpGet("/api/user")]
        public ActionResult<IEnumerable<UserReadDto>> GetAllUsers()
        {
            var users = _repository.GetAllUsers();
            return Ok(_mapper.Map<IEnumerable<UserReadDto>>(users));
        }

        /* Récupère USER par ID*/
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
        /* Récupère USER par email*/
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

        /* Récupère TokenSkytap Par Email*/

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

        /* Connexion de l'utilisateur*/
        [AllowAnonymous]
        [HttpPost("/api/user/signin")]
        public async Task<string> UserSignIn(UserCreateDto user)
        {
            SkytapModelToken skytapModelToken = await GetTokenDb(user.Email);
            user.Email = user.Email + "addvals";
            user.Login = user.Email;

            try
            {
                /* récupère l'enviro si il existe, le supprime et en crée un nouveau */
                EnviroVmUrl enviroEtVm1 = await GetEnviroSkytap(user, skytapModelToken.api_token);
                await DeleteEnviroSkytap(user, skytapModelToken.api_token, enviroEtVm1.enviro);
                SkytapDataEnviroModel skytapDataEnviroModel = await CreateEnviroSkytap(user, skytapModelToken.api_token);
                Console.WriteLine("GET/DELETE/CREATE");
            }
            catch (System.Exception)
            {
                /* création d'environnement */
                SkytapDataEnviroModel skytapDataEnviroModel = await CreateEnviroSkytap(user, skytapModelToken.api_token);
                Console.WriteLine("/CREATE");
            }

            /* récupère les informations de l'environnement */
            EnviroVmUrl enviroVmUrl = await GetEnviroSkytap(user, skytapModelToken.api_token);
            /* arret automatique (BETA) */
            await shutdown_on_idle(user, enviroVmUrl.enviro, skytapModelToken.api_token);
            /* Lancement de la machine */
            await UpdateStatsSkytap(user, enviroVmUrl.enviro, skytapModelToken.api_token);

            Console.WriteLine("-----------------------------------------");
            Console.WriteLine("USER : " + user.Email);
            Console.WriteLine("TOKEN : " + skytapModelToken.api_token);
            Console.WriteLine("ENVIRO : " + enviroVmUrl.enviro);
            Console.WriteLine("VM : " + enviroVmUrl.vm);

            /* Création de la machine partagé */
            string ok = await CreateSharingSkytap(user, skytapModelToken.api_token, enviroVmUrl.enviro);
            /* Récupère l'URL de la machine partagé */
            SharingUrl sharingUrl = await GetSharing(user, skytapModelToken.api_token, enviroVmUrl.enviro);
            /* récupère les informations de l'environnement */
            EnviroVmUrl urlFinal = await GetEnviroSkytap(user, skytapModelToken.api_token);
            /* Maj des access de la machine partagé*/
            await UpdateAccesSharingSkytap(user, skytapModelToken.api_token, enviroVmUrl.enviro, enviroVmUrl.vm, sharingUrl.id);

            Console.WriteLine("-----------------------------------------");
            Console.WriteLine("URL : " + urlFinal.url);
            Console.WriteLine("-----------------------------------------");

            return urlFinal.url;

            // return Ok();

        }


        /*         [HttpGet]
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
                            apiResponse = await response.Content.ReadAsStringAsync();
                        }
                    }
                    return apiResponse;
                } */

        /* Création de l'utilisateur */
        [AllowAnonymous]
        [HttpPost("/api/user")]
        public async Task<ActionResult<UserReadDto>> CreateUser(UserCreateDto user)
        {
            /* Création de l'utilisateur dans la DB*/

            UserModel userModel = new UserModel();
            userModel.Password = user.Password;
            userModel.Email = user.Email;
            userModel.FirstName = user.FirstName;
            userModel.LastName = user.LastName;
            userModel.Company = user.Company;
            userModel.Group = user.Group;


            var userMap = _mapper.Map<UserModel>(user);
            _repository.CreatUser(userMap);
            _repository.SaveChanges();

            var UserReadDto = _mapper.Map<UserReadDto>(userMap);


            //////////SkyTap//////////////
            /* Création de l'utilisateur sur Skytap*/

            SkytapModelDeux sky = await addSkytap(user);
            await UpdatePwdSkytap(sky.id, userModel);
            //await CreateToken(user);
            //SkytapModelToken skytapModelToken = await GetToken(user); ;
            //Console.WriteLine("token 1" + skytapModelToken.api_token);

            //Console.WriteLine("ID SKYTAP = " + sky.id);
            //user.TokenSkytap = skytapModelToken.api_token;

            //userModel.TokenSkytap = skytapModelToken.api_token;
            user.idSkytap = sky.id;
            userModel.idSkytap = sky.id;

            //Console.WriteLine("token 2" + userModel.TokenSkytap);
            //////////SkyTap//////////////  

            await UpdateUser(user);

            /* Envoi Email à l'uilisateur pour la création de son compte*/
            String UrlChangePassword = $"http://localhost:4200/profil/{user.Email}";
            ActionResult actionResult = SendEmail(UrlChangePassword, user);

            Console.WriteLine("Création de compte : OK");

            return Ok(/*sky*/);
        }

        /* MAJ du profil par l'utilisateur*/
        [HttpPut("/api/user")]
        public async Task<ActionResult> UpdateUser(UserCreateDto userUpdater)
        {
            UserModel userToUpdate = _repository.GetUserByEmail(userUpdater.Email);

            if (userToUpdate == null)
            {
                return NotFound();
            }

            UserModel newUser = userToUpdate;
            newUser.Password = userUpdater.Password;
           // newUser.idSkytap = userUpdater.idSkytap;

            _mapper.Map(newUser, userToUpdate);
            _repository.UpdateUser(userToUpdate);
            _repository.SaveChanges();

            return Ok();
        }

        /* MAJ du profil par l'utilisateur*/
        [AllowAnonymous]
        [HttpPut("/api/user/profil")]
        public async Task<ActionResult> UpdatePasswordFinal(UserModel userChange)
        {
            UserModel userToUpdate = _repository.GetUserByEmail(userChange.Email);

            if (userToUpdate == null)
            {
                return NotFound();
            }
            /* MAJ du profil sur la DB*/
            UserModel newUser = userToUpdate;
            newUser.Password = userChange.Password;
            _mapper.Map(newUser, userToUpdate);
            _repository.UpdateUser(userToUpdate);

            _repository.SaveChanges();

            /* MAJ du mot de passe sur Skytap*/
            await UpdatePwdSkytap(newUser.idSkytap, newUser);

            return Ok();
        }
        /* MAJ du profil sur la DB*/
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

        /* Patch User */
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

        /* Suppression d'un User avec son ID Skytap*/
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
                var authString2 = Convert.ToBase64String(Encoding.UTF8.GetBytes("elayadi.ilias@gmail.com:karim1302"));
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
            return Ok(response);
        }

        /* Suppression d'un User dans la DB & Skytap*/
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

        /* Création d'utilisateur sur Skytap*/
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
                var authString = Convert.ToBase64String(Encoding.UTF8.GetBytes("elayadi.ilias@gmail.com:karim1302"));
                httpClient.DefaultRequestHeaders.Clear();
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authString);
                httpClient.DefaultRequestHeaders.Add("Accept", "application/json");

                StringContent content = new StringContent(JsonConvert.SerializeObject(skytapModel), Encoding.UTF8, "application/json");

                using (var response = await httpClient.PostAsync("https://cloud.skytap.com/users.json", content))
                {
                    string apiResponse = await response.Content.ReadAsStringAsync();
                    SkyRep = JsonConvert.DeserializeObject<SkytapModelDeux>(apiResponse);
                    //Console.WriteLine("REGARDE ICI" + apiResponse);
                }
            }
            return SkyRep;
        }


        /* MAJ de l'utilisateur sur Skytap*/

        [HttpPut]
        public async Task<SkytapModelDeux> UpdatePwdSkytap(string id, UserModel user)
        {

            Console.WriteLine(" CurrentUser = " + user.Email + "addvals" + " / " + user.Password);
            using (var httpClient = new HttpClient())
            {
                var authString = Convert.ToBase64String(Encoding.UTF8.GetBytes("elayadi.ilias@gmail.com:karim1302"));
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authString);

                httpClient.DefaultRequestHeaders.Add("Accept", "application/json");

                using (var response = await httpClient.PutAsync($"https://cloud.skytap.com/users/{id}?password={user.Password}", null))
                {

                    string apiResponse = await response.Content.ReadAsStringAsync();

                }
                using (var response = await httpClient.PutAsync($"https://cloud.skytap.com/users/{id}?first_name={user.Email}", null))
                {
                    string apiResponse = await response.Content.ReadAsStringAsync();

                }
            }
            return null;
        }
        /* MAJ du statut de la machine en mode running*/

        [HttpPut]
        public async Task<SkytapModelDeux> UpdateStatsSkytap(UserCreateDto user, String enviro, String token)
        {
            SkytapRunModel skytapRunModel = new SkytapRunModel
            {
                runstate = "running",
            };
            using (var httpClient = new HttpClient())
            {
                var authString = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{user.Email}:{user.Password}"));
                httpClient.DefaultRequestHeaders.Clear();
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authString);
                httpClient.DefaultRequestHeaders.Add("Accept", "application/json");

                StringContent content = new StringContent(JsonConvert.SerializeObject(skytapRunModel), Encoding.UTF8, "application/json");

                using (var response = await httpClient.PutAsync($"https://cloud.skytap.com/configurations/{enviro}.json", content))
                {
                    string apiResponse = await response.Content.ReadAsStringAsync();
                }
            }
            return null;
        }

        /* Récupération du Token de l'utilisateur sur Skytap*/
        [AllowAnonymous]
        [HttpGet]
        public async Task<SkytapModelToken> GetToken(UserCreateDto user)
        {
            SkytapModelToken skytapModelToken;

            using (var httpClient = new HttpClient())
            {
                var authString = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{user.Email}addvals:{user.Password}"));
                Console.WriteLine("lors de la recup du token MAIL + MDP " + user.Email + "addvals : " + user.Password);

                httpClient.DefaultRequestHeaders.Clear();
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authString);
                httpClient.DefaultRequestHeaders.Add("Accept", "application/json");


                using (var response = await httpClient.GetAsync("https://cloud.skytap.com/account/api_token"))
                {
                    string apiResponse = await response.Content.ReadAsStringAsync();
                    skytapModelToken = JsonConvert.DeserializeObject<SkytapModelToken>(apiResponse);

                    Console.WriteLine("Apres creation la recup" + apiResponse);
                }
            }
            return skytapModelToken;
        }

        /* Création du token sur Skytap*/
        [HttpPost]
        public async Task<string> CreateToken(UserCreateDto user)
        {
            var payload1 = new Dictionary<string, string>
                    {
                    {$"{user.Email}addvals", user.Password},
                    };

            using (var httpClient = new HttpClient())
            {
                var authString = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{user.Email}addvals:{user.Password}"));
                //Console.WriteLine("lors de la creation du token : MAIL + MDP " + $"{user.Email}addvals:{user.Password}");
                httpClient.DefaultRequestHeaders.Clear();
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authString);

                StringContent content1 = new StringContent(JsonConvert.SerializeObject(payload1), Encoding.UTF8, "application/json");

                using (var response = await httpClient.PostAsync("https://cloud.skytap.com/v2/account/api_tokens.json", null))
                {
                    string apiResponse = await response.Content.ReadAsStringAsync();
                    Console.WriteLine("creation du token" + apiResponse);
                }
            }
            return "ok";
        }

        /* Création de l'environnement sur Skytap*/
        [HttpPost]
        public async Task<SkytapDataEnviroModel> CreateEnviroSkytap(UserCreateDto user, string token)
        {
            SkytapDataEnviroModel skytapDataEnviroModel;

            /* Création qui se base sur le le TEMPLATE qui possede un certain ID  1911559 */

            var payload1 = new Dictionary<string, int>{};

            if (user.Group == "1")
            {
                payload1.Add("template_id", 1911559);
                Console.WriteLine("template_id g1, 1911559");
              
            }
            else if (user.Group == "2" || user.Group == "3" || user.Group == "4")
            {
                // var payload1 = new Dictionary<string, int>
                // {
                //    {"template_id", 1899007},
                // };

                 payload1.Add("template_id", 1899007);
                 Console.WriteLine("template_id g:2-3-4, 1899007");
            }


            using (var httpClient = new HttpClient())
            {
                var authString2 = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{user.Email}:{user.Password}"));
                httpClient.DefaultRequestHeaders.Clear();
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authString2);

                StringContent content1 = new StringContent(JsonConvert.SerializeObject(payload1), Encoding.UTF8, "application/json");

                using (var response = await httpClient.PostAsync("https://cloud.skytap.com/configurations.json", content1))
                {
                    string apiResponse = await response.Content.ReadAsStringAsync();
                    skytapDataEnviroModel = JsonConvert.DeserializeObject<SkytapDataEnviroModel>(apiResponse);
                }
            }
            return skytapDataEnviroModel;
        }

        /* Création de la machine partagé */
        [HttpPost]
        public async Task<string> CreateSharingSkytap(UserCreateDto user, string token, string enviro)
        {
            CreateSharing createSharing = new CreateSharing
            {
                name = $"Machine de {user.Email}",
            };

            using (var httpClient = new HttpClient())
            {
                var authString = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{user.Email}:{user.Password}"));
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

        /* Récupère l'URL de la machine partagé */
        [HttpGet]
        public async Task<SharingUrl> GetSharing(UserCreateDto user, string token, string enviro)
        {
            //SkytapDataSharingModel skytapDataSharingModel;
            SharingUrl sharingUrl = new SharingUrl();

            using (var httpClient = new HttpClient())
            {
                var authString = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{user.Email}:{user.Password}"));
                httpClient.DefaultRequestHeaders.Clear();
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authString);
                httpClient.DefaultRequestHeaders.Add("Accept", "application/json");

                using (var response = await httpClient.GetAsync($"https://cloud.skytap.com/configurations/{enviro}/publish_sets.json"))
                {
                    string apiResponse = await response.Content.ReadAsStringAsync();
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
                var authString = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{user.Email}:{user.Password}"));
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

        /*Suppresion de l'environnement*/
        [HttpDelete]
        public async Task<OkResult> DeleteEnviroSkytap(UserCreateDto user, string token, String enviro)
        {
            using (var httpClient = new HttpClient())
            {
                var authString2 = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{user.Email}:{user.Password}"));
                httpClient.DefaultRequestHeaders.Clear();
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authString2);

                using (var response = await httpClient.DeleteAsync($"https://cloud.skytap.com/configurations/{enviro}"))
                {
                    string apiResponse = await response.Content.ReadAsStringAsync();
                }
            }
            return Ok();
        }


        /* récupère les informations de l'environment  */
        [HttpGet]
        public async Task<EnviroVmUrl> GetEnviroSkytap(UserCreateDto user, string token)
        {
            //SkytapDataEnviroModel skytapDataEnviroModel;          
            EnviroIdModel enviro = new EnviroIdModel();
            String enviroId = "";

            using (var httpClient = new HttpClient())
            {
                var authString2 = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{user.Email}:{user.Password}"));
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



        /* arret automatique (BETA) */
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
                var authString = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{user.Email}:{user.Password}"));
                httpClient.DefaultRequestHeaders.Clear();
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authString);
                httpClient.DefaultRequestHeaders.Add("Accept", "application/json");

                StringContent content = new StringContent(JsonConvert.SerializeObject(payload1), Encoding.UTF8, "application/json");

                using (var response = await httpClient.PutAsync($"https://cloud.skytap.com/configurations/{enviro}.json", content))
                {
                    string apiResponse = await response.Content.ReadAsStringAsync();
                }
            }
            return null;
        }

        /* récupère les informations de la machine virtuelle  */
        [HttpGet]
        public async Task<EnviroVmUrl> GetVmsSkytap(UserCreateDto user, string token, string enviro)
        {
            SkytapDataEnviroModel skytapDataEnviroModel;
            EnviroVmUrl enviroVmUrl = new EnviroVmUrl();

            using (var httpClient = new HttpClient())
            {
                var authString2 = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{user.Email}:{user.Password}"));
                httpClient.DefaultRequestHeaders.Clear();
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authString2);

                using (var response = await httpClient.GetAsync($"https://cloud.skytap.com/configurations/{enviro}.json"))
                {
                    string apiResponse = await response.Content.ReadAsStringAsync();
                    skytapDataEnviroModel = JsonConvert.DeserializeObject<SkytapDataEnviroModel>(apiResponse);

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

                }
            }
            return enviroVmUrl;
        }


        /* Envoi Email à l'uilisateur pour la création de son compte*/
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
                    Text = $@"Cher Monsieur/Madame {user.LastName}
                    
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