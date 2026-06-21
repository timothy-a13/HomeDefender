using System.ComponentModel.DataAnnotations;

namespace BlazorApp1.Data
{
    public class Userinfo
    {
        [Key]
        public int uid { get; set; }
        public string id { get; set; }
        public string name { get; set; }
        public string email { get; set; }
        public string gender { get; set; }
        public int camera_num { get; set; }
    }
}
