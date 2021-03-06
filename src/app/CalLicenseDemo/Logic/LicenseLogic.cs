﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management;
using System.Security.Cryptography;
using System.Text;
using CalLicenseDemo.Common;
using CalLicense.Core.Model;
using LicenseKey;
using Newtonsoft.Json;

namespace CalLicenseDemo.Logic
{
    /// <summary>
    /// Generating license key logic
    /// </summary>
    internal class LicenseLogic
    {
        /// <summary>
        /// Constructor data initialization
        /// </summary>
        public LicenseLogic()
        {

        }

        /// <summary>
        /// Error message information property
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Activating license key to subscriber.
        /// </summary>
        public void ActivateSubscription()
        {

            //code for creating the licensekey and mapping with user and updating on the server.
            string liceseKey = GenerateLicense();
            LicenseJsonData licenseDetails;
            var licType = SingletonLicense.Instance.SelectedSubscription;
            licenseDetails = SingletonLicense.Instance.LicenseData ?? new LicenseJsonData();

            //adding the new license record to the list
            var detail = new LicenseDetails();
            detail.LicenseKey = liceseKey;
            detail.Type = licType;
            detail.ActivationDate = DateTime.Now;
            detail.ExpireDate = detail.ActivationDate.AddDays(licType.ActiveDuration);
            licenseDetails.LicenseList.Add(detail);
            SingletonLicense.Instance.SelectedSubscription = null;
            var datalicence = JsonConvert.SerializeObject(licenseDetails);
            common.SaveDatatoFile(datalicence, "LicenseData.txt");
        }

        /// <summary>
        /// Generating license key logic
        /// </summary>
        /// <returns>License key string</returns>
        public string GenerateLicense()
        {
            bool status;
            var userUniqueId = UniqueuserIdentifier();
            var licenseKey = string.Empty;
            if (SingletonLicense.Instance.SelectedSubscription.TypeName.ToLower() != "trial")
                while (string.IsNullOrEmpty(licenseKey))
                {
                    var key = GetlicenseKey();
                    if (!SingletonLicense.Instance.Context.License.ToList().Any(l => l.LicenseKey == key))
                        licenseKey = key;
                }
            else
                licenseKey = "trial";
            status = UpdateSubScriptionDetails(userUniqueId, licenseKey);
            if (status)
                return licenseKey;
            return string.Empty;
        }

        /// <summary>
        /// This method will gives the license key
        /// </summary>
        /// <returns>License key string</returns>
        public string GetlicenseKey()
        {
            var keygeneration = new GenerateKey();
            keygeneration.LicenseTemplate = "vvvvppppxxxxxxxxxxxx-xxxxxxxxxxxxxxxxxxxx-xxxxxxxxxxxxxxxxxxxx" +
                                            "-xxxxxxxxxxxxxxxxxxxx-xxxxxxxxxxxxxxxxxxxx";
            keygeneration.MaxTokens = 2;
            keygeneration.AddToken(0, "v", GenerateKey.TokenTypes.NUMBER, "1");
            keygeneration.AddToken(1, "p", GenerateKey.TokenTypes.NUMBER, "2");
            keygeneration.UseBase10 = false;
            keygeneration.UseBytes = false;
            keygeneration.CreateKey();
            return keygeneration.GetLicenseKey();
        }

        /// <summary>
        /// Updating user subscription details
        /// </summary>
        /// <param name="userUniqueId">userUniqueId</param>
        /// <param name="licenseKey">licenseKey</param>
        /// <returns></returns>
        public bool UpdateSubScriptionDetails(string userUniqueId, string licenseKey)
        {
            var lic = new License();
            lic.IsAvailable = false;
            lic.LicenseKey = licenseKey;
            lic.LicenseType = SingletonLicense.Instance.SelectedSubscription;
            var userLic = new UserLicense();
            userLic.UserKey = userUniqueId;
            userLic.ActivationDate = DateTime.Today;
            userLic.UserId = SingletonLicense.Instance.User.UserId;
            DateTime expireDate = DateTime.Today;
            userLic.ExpirationDate = expireDate.AddDays(SingletonLicense.Instance.SelectedSubscription.ActiveDuration);
            try
            {
                lic = SingletonLicense.Instance.Context.License.Add(lic);
                SingletonLicense.Instance.Context.SaveChanges();
                userLic.License = lic;
                SingletonLicense.Instance.Context.UserLicense.Add(userLic);
                SingletonLicense.Instance.Context.SaveChanges();

            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return false;
            }
            return true;
        }

        /// <summary>
        /// Identifying the new user.
        /// </summary>
        /// <returns>returns the user info</returns>
        public string UniqueuserIdentifier()
        {
            string processId = string.Empty, motherBoard = string.Empty, volumeSerial = string.Empty;
            var mos = new ManagementObjectSearcher("SELECT * FROM Win32_Processor");
            var proDetails = mos.Get();
            foreach (ManagementObject o in proDetails)
                processId = o.Properties["ProcessorId"].Value.ToString();

            mos = new ManagementObjectSearcher("SELECT * FROM Win32_BaseBoard");
            var moc = mos.Get();
            foreach (ManagementObject mo in moc)
                motherBoard = (string)mo["SerialNumber"];
            try
            {
                var _id = string.Concat("LicenceModule", processId, motherBoard);
                var _byteIds = Encoding.UTF8.GetBytes(_id);

                //Use MD5 to get the fixed length checksum of the ID string
                var _md5 = new MD5CryptoServiceProvider();
                var _checksum = _md5.ComputeHash(_byteIds);

                //Convert checksum into 4 ulong parts and use BASE36 to encode both
                var _part1Id = Base36.Encode(BitConverter.ToUInt32(_checksum, 0));
                var _part2Id = Base36.Encode(BitConverter.ToUInt32(_checksum, 4));
                var _part3Id = Base36.Encode(BitConverter.ToUInt32(_checksum, 8));
                var _part4Id = Base36.Encode(BitConverter.ToUInt32(_checksum, 12));

                //Concat these 4 part into one string
                return string.Format("{0}-{1}-{2}-{3}", _part1Id, _part2Id, _part3Id, _part4Id);
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
            return null;
        }

        /// <summary>
        /// To get user subscription details
        /// </summary>
        /// <returns>License type details</returns>

        public List<LicenseType> GetSubscriptionDetails()
        {
            return SingletonLicense.Instance.Context.LicenseType.ToList().FindAll(l => l.TypeName.ToLower() != "trial");
        }

        /// <summary>
        /// To get the trial license info
        /// </summary>
        /// <returns>license type info</returns>
        public LicenseType GetTrialLicense()
        {
            return SingletonLicense.Instance.Context.LicenseType.FirstOrDefault(l => l.TypeName.ToLower() == "trial");
        }
    }
}