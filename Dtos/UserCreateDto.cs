
using System.ComponentModel.DataAnnotations;

namespace AddvalsApi.Dtos
{
    public class UserCreateDto
    {   
        [Required]         
        public string Login { get; set; }

        [Required] 
        public string Password{ get; set; }

        //[Required]
        public string Email { get; set; }

        //[Required]
        public string FirstName { get; set; }
        
        //[Required]
        public string LastName { get; set; }


   
    }
}