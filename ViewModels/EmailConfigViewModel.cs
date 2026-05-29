using System.ComponentModel.DataAnnotations;

namespace ManageEngineWebApp.ViewModels
{
    public class EmailConfigViewModel
    {
        [Required]
        public int CompanyId { get; set; }

        [Required(ErrorMessage = "SMTP host is required.")]
        [Display(Name = "SMTP Host")]
        public string SmtpHost { get; set; } = string.Empty;

        [Required]
        [Range(1, 65535, ErrorMessage = "Enter a valid port number.")]
        [Display(Name = "SMTP Port")]
        public int SmtpPort { get; set; } = 587;

        [Display(Name = "Enable SSL/TLS")]
        public bool EnableSsl { get; set; } = true;

        [Required(ErrorMessage = "Sender email is required.")]
        [EmailAddress]
        [Display(Name = "Sender Email")]
        public string SenderEmail { get; set; } = string.Empty;

        [Display(Name = "Sender Display Name")]
        public string SenderName { get; set; } = string.Empty;

        [Required(ErrorMessage = "SMTP username is required.")]
        [Display(Name = "SMTP Username")]
        public string SmtpUsername { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "SMTP Password")]
        public string? SmtpPassword { get; set; }
    }
}
