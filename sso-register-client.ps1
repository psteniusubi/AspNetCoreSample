[CmdletBinding()]
param(
    [parameter()] [uri] $Uri = "https://login.example.ubidemo.com",
    [parameter()] [uri] $ManageUri = "https://manage.example.ubidemo.com",
    [parameter()] [uri] $RedirectUri = "http://localhost:19282/signin-oidc"
)
begin {
    Import-Module "Ubisecure.OAuth2" -ErrorAction Stop
    Import-Module "Ubisecure.SSO.Management" -ErrorAction Stop
    $public_client_config = @"
{
    "redirect_uris":  [
                          "http://localhost/public",
                          "http://localhost/spa.html"
                      ],
    "grant_types":  [
                        "authorization_code"
                    ],
    "client_id":  "public",
    "client_secret":  "public"
}
"@
    New-OAuthClientConfig -Json $public_client_config | New-SSOLogon -Uri $Uri -ManageUri $ManageUri -Browser "default" 
}
process {
    Get-OAuthMetadata -Authority ([uri]::new($Uri, "/uas")) | ConvertTo-Json | Set-Content -Path "openid-configuration.json" -Force
    $password1 = Get-SSOObject -Type "method" "password.1" -ErrorAction Stop
    $sms1 = Get-SSOObject -Type "method" "sms.1" -ErrorAction Stop
    $smtp1 = Get-SSOObject -Type "method" "smtp.1" -ErrorAction Stop
    $google1 = Get-SSOObject -Type "method" "oidc.google.1" -ErrorAction Stop
    $facebook1 = Get-SSOObject -Type "method" "oidc.facebook.1" -ErrorAction Stop
    $allusers = Get-SSOObject -Type "group" "System","Authenticated Users" -ErrorAction Stop

    $site = Set-SSOObject -Type "site" "AspNetCoreSample"
    $password1,$sms1,$smtp1,$google1,$facebook1 | Set-SSOLink -Link $site | Out-Null

    $users = $site | Set-SSOChild -ChildType "group" "users"
    $users | Set-SSOLink -LinkName "member" -Link $allusers | Out-Null
    $sms1,$smtp1,$google1,$facebook1 | Set-SSOLink -Link $users | Out-Null

    $policy = $site | Set-SSOChild -ChildType "policy" "policy"

    $app = $site | Set-SSOChild -ChildType "application" $RedirectUri.Authority -Enabled 
    $app | Set-SSOLink -LinkName "allowedTo" -Link $users | Out-Null
    $app | Set-SSOLink -Link $policy | Out-Null
    $password1,$sms1,$smtp1,$google1,$facebook1 | Set-SSOLink -Link $app -Enabled | Out-Null

    $request = @{
    "grant_types"=@("authorization_code")
    "scope"="openid"
    "redirect_uris"=@($RedirectUri)
    } | ConvertTo-Json
    $response = $app | Set-SSOAttribute -Name "metadata" -ContentType "application/json" -Body $request

    $response | ConvertTo-Json | Set-Content -Path "client-config.json" -Force
}
