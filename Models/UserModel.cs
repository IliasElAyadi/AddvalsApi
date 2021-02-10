using System;
using System.ComponentModel.DataAnnotations;
using AddvalsApi.Dtos;

namespace AddvalsApi.Model
{
    public class UserModel
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Login { get; set; }

        [Required]
        public string Password { get; set; }

        [Required]
        public string Email { get; set; }

        //[Required]
        public string FirstName { get; set; }

        //[Required]
        public string LastName { get; set; }

        [Required]
        public string TokenApi { get; set; }

        [Required]
        public string Company { get; set; }

        [Required]
        public string Group { get; set; }

        [Required]
        public string TokenSkytap { get; set; }

        public string idSkytap { get; set; }


    }
}