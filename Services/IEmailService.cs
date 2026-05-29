namespace ManageEngineWebApp.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string subject, string htmlBody);
        Task SendEmailForCompanyAsync(int? companyId, string toEmail, string subject, string htmlBody);
    }
}