using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System.ComponentModel.Design;
using System.Net;
using System.Net.Mail;
using System.Text;

namespace ManageEngineWebApp.Services
{
    public class EmailService : IEmailService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _config;
        private readonly string _apiBase;

        public EmailService(IHttpClientFactory httpClientFactory, IConfiguration config)
        {
            _httpClientFactory = httpClientFactory;
            _config = config;
            _apiBase = _config["ApiSettings:BaseUrl"];
        }
        public async Task SendEmailAsync(string toEmail, string subject, string htmlBody)
           => await SendViaApiAsync(companyId: null, toEmail, subject, htmlBody);

        public async Task SendEmailForCompanyAsync(int? companyId, string toEmail, string subject, string htmlBody)
            => await SendViaApiAsync(companyId, toEmail, subject, htmlBody);
        
        public async Task SendViaApiAsync(int? companyId, string toEmail, string subject, string htmlBody)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("ManageEngineApi");

                var endpoint = companyId.HasValue
                    ? $"{_apiBase}/api/Email/Send/{companyId}"
                    : $"{_apiBase}/api/Email/Send";

                var payload = JsonConvert.SerializeObject(new
                {
                    ToEmail = toEmail,
                    Subject = subject,
                    HtmlBody = htmlBody
                });

                var content = new StringContent(payload, Encoding.UTF8, "application/json");
                var response = await client.PostAsync(endpoint, content);

                if (!response.IsSuccessStatusCode)
                {
                    var err = await response.Content.ReadAsStringAsync();
                    throw new InvalidOperationException($"Email API returned {response.StatusCode}: {err}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[EmailService] Send failed: {ex.Message}");
                throw;
            }
        }
    
    }
}
