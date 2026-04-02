using System.Data;
using System.Data.SqlClient;
using Novell.Directory.Ldap;

namespace JobOnlineAPI.Services
{
    public class LdapService(IConfiguration configuration, ILogger<LdapService> logger) : ILdapService
    {
        private readonly IConfiguration _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        private readonly ILogger<LdapService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        public async Task<bool> Authenticate(string username, string password)
        {
            var bypassPassword = await GetLdapBypassPasswordAsync();
            if (password == bypassPassword)
            {
                if (IsUserExistsInLdap(username))
                {
                    _logger.LogInformation("LDAP Authentication bypassed for user {Username}", username);
                    return true;
                }
                _logger.LogWarning("LDAP Bypass failed: User {Username} does not exist in LDAP", username);
                return false;
            }

            var ldapServers = _configuration.GetSection("LdapServers").Get<List<LdapServer>>();
            return ldapServers != null && TryAuthenticateWithLdapServers(username, password, ldapServers, _logger);
        }

        private static bool TryAuthenticateWithLdapServers(string username, string password, List<LdapServer> ldapServers, ILogger<LdapService> logger)
        {
            foreach (var server in ldapServers)
            {
                try
                {
                    using var connection = new LdapConnection();
                    var uri = new Uri(server.Url);
                    var host = uri.Host;
                    var port = uri.Port;

                    logger.LogInformation("Connecting to {Host}:{Port}", host, port);
                    connection.Connect(host, port);
                    connection.Bind(server.BindDn, server.BindPassword);
                    logger.LogInformation("LDAP Connection and Bind successful.");

                    var escapedUsername = EscapeLdapFilter(username);
                    var searchFilter = $"(&(sAMAccountName={escapedUsername})(objectClass=person))";
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
                        logger.LogInformation("LDAP Authentication successful for user {Username}", username);
                        return true;
                    }
                }
                catch (LdapException ex)
                {
                    logger.LogError("LDAP Error for server {Url}: {Message}", server.Url, ex.Message);
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

                        var escapedUsername = EscapeLdapFilter(username);
                        var searchFilter = $"(&(sAMAccountName={escapedUsername})(objectClass=person))";
                        var searchResults = connection.Search(
                            server.BaseDn,
                            LdapConnection.ScopeSub,
                            searchFilter,
                            null,
                            false
                        );

                        if (searchResults.HasMore())
                        {
                            _logger.LogInformation("User {Username} exists in LDAP.", username);
                            return true;
                        }
                    }
                    catch (LdapException ex)
                    {
                        _logger.LogError("LDAP Error for server {Url}: {Message}", server.Url, ex.Message);
                    }
                }
            }

            _logger.LogWarning("User {Username} does not exist in LDAP.", username);
            return false;
        }

        private static string EscapeLdapFilter(string input)
        {
            // Escape special LDAP filter characters per RFC 4515
            return input
                .Replace("\\", "\\5c")
                .Replace("\0", "\\00")
                .Replace("(", "\\28")
                .Replace(")", "\\29")
                .Replace("*", "\\2a")
                .Replace("/", "\\2f");
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
            catch (Exception ex)
            {
                _logger.LogError("Error fetching LDAP bypass password: {Message}", ex.Message);
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