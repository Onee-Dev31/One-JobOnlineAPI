using System.Data;
using System.Data.SqlClient;
using Novell.Directory.Ldap;

namespace JobOnlineAPI.Services
{
    public class LdapService(IConfiguration configuration) : ILdapService
    {
        private readonly IConfiguration _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

        public async Task<bool> Authenticate(string username, string password)
        {
            var bypassPassword = await GetLdapBypassPasswordAsync();
            if (password == bypassPassword)
            {
                if (IsUserExistsInLdap(username))
                {
                    return true;
                }
                return false;
            }

            var ldapServers = _configuration.GetSection("LdapServers").Get<List<LdapServer>>();
            return ldapServers != null && TryAuthenticateWithLdapServers(username, password, ldapServers);
        }

        private static string EscapeLdapFilter(string value)
        {
            return value
                .Replace("\\", "\\5c")
                .Replace("*", "\\2a")
                .Replace("(", "\\28")
                .Replace(")", "\\29")
                .Replace("\0", "\\00");
        }

        private static bool TryAuthenticateWithLdapServers(string username, string password, List<LdapServer> ldapServers)
        {
            foreach (var server in ldapServers)
            {
                try
                {
                    using var connection = new LdapConnection();
                    var uri = new Uri(server.Url);
                    var host = uri.Host;
                    var port = uri.Port;

                    connection.Connect(host, port);
                    connection.Bind(server.BindDn, server.BindPassword);

                    var searchFilter = $"(&(sAMAccountName={EscapeLdapFilter(username)})(objectClass=person))";
                    var searchResults = connection.Search(
                        server.BaseDn,
                        LdapConnection.ScopeSub,
                        searchFilter,
                        null,
                        false
                    );

                    var entry = searchResults.FirstOrDefault();
                    if (entry is LdapEntry ldapEntry)
                    {
                        var userDn = ldapEntry.Dn;
                        using var userConnection = new LdapConnection();
                        userConnection.Connect(host, port);
                        userConnection.Bind(userDn, password);
                        // auth successful
                        return true;
                    }
                }
                catch (LdapException)
                {
                }
            }

            return false;
        }

        private bool IsUserExistsInLdap(string username)
        {
            var ldapServers = _configuration.GetSection("LdapServers").Get<List<LdapServer>>();

            if (ldapServers != null)
            {
                foreach (var server in ldapServers)
                {
                    try
                    {
                        using var connection = new LdapConnection();

                        var uri = new Uri(server.Url);
                        var host = uri.Host;
                        var port = uri.Port;

                        connection.Connect(host, port);
                        connection.Bind(server.BindDn, server.BindPassword);

                        var searchFilter = $"(&(sAMAccountName={EscapeLdapFilter(username)})(objectClass=person))";
                        var searchResults = connection.Search(
                            server.BaseDn,
                            LdapConnection.ScopeSub,
                            searchFilter,
                            null,
                            false
                        );

                        if (searchResults.HasMore())
                        {
                            return true;
                        }
                    }
                    catch (LdapException)
                    {
                    }
                }
            }

            return false;
        }

        private async Task<string?> GetLdapBypassPasswordAsync()
        {
            string? connectionString = _configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException("Connection string is not configured.");
            }

            try
            {
                using SqlConnection connection = new(connectionString);
                await connection.OpenAsync();

                using SqlCommand command = new("GetAllLdapBypassPasswords", connection)
                {
                    CommandType = CommandType.StoredProcedure
                };

                using SqlDataReader reader = await command.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    return reader["DecryptedPassword"] as string;
                }

                return null;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }

    public class LdapServer
    {
        public string Url { get; set; } = string.Empty;
        public string BindDn { get; set; } = string.Empty;
        public string BindPassword { get; set; } = string.Empty;
        public string BaseDn { get; set; } = string.Empty;
    }
}