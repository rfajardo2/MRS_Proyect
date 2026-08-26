param(
    [Parameter(Mandatory = $true)]
    [string]$Root,

    [int]$Port = 5500
)

$ErrorActionPreference = "Stop"

$resolvedRoot = (Resolve-Path -LiteralPath $Root).Path
$listener = [System.Net.Sockets.TcpListener]::new([System.Net.IPAddress]::Loopback, $Port)
$listener.Start()

Write-Host "Serving $resolvedRoot at http://localhost:$Port/"

function Get-ContentType {
    param([string]$Path)

    switch ([System.IO.Path]::GetExtension($Path).ToLowerInvariant()) {
        ".html" { "text/html; charset=utf-8"; break }
        ".css" { "text/css; charset=utf-8"; break }
        ".js" { "application/javascript; charset=utf-8"; break }
        ".json" { "application/json; charset=utf-8"; break }
        ".svg" { "image/svg+xml"; break }
        ".png" { "image/png"; break }
        ".jpg" { "image/jpeg"; break }
        ".jpeg" { "image/jpeg"; break }
        ".ico" { "image/x-icon"; break }
        ".woff" { "font/woff"; break }
        ".woff2" { "font/woff2"; break }
        default { "application/octet-stream" }
    }
}

function Send-Response {
    param(
        [System.Net.Sockets.NetworkStream]$Stream,
        [int]$StatusCode,
        [string]$StatusText,
        [byte[]]$Body,
        [string]$ContentType
    )

    $headers = @(
        "HTTP/1.1 $StatusCode $StatusText",
        "Content-Type: $ContentType",
        "Content-Length: $($Body.Length)",
        "Connection: close",
        "",
        ""
    ) -join "`r`n"

    $headerBytes = [System.Text.Encoding]::ASCII.GetBytes($headers)
    $Stream.Write($headerBytes, 0, $headerBytes.Length)
    if ($Body.Length -gt 0) {
        $Stream.Write($Body, 0, $Body.Length)
    }
}

while ($true) {
    $client = $listener.AcceptTcpClient()
    try {
        $stream = $client.GetStream()
        $buffer = New-Object byte[] 8192
        $read = $stream.Read($buffer, 0, $buffer.Length)
        if ($read -le 0) {
            $client.Close()
            continue
        }

        $requestText = [System.Text.Encoding]::ASCII.GetString($buffer, 0, $read)
        $requestLine = ($requestText -split "`r`n")[0]
        $parts = $requestLine -split " "
        $requestPath = if ($parts.Length -ge 2) { $parts[1] } else { "/" }
        $requestPath = [System.Uri]::UnescapeDataString($requestPath.Split("?")[0].TrimStart("/"))

        if ([string]::IsNullOrWhiteSpace($requestPath)) {
            $requestPath = "index.html"
        }

        $relativePath = $requestPath.Replace("/", [System.IO.Path]::DirectorySeparatorChar)
        $fullPath = [System.IO.Path]::GetFullPath((Join-Path $resolvedRoot $relativePath))
        if (-not $fullPath.StartsWith($resolvedRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
            $body = [System.Text.Encoding]::UTF8.GetBytes("Forbidden")
            Send-Response $stream 403 "Forbidden" $body "text/plain; charset=utf-8"
            continue
        }

        if (-not (Test-Path -LiteralPath $fullPath -PathType Leaf)) {
            $fullPath = Join-Path $resolvedRoot "index.html"
        }

        $bytes = [System.IO.File]::ReadAllBytes($fullPath)
        Send-Response $stream 200 "OK" $bytes (Get-ContentType $fullPath)
    } catch {
        if ($stream) {
            $body = [System.Text.Encoding]::UTF8.GetBytes("Internal Server Error")
            Send-Response $stream 500 "Internal Server Error" $body "text/plain; charset=utf-8"
        }
    } finally {
        $client.Close()
    }
}
