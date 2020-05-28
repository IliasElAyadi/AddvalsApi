using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AddvalsApi.Model
{
    public class AuthenticateModel
    {
        
        public string Email { get; set; }

      
        public string password { get; set; }

    }
}