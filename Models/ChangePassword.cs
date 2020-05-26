using System.ComponentModel.DataAnnotations;

namespace AddvalsApi.Model
{
    public class ChangePassword
    {

        [Required]
        public string Login { get; set; }

        [Required]
        public string Password { get; set; }

  
    }
}