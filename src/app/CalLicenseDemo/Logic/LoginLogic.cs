﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using CalLicenseDemo.Common;
using CalLicense.Core.Model;
using CalLicense.Core.DatabaseContext;
using Newtonsoft.Json;

namespace CalLicenseDemo.Logic
{
    /// <summary>
    /// Validation methods for verifying the login details.
    /// </summary>
    public class LoginLogic : IDisposable
    {
        private readonly string folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
               "CalibrationLicense");

        private readonly string tempFolderPath = Path.Combine(Path.GetTempPath(),
                "CalibrationLicense");
        /// <summary>
        /// It holds the error message
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// LoginLogic constructor initialization
        /// </summary>
        public LoginLogic()
        {
            SingletonLicense.Instance.Context = new LicenseAppDBContext();
        }

        /// <summary>
        /// clearing the object from application runtime memory.
        /// </summary>
        public void Dispose()
        {
            SingletonLicense.Instance.Context.Dispose();
        }
        /// <summary>
        /// Validate Email address.
        /// </summary>
        /// <param name="email">email data</param>
        /// <returns> true:if email does not exist
        /// false:if email Exist</returns>
        public bool ValidateEmail(string email)
        {
            if (SingletonLicense.Instance.Context.User.ToList().Count == 0)
                return true;
            return !(SingletonLicense.Instance.Context.User.Any(u => u.Email.ToLower() == email.ToLower()));
        }

        /// <summary>
        /// Logic for authenticate the user login information.
        /// </summary>
        /// <param name="userName">userName details</param>
        /// <param name="password">password details</param>
        /// <returns></returns>
        public bool AuthenticateUser(string userName, string password)
        {
            var encryptedPassword = password;
            var status = SingletonLicense.Instance.Context.User.ToList().Any(u => u.Email.ToLower() == userName.ToLower());
            User user = null;
            if (status)
            {
                user =
                    SingletonLicense.Instance.Context.User.Where(u => u.Email.ToLower() == userName.ToLower()).ToList()[0];

                if (user != null)
                {
                    string hashPasword = CreatePasswordhash(password, user.ThumbPrint);
                    if (hashPasword == user.PasswordHash)
                    {
                        SingletonLicense.Instance.User = user;
                        var jsonData = JsonConvert.SerializeObject(user);
                        if (!common.IsFileExist("credential.txt"))
                            common.SaveDatatoFile(jsonData, "credential.txt");
                        return true;
                    }
                    ErrorMessage = "Invalid Password";
                }
                else
                    ErrorMessage = "Specified Credentials are invalid";
            }
            else
                ErrorMessage = "Specified Email is not registered";
            return false;
        }

        /// <summary>
        ///     Verifying the user license is still active and listing feature based on the
        /// </summary>

        //public void GetFeatureList()
        //{
        //    String jsonData = common.GetJsonDataFromFile("LicenseData.txt");
        //    if (String.IsNullOrEmpty(jsonData)) return;
        //    var licenseDetails = JsonConvert.DeserializeObject<UserLicenseJsonData>(jsonData);
        //    var validLienseList = licenseDetails;
        //    foreach (var ld in licenseDetails.LicenseList)
        //        if (ld.ExpireDate.Date > DateTime.Today.Date)
        //            foreach (var feature in ld.Type.FeatureList)
        //            {
        //                if (!SingletonLicense.Instance.FeatureList.Contains(feature))
        //                    SingletonLicense.Instance.FeatureList.Add(feature);
        //            }
        //        else
        //            //Code is used to remove the Subscription which is expired
        //            validLienseList.LicenseList.Remove(ld);

        //    SingletonLicense.Instance.LicenseData = validLienseList;
        //}

        /// <summary>
        ///    To get the feature list information for login user. 
        /// </summary>
        public void GetFeatureList()
        {
            List<UserLicense> userLicense = SingletonLicense.Instance.Context.UserLicense.ToList().FindAll(l => l.UserId == SingletonLicense.Instance.User.UserId);
            LicenseJsonData data = new LicenseJsonData();
            foreach (var ld in userLicense)
            {
                if (ld.ExpirationDate.Date > DateTime.Today.Date)
                {
                    foreach (var feature in ld.License.LicenseType.FeatureList)
                    {
                        if (!SingletonLicense.Instance.FeatureList.Contains(feature))
                            SingletonLicense.Instance.FeatureList.Add(feature);
                    }
                    LicenseDetails details = new LicenseDetails();
                    details.ActivationDate = ld.ActivationDate;
                    details.ExpireDate = ld.ExpirationDate;
                    data.LicenseList.Add(details);
                }
            }

            SingletonLicense.Instance.LicenseData = data;
        }


