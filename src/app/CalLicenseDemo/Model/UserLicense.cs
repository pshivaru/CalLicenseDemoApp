using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CalLicenseDemo.Model
{
    public class UserLicense
    {
        public UserLicense()
        {
            UserLicenseId = 0;
            UserKey = String.Empty;
            IsExpired = false;
            IsDeleted = false;
            ActivationDate = DateTime.MinValue;
            License = new License();
        }
        [Key]
        public int UserLicenseId { get; set; }
        public string UserKey { get; set; }
        public bool IsExpired { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime ActivationDate { get; set; }
        public int UserId { get; set; }
        [ForeignKey("UserId")]
        public virtual  User User { get; set; }
        public int LicenseId { get; set; }
        [ForeignKey("LicenseId")]
        public virtual License License { get; set; }
    }
}