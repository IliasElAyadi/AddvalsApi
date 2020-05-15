using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AddvalsApi.Model
{
    public class SharingUrl
    {
        public string url { get; set; }  

        public string id { get; set; }

        public SharingUrlModel[] vms { get; set; }        

    }
}