using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace ManageEngineWebApp.Models
{
    public class WindowsUserDetails
    {
        public int Id { get; set; }
        public int CompanyId { get; set; }
        public int GroupId { get; set; }
        public int LocationId { get; set; }        
        public string UserName { get; set; }
      
        public string DomainName { get; set; }
        public string SID { get; set; }
        public string AccountType { get; set; }
        public string FullName { get; set; }
        public string Status { get; set; }
        public string UserCode { get; set; }
        public DateTime DateTime { get; set; }
    }
}
