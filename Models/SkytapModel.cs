using System;
using System.ComponentModel.DataAnnotations;

namespace AddvalsApi.Model
{
    public class SkytapModel
    {
        [Required]
        public string login_name { get; set; }

        [Required]
        public string email{ get; set; }

        [Required]
        public string account_role{ get; set; }
    }
}