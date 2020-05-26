using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AddvalsApi.Model
{
    public class AuthenticateModel
    {
        
        public string login { get; set; }

      
        public string password { get; set; }

    }
}