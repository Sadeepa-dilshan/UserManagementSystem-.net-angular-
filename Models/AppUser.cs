using Microsoft.AspNetCore.Identity;

namespace Api.Models
{
    public class AppUser:IdentityUser
    {
        public string FullName { get; set; }
    }
}
