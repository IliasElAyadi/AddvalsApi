using System;
using System.ComponentModel.DataAnnotations;

namespace AddvalsApi.Model
{
    public class SkytapModelToken
    {
        [Required]
        public string api_token { get; set; }

    }
}