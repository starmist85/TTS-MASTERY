<#
.SYNOPSIS
    Capture a screenshot of a running window so you can do the fresh-eyes pass on a
    still image.

.DESCRIPTION
    Looking at a live window, you unconsciously filter out misalignment; in a static
    screenshot you see it immediately. Run the app, capture it, then open the PNG and
    check the fresh-eyes list in SKILL.md against it.

    Uses DWM extended frame bounds rather than GetWindowRect so the capture excludes
    the invisible resize border Windows 10/11 add around every window (otherwise you
    get ~7px of desktop bleeding into every edge).

.PARAMETER ProcessName
    Process name without .exe, e.g. "LocalTtsStudio".

.PARAMETER OutputPath
    Where to write the PNG. Defaults to .\window.png

.PARAMETER DelaySeconds
    Wait before capturing, to let the window finish rendering or to give yourself
    time to hover/open a menu you want in the shot.

.EXAMPLE
    .\Capture-WindowScreenshot.ps1 -ProcessName LocalTtsStudio -OutputPath .\shots\generate.png
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$ProcessName,
    [string]$OutputPath = (Join-Path (Get-Location) 'window.png'),
    [int]$DelaySeconds = 1
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

if (-not ('WpfDesign.Win32' -as [type])) {
    Add-Type -Namespace WpfDesign -Name Win32 -MemberDefinition @'
[StructLayout(LayoutKind.Sequential)]
public struct RECT { public int Left, Top, Right, Bottom; }

[DllImport("user32.dll")]
public static extern bool SetForegroundWindow(IntPtr hWnd);

[DllImport("user32.dll")]
public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

[DllImport("dwmapi.dll")]
public static extern int DwmGetWindowAttribute(IntPtr hwnd, int attr, out RECT value, int size);
'@ -UsingNamespace System.Runtime.InteropServices
}

$proc = Get-Process -Name $ProcessName -ErrorAction SilentlyContinue |
        Where-Object { $_.MainWindowHandle -ne 0 } |
        Select-Object -First 1

if (-not $proc) {
    throw "No running process named '$ProcessName' with a visible main window. Start the app first."
}

$hwnd = $proc.MainWindowHandle
[void][WpfDesign.Win32]::ShowWindow($hwnd, 9)          # SW_RESTORE
[void][WpfDesign.Win32]::SetForegroundWindow($hwnd)
Start-Sleep -Seconds $DelaySeconds

# DWMWA_EXTENDED_FRAME_BOUNDS = 9 — the real visible bounds.
$rect = New-Object WpfDesign.Win32+RECT
$size = [System.Runtime.InteropServices.Marshal]::SizeOf($rect)
$hr = [WpfDesign.Win32]::DwmGetWindowAttribute($hwnd, 9, [ref]$rect, $size)
if ($hr -ne 0) { throw "DwmGetWindowAttribute failed (HRESULT 0x{0:X})." -f $hr }

$width  = $rect.Right - $rect.Left
$height = $rect.Bottom - $rect.Top
if ($width -le 0 -or $height -le 0) { throw "Window reported a zero-size rectangle; is it minimised?" }

$bmp = New-Object System.Drawing.Bitmap $width, $height
try {
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    try {
        $g.CopyFromScreen($rect.Left, $rect.Top, 0, 0, $bmp.Size)
    } finally { $g.Dispose() }

    $dir = Split-Path -Parent $OutputPath
    if ($dir -and -not (Test-Path $dir)) { New-Item -ItemType Directory -Path $dir -Force | Out-Null }

    $bmp.Save($OutputPath, [System.Drawing.Imaging.ImageFormat]::Png)
} finally { $bmp.Dispose() }

Write-Host "Saved ${width}x${height} screenshot to $OutputPath"
