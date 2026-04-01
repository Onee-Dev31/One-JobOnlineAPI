#!/bin/bash
# Run once to configure user secrets for local development.
# Usage: bash setup-secrets.sh

set -e
cd "$(dirname "$0")"

echo "Setting up User Secrets for JobOnlineAPI..."

# Database
dotnet user-secrets set "ConnectionStrings:DefaultConnection"     "Server=10.31.1.90;Database=JobOnlineDB;User Id=sa;Password=Mv5148cBX@;Trusted_Connection=False;MultipleActiveResultSets=true;TrustServerCertificate=True;"
dotnet user-secrets set "ConnectionStrings:DefaultConnectionHRMS" "Server=10.31.1.87;Database=HRMS;User Id=sa;Password=Mv5148cBX@;Trusted_Connection=False;MultipleActiveResultSets=true;TrustServerCertificate=True;"

# JWT
dotnet user-secrets set "JwtSettings:AccessSecret"  "This is onee secret key for authentication"
dotnet user-secrets set "JwtSettings:RefreshSecret" "noitacitnehtua rof yek terces eeno si sihT"
dotnet user-secrets set "Jwt:Key"                   "ThisIsASecretKeyWith256BitLength123!"

# Email
dotnet user-secrets set "EmailSettings:SmtpUser" "appnoti@onehd.net"
dotnet user-secrets set "EmailSettings:SmtpPass" "P@ssw0rd"

# Network share
dotnet user-secrets set "FileStorage:NetworkUsername" "Administrator"
dotnet user-secrets set "FileStorage:NetworkPassword" "Qu3r3manageadm!n"

# LDAP bind passwords
dotnet user-secrets set "LdapServers:0:BindPassword" "ONEEP@ssw0rd"
dotnet user-secrets set "LdapServers:1:BindPassword" "ONEEP@ssw0rd"
dotnet user-secrets set "LdapServers:2:BindPassword" "ONEEP@ssw0rd"
dotnet user-secrets set "LdapServers:3:BindPassword" "ONEEP@ssw0rd"
dotnet user-secrets set "LdapServers:4:BindPassword" "P@ssw0rd"

echo "Done. User secrets configured successfully."
