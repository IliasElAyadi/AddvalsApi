
using System.ComponentModel.DataAnnotations;

namespace AddvalsApi.Dtos
{
    public class UserCreateDto
    {

        public int Id { get; set; }
        [Required]
        public string Login { get; set; }

        [Required]
        public string Password { get; set; }

        //[Required]
        public string Email { get; set; }

        //[Required]
        public string FirstName { get; set; }

        //[Required]
        public string LastName { get; set; }

        public string Company { get; set; }

        public string Group { get; set; }

        public string TokenApi { get; set; }

        public string TokenSkytap { get; set; }

        public string idSkytap { get; set; }



    }
}