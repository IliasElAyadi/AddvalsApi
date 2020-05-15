using System;
using System.ComponentModel.DataAnnotations;

namespace AddvalsApi.Model
{
    public class SkytapRunModel
    {
        [Required]
        public string runstate { get; set; }

    }
}