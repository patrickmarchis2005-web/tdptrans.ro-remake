[CmdletBinding()]
param(
    [string]$OutputDir = '',
    [string[]]$DnsNames = @(),
    [string[]]$IpAddresses = @()
)

$ErrorActionPreference = 'Stop'

$rootFriendlyName = 'TdpTrans Frontend LAN Root CA'
$leafFriendlyName = 'TdpTrans Frontend LAN HTTPS'
$rootSubject = 'CN=TdpTrans Frontend LAN Root CA'
$defaultLeafCommonName = 'tdptrans-frontend-lan'

$scriptDirectory = Split-Path -Parent $MyInvocation.MyCommand.Path

if (-not $OutputDir) {
    $OutputDir = Join-Path $scriptDirectory '..\certificates\lan-https'
}

$OutputDir = [System.IO.Path]::GetFullPath($OutputDir)

$rootCertificatePath = Join-Path $OutputDir 'tdptrans-lan-root.cer'
$leafPfxPath = Join-Path $OutputDir 'tdptrans-frontend-lan.pfx'
$leafPassphrasePath = Join-Path $OutputDir 'tdptrans-frontend-lan.passphrase.txt'
$hostsMetadataPath = Join-Path $OutputDir 'tdptrans-frontend-lan-hosts.json'

function Get-UniqueValues {
    param([string[]]$Values)

    $seenValues = New-Object 'System.Collections.Generic.HashSet[string]' ([System.StringComparer]::OrdinalIgnoreCase)
    $orderedValues = New-Object 'System.Collections.Generic.List[string]'

    foreach ($value in $Values) {
        if ($null -eq $value) {
            continue
        }

        $trimmedValue = $value.Trim()
        if ($trimmedValue -and $seenValues.Add($trimmedValue)) {
            [void]$orderedValues.Add($trimmedValue)
        }
    }

    return [string[]]$orderedValues
}

function Get-DefaultDnsNames {
    $dnsNames = @('localhost', $env:COMPUTERNAME, [System.Net.Dns]::GetHostName())

    try {
        $hostEntry = [System.Net.Dns]::GetHostEntry([System.Net.Dns]::GetHostName())
        if ($hostEntry.HostName) {
            $dnsNames += $hostEntry.HostName
        }
    }
    catch {
        # Hostname lookup is best-effort only.
    }

    return Get-UniqueValues -Values $dnsNames
}

function Get-DefaultIpAddresses {
    $addresses = @('127.0.0.1')
    $gatewayInterfaceIds = @(
        Get-NetIPConfiguration -ErrorAction SilentlyContinue |
            Where-Object {
                $_.NetAdapter.Status -eq 'Up' -and
                $_.IPv4DefaultGateway
            } |
            Select-Object -ExpandProperty InterfaceIndex
    )
    $upInterfaceIds = @(
        Get-NetAdapter -Physical -ErrorAction SilentlyContinue |
            Where-Object Status -eq 'Up' |
            Select-Object -ExpandProperty ifIndex
    )

    if ($upInterfaceIds.Count -eq 0) {
        $upInterfaceIds = @(
            Get-NetAdapter -ErrorAction SilentlyContinue |
                Where-Object Status -eq 'Up' |
                Select-Object -ExpandProperty ifIndex
        )
    }

    $candidateAddresses = @(
        Get-NetIPAddress -AddressFamily IPv4 -ErrorAction SilentlyContinue |
            Where-Object {
                $_.IPAddress -and
                $_.IPAddress -ne '127.0.0.1' -and
                $_.IPAddress -notlike '169.254.*' -and
                ($gatewayInterfaceIds.Count -eq 0 -or $gatewayInterfaceIds -contains $_.InterfaceIndex)
            } |
            Select-Object -ExpandProperty IPAddress
    )

    if ($candidateAddresses.Count -eq 0) {
        $candidateAddresses = @(
            Get-NetIPAddress -AddressFamily IPv4 -ErrorAction SilentlyContinue |
                Where-Object {
                    $_.IPAddress -and
                    $_.IPAddress -ne '127.0.0.1' -and
                    $_.IPAddress -notlike '169.254.*' -and
                    ($upInterfaceIds.Count -eq 0 -or $upInterfaceIds -contains $_.InterfaceIndex)
                } |
                Select-Object -ExpandProperty IPAddress
        )
    }

    $addresses += $candidateAddresses
    return Get-UniqueValues -Values $addresses
}

function Get-OrCreateRootCertificate {
    $rootCertificate = Get-ChildItem -Path Cert:\CurrentUser\My |
        Where-Object { $_.FriendlyName -eq $rootFriendlyName } |
        Sort-Object NotAfter -Descending |
        Select-Object -First 1

    if (-not $rootCertificate) {
        $rootCertificate = New-SelfSignedCertificate `
            -Type Custom `
            -Subject $rootSubject `
            -FriendlyName $rootFriendlyName `
            -Provider 'Microsoft Software Key Storage Provider' `
            -KeyAlgorithm RSA `
            -KeyLength 2048 `
            -KeyProtection None `
            -HashAlgorithm SHA256 `
            -KeyExportPolicy Exportable `
            -KeyUsage CertSign, CRLSign, DigitalSignature `
            -KeyUsageProperty Sign `
            -NotAfter (Get-Date).AddYears(5) `
            -TextExtension @(
                '2.5.29.19={critical}{text}CA=true&pathlength=1'
            ) `
            -CertStoreLocation Cert:\CurrentUser\My
    }

    $trustedRoot = Get-ChildItem -Path Cert:\CurrentUser\Root |
        Where-Object Thumbprint -eq $rootCertificate.Thumbprint |
        Select-Object -First 1

    if (-not $trustedRoot) {
        $rootStore = New-Object System.Security.Cryptography.X509Certificates.X509Store 'Root', 'CurrentUser'
        $rootStore.Open([System.Security.Cryptography.X509Certificates.OpenFlags]::ReadWrite)
        try {
            $rootStore.Add($rootCertificate)
        }
        finally {
            $rootStore.Close()
        }
    }

    return $rootCertificate
}

