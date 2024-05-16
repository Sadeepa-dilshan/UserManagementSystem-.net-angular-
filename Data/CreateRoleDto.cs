using System.ComponentModel.DataAnnotations;

namespace Api.Data
{
    public class CreateRoleDto
    {
        [Required(ErrorMessage = "Role Name is required!")]
        public string RoleName { get; set; } = null;

    }
}
