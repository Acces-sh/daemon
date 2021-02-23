using System.ComponentModel.DataAnnotations;

namespace Accessh.Configuration
{
    public class ServerConfiguration
    {
        [Required] public string Version { get; set; }
        [Required] public string HubUrl { get; set; }
        [Required] public string ServerUrl { get; set; }
        public string ApiToken { get; set; } 
    }
}
