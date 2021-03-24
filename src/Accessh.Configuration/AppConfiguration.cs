using System.ComponentModel.DataAnnotations;
using Accessh.Configuration.Enums;

namespace Accessh.Configuration
{
    public class AppConfiguration
    {
        [Required] public string Version { get; set; }
        [Required] public string HubUrl { get; set; }
        [Required] public string ServerUrl { get; set; }
        public string ConfigurationFilePath { get; set; }
        [Required] public Mode Mode { get; set; }
    }
}