        /// <summary>
        /// This method is used to generate the user profile in Database system
        /// </summary>
        /// <param name="registrationModel">registrationModel</param>
        /// <returns> true:User profile created
        /// false:user already exist</returns>
        public bool CreateUserInfo(RegistrationModel registrationModel)
        {
            try
            {
                if (SingletonLicense.Instance.Context.User.ToList().Any(u => u.Email == registrationModel.Email))
                {
                    ErrorMessage = "Email id is lready registered";
                    return false;
                }
                User user = new User();
                user.FName = registrationModel.FName;
                user.LName = registrationModel.LName;
                user.Email = registrationModel.Email;

                Team _team =
                    SingletonLicense.Instance.Context.Team.ToList()
                        .FirstOrDefault(
                            t => t.Name.ToLower().Trim() == registrationModel.OrganizationName.ToLower().Trim());
                if (_team != null)
                    user.Organization = _team;
                else
                {
                    _team = new Team() { Name = registrationModel.OrganizationName };
                    _team = SingletonLicense.Instance.Context.Team.Add(_team);
                    SingletonLicense.Instance.Context.SaveChanges();
                    user.Organization = _team;
                }
                string thumbPrint = string.Empty;
                user.PasswordHash = HashPassword(registrationModel.Password, out thumbPrint);
                user.ThumbPrint = thumbPrint;
                user.TeamId = _team.TeamId;
                user = SingletonLicense.Instance.Context.User.Add(user);
                SingletonLicense.Instance.Context.SaveChanges();
                SingletonLicense.Instance.User = user;
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Password encription method
        /// </summary>
        /// <param name="password">user entered password</param>
        /// <param name="thumbprint">password attaching with thumbprint</param>
        /// <returns></returns>
        public string HashPassword(string password, out string thumbprint)
        {
            int size = 10;
            thumbprint = CreateSalt(size);
            return CreatePasswordhash(password, thumbprint);
        }

        /// <summary>
        /// serialization of password
        /// </summary>
        /// <param name="size">password split size</param>
        /// <returns></returns>
        public string CreateSalt(int size)
        {
            byte[] bytedata = new byte[size];
            var rngProvider = new System.Security.Cryptography.RNGCryptoServiceProvider();
            rngProvider.GetBytes(bytedata);
            return Convert.ToBase64String(bytedata);
        }
        /// <summary>
        /// Password encription 
        /// </summary>
        /// <param name="password">password</param>
        /// <param name="thumbPrint">thumbPrint</param>
        /// <returns></returns>
        public string CreatePasswordhash(string password, string thumbPrint)
        {
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(password + thumbPrint);
            System.Security.Cryptography.SHA256Managed sha256 = new SHA256Managed();
            var byteHashData = sha256.ComputeHash(bytes);
            return ByteArrayToHexString(byteHashData);
        }
        /// <summary>
        /// Password encription 
        /// </summary>
        /// <param name="bytes">bytes</param>
        /// <returns>heaxdecimal string fromate</returns>
        public String ByteArrayToHexString(byte[] bytes)
        {
            int length = bytes.Length;

            char[] chars = new char[length << 1];

            for (int i = 0, j = 0; i < length; i++, j++)
            {
                byte b = (byte)(bytes[i] >> 4);
                chars[j] = (char)(b > 9 ? b + 0x37 : b + 0x30);

                j++;

                b = (byte)(bytes[i] & 0x0F);
                chars[j] = (char)(b > 9 ? b + 0x37 : b + 0x30);
            }

            return new String(chars);
        }

        /// <summary>
        /// Offline user authenication
        /// </summary>
        /// <param name="email">email details</param>
        /// <param name="password">password details</param>
        /// <returns></returns>
        public bool AuthenticateUserOffline(string email, string password)
        {
            if (!common.IsFileExist("credential.txt"))
            {
                ErrorMessage = "Network is not available for authentication";
                return false;
            }

            string jsonData = common.GetJsonDataFromFile("credential.txt");
            User user = JsonConvert.DeserializeObject<User>(jsonData);
            string hashPasword = string.Empty;
            if (user.Email.ToLower() == email)
            {
                hashPasword = CreatePasswordhash(password, user.ThumbPrint);
                if (hashPasword == user.PasswordHash)
                    SingletonLicense.Instance.User = user;
                return true;
            }
            else
                ErrorMessage = "Invalid Email";
            return false;
        }


    }
}