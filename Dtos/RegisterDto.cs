using System.ComponentModel.DataAnnotations;

namespace ManageEngineWebApp.Dtos
{
    public class RegisterDto
    {

        [Required]
        public string Username { get; set; }

        [Required, EmailAddress]
        public string Email { get; set; }

        [Required, DataType(DataType.Password)]
        public string Password { get; set; }

        [Required, DataType(DataType.Password), Compare("Password")]
        public string ConfirmPassword { get; set; }
        public string Role {  get; set; }
        public int? CompanyId { get; set; }
    }
}
