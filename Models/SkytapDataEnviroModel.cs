using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AddvalsApi.Model
{
    public class SkytapDataEnviroModel
    {
        public string id { get; set; }

        //public string vms { get; set; }

        public EnviroIdModel[] vms { get; set; }


        public SharingUrl[] publish_sets { get; set; }
    }
}