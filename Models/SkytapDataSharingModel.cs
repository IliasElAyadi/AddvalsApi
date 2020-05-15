using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AddvalsApi.Model
{
    public class SkytapDataSharingModel
    {
        public string id { get; set; }
        
        //public string vms { get; set; }
        //public SharingIdModel[] vms { get; set; }

        public List<SharingUrlModel> vms { get; set; }

    }
}