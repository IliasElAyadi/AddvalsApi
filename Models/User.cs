using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AddvalsApi.Model
{
    public class User
    {
        public int Id { get; set; }
        public string Login { get; set; }

        [JsonIgnore]
        public string Password { get; set; }

        public string Email { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }
        [JsonIgnore]
        public string Token { get; set; }
        [JsonIgnore]
        public string idSkytape { get; set; }

        public string TokenApi { get; set; }



    }
}