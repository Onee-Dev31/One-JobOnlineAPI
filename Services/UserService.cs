using JobOnlineAPI.Models;
using JobOnlineAPI.Repositories;
using BCrypt.Net;

namespace JobOnlineAPI.Services
{
    public class UserService(IAdminRepository adminRepository, ILdapService ldapService) : IUserService
    {
        private readonly IAdminRepository _adminRepository = adminRepository;
        private readonly ILdapService _ldapService = ldapService;

        public async Task<AdminUser?> AuthenticateAsync(string username, string password, int JobID)
        {
            var user = await _adminRepository.GetUserByEmailAsync(username, JobID);

            if (user != null)
            {
                password = password.Trim();

                if (!string.IsNullOrEmpty(user.BypassPassword) && user.BypassUsed)
                {
                    string bypassHash = BCrypt.Net.BCrypt.HashPassword(user.BypassPassword);
                    bool isBypassPasswordMatched = BCrypt.Net.BCrypt.Verify(password, bypassHash);
                    if (isBypassPasswordMatched)
                    {
                        return new AdminUser
                        {
                            Username = user.Email,
                            Password = user.PasswordHash,
                            Role = "User",
                            UserId = user.UserId,
                            ConfirmConsent = user.ConfirmConsent,
                            ApplicantID = user.ApplicantID,
                            JobID = user.JobID,
                            Status = user.Status
                        };
                    }
                }

                bool isPasswordMatched;
                if (user.PasswordHash.StartsWith("$2"))
                {
                    isPasswordMatched = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
                }
                else
                {
                    isPasswordMatched = _adminRepository.VerifySHA256Hash(password, user.PasswordHash);
                }

                if (isPasswordMatched)
                {
                    return new AdminUser
                    {
                        Username = user.Email,
                        Password = user.PasswordHash,
                        Role = "User",
                        UserId = user.UserId,
                        ConfirmConsent = user.ConfirmConsent,
                        ApplicantID = user.ApplicantID,
                        JobID = user.JobID,
                        Status = user.Status
                    };
                }
            }

            var isLdapAuthenticated = await _ldapService.Authenticate(username, password);
            if (isLdapAuthenticated)
            {
                return new AdminUser
                {
                    AdminID = 0,
                    Username = username,
                    Password = "LDAPAuthenticated",
                    Role = "LDAP User"
                };
            }

            return null;
        }

        public async Task<string?> GetConfigValueAsync(string key)
        {
            return await _adminRepository.GetConfigValueAsync(key);
        }

        public async Task<string?> GetStyleValueAsync(string key)
        {
            return await _adminRepository.GetStyleValueAsync(key);
        }
    }
}