function Remove-ExistingLeafCertificates {
    $existingCertificates = Get-ChildItem -Path Cert:\CurrentUser\My |
        Where-Object { $_.FriendlyName -eq $leafFriendlyName }

    foreach ($certificate in $existingCertificates) {
        try {
            Remove-Item -Path $certificate.PSPath -DeleteKey
        }
        catch {
            Remove-Item -Path $certificate.PSPath
        }
    }
}

function New-LeafCertificate {
    param(
        [System.Security.Cryptography.X509Certificates.X509Certificate2]$SignerCertificate,
        [string[]]$ResolvedDnsNames,
        [string[]]$ResolvedIpAddresses
    )

    $subjectCommonName = if ($ResolvedDnsNames.Count -gt 0) { $ResolvedDnsNames[0] } else { $defaultLeafCommonName }
    $sanEntries = @(
        $ResolvedDnsNames | ForEach-Object { "DNS=$_" }
        $ResolvedIpAddresses | ForEach-Object { "IP Address=$_" }
    )

    return New-SelfSignedCertificate `
        -Type Custom `
        -Subject "CN=$subjectCommonName" `
        -FriendlyName $leafFriendlyName `
        -Signer $SignerCertificate `
        -KeyAlgorithm RSA `
        -KeyLength 2048 `
        -KeyProtection None `
        -HashAlgorithm SHA256 `
        -KeyExportPolicy Exportable `
        -KeyUsage DigitalSignature, KeyEncipherment `
        -TextExtension @(
            "2.5.29.17={text}$($sanEntries -join '&')",
            '2.5.29.37={text}1.3.6.1.5.5.7.3.1'
        ) `
        -NotAfter (Get-Date).AddYears(2) `
        -CertStoreLocation Cert:\CurrentUser\My
}

function Get-OrCreatePassphrase {
    if (Test-Path -LiteralPath $leafPassphrasePath) {
        return (Get-Content -Path $leafPassphrasePath -Raw).Trim()
    }

    $generatedPassphrase = ([Guid]::NewGuid().ToString('N') + [Guid]::NewGuid().ToString('N'))
    Set-Content -Path $leafPassphrasePath -Value $generatedPassphrase -NoNewline
    return $generatedPassphrase
}

$resolvedDnsNames = if ($DnsNames.Count -gt 0) { Get-UniqueValues -Values $DnsNames } else { Get-DefaultDnsNames }
$resolvedIpAddresses = if ($IpAddresses.Count -gt 0) { Get-UniqueValues -Values $IpAddresses } else { Get-DefaultIpAddresses }

if ($resolvedDnsNames.Count -eq 0) {
    throw 'Nu am putut determina niciun nume DNS pentru certificatul frontend.'
}

if ($resolvedIpAddresses.Count -eq 0) {
    throw 'Nu am putut determina nicio adresa IP pentru certificatul frontend.'
}

New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null

$rootCertificate = Get-OrCreateRootCertificate
Remove-ExistingLeafCertificates
$leafCertificate = New-LeafCertificate -SignerCertificate $rootCertificate -ResolvedDnsNames $resolvedDnsNames -ResolvedIpAddresses $resolvedIpAddresses
$leafPassphrase = Get-OrCreatePassphrase
$leafSecurePassphrase = ConvertTo-SecureString -String $leafPassphrase -AsPlainText -Force

Export-Certificate -Cert $rootCertificate -FilePath $rootCertificatePath -Force | Out-Null
Export-PfxCertificate -Cert $leafCertificate -FilePath $leafPfxPath -Password $leafSecurePassphrase -Force | Out-Null

$hostsMetadata = [pscustomobject]@{
    generatedAtUtc = (Get-Date).ToUniversalTime().ToString('o')
    dnsNames = $resolvedDnsNames
    ipAddresses = $resolvedIpAddresses
    suggestedUrls = @($resolvedDnsNames | ForEach-Object { "https://$($_):5173" }) +
        @($resolvedIpAddresses | Where-Object { $_ -ne '127.0.0.1' } | ForEach-Object { "https://$($_):5173" })
    rootCertificate = [pscustomobject]@{
        subject = $rootCertificate.Subject
        thumbprint = $rootCertificate.Thumbprint
        exportedPath = $rootCertificatePath
    }
    frontendCertificate = [pscustomobject]@{
        subject = $leafCertificate.Subject
        thumbprint = $leafCertificate.Thumbprint
        exportedPfxPath = $leafPfxPath
    }
}

$hostsMetadata | ConvertTo-Json -Depth 6 | Set-Content -Path $hostsMetadataPath

Write-Host 'Certificatul HTTPS pentru frontend a fost generat.'
Write-Host "Root CA exportat: $rootCertificatePath"
Write-Host "Certificat frontend exportat: $leafPfxPath"
Write-Host 'Importa root CA pe fiecare dispozitiv client in Trusted Root Certification Authorities.'
Write-Host 'URL-uri sugerate:'
$hostsMetadata.suggestedUrls | ForEach-Object { Write-Host " - $_" }
