using System.ComponentModel.DataAnnotations;

namespace Accessh.Configuration
{
    public class KeyConfiguration
    {
        [Required] public string AuthorizedKeyFilePath { get; set; }
        public string ApiToken { get; set; }
    }
}
