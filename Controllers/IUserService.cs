using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using AddvalsApi.Data;
using AddvalsApi.Model;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;


namespace WebApi.Services
{
    public interface IUserService
    {
        UserModel Authenticate(string login, string password);    
    }

    public class UserService : IUserService
    {
        // users hardcoded for simplicity, store in a db with hashed passwords in production applications
        private readonly AppSettings _appSettings;
        private readonly IUserRepo _repository;

        public UserService(IUserRepo repository, IOptions<AppSettings> appSettings)
        {
            _appSettings = appSettings.Value;
            _repository = repository;
        }

        public UserModel Authenticate(string email, string password)
        {   
            UserModel user =_repository.GetUserByEmailAndPassword(email,password);

            // return null if user not found
            if (user == null)
                return null;

            // authentication successful so generate jwt token
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_appSettings.Secret);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimTypes.Name, user.Id.ToString())
                }),
                Expires = DateTime.UtcNow.AddHours(24),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            user.TokenApi = tokenHandler.WriteToken(token);

            return user;
        }

    }
}