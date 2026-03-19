@echo off
:: Setup dotnet user-secrets for JobOnlineAPI (Windows)
:: Fill in the values below before running.

echo Setting up User Secrets for JobOnlineAPI...
cd /d %~dp0

:: --- Database ---
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=10.31.1.90;Database=JobOnlineDB;User Id=<DB_USER>;Password=<DB_PASS>;Trusted_Connection=False;MultipleActiveResultSets=true;TrustServerCertificate=True;"
dotnet user-secrets set "ConnectionStrings:DefaultConnectionHRMS" "Server=10.31.1.90;Database=HRMS;User Id=<DB_USER>;Password=<DB_PASS>;Trusted_Connection=False;MultipleActiveResultSets=true;TrustServerCertificate=True;"

:: --- JWT ---
dotnet user-secrets set "JwtSettings:AccessSecret"  "<ACCESS_SECRET>"
dotnet user-secrets set "JwtSettings:RefreshSecret" "<REFRESH_SECRET>"
dotnet user-secrets set "Jwt:Key"                   "<JWT_KEY>"

:: --- File Storage ---
dotnet user-secrets set "FileStorage:ProductionPath" "<PRODUCTION_PATH>"
dotnet user-secrets set "FileStorage:NetworkPath"    "<NETWORK_PATH>"
dotnet user-secrets set "FileStorage:Username"       "<SHARE_USERNAME>"
dotnet user-secrets set "FileStorage:Password"       "<SHARE_PASSWORD>"

:: --- Email ---
dotnet user-secrets set "EmailSettings:SmtpUser" "<SMTP_USER>"
dotnet user-secrets set "EmailSettings:SmtpPass" "<SMTP_PASS>"

:: --- LDAP ---
dotnet user-secrets set "LdapServers:0:BindPassword" "<LDAP_PASS>"
dotnet user-secrets set "LdapServers:1:BindPassword" "<LDAP_PASS>"
dotnet user-secrets set "LdapServers:2:BindPassword" "<LDAP_PASS>"
dotnet user-secrets set "LdapServers:3:BindPassword" "<LDAP_PASS>"
dotnet user-secrets set "LdapServers:4:BindPassword" "<LDAP_PASS>"

echo Done! Run "dotnet user-secrets list" to verify.
pause
