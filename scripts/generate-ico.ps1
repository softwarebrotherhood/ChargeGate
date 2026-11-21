Add-Type -AssemblyName System.Drawing

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$srcImage = Join-Path $scriptDir "..\docs\assets\icon.png"
$destIco = Join-Path $scriptDir "..\src\ChargeGate.UI\Resources\app.ico"
$installerIco = Join-Path $scriptDir "..\installer\app.ico"

# Load source image
$bmp = [System.Drawing.Image]::FromFile($srcImage)

# Save high-res PNG for web
$bmp.Save($destPng, [System.Drawing.Imaging.ImageFormat]::Png)

# Icon sizes
$sizes = @(256, 128, 64, 48, 32, 16)
$pngStreams = @()

foreach ($s in $sizes) {
    $resized = New-Object System.Drawing.Bitmap($s, $s)
    $g = [System.Drawing.Graphics]::FromImage($resized)
    $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
    $g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $g.DrawImage($bmp, 0, 0, $s, $s)
    $g.Dispose()

    $ms = New-Object System.IO.MemoryStream
    $resized.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    $resized.Dispose()
    $pngStreams += $ms
}

$bmp.Dispose()

# Build ICO binary
$fs = [System.IO.File]::Create($destIco)
$bw = New-Object System.IO.BinaryWriter($fs)

# Header
$bw.Write([UInt16]0)            # Reserved
$bw.Write([UInt16]1)            # 1 = ICO
$bw.Write([UInt16]$sizes.Count) # Image count

$offset = 6 + ($sizes.Count * 16)

for ($i = 0; $i -lt $sizes.Count; $i++) {
    $s = $sizes[$i]
    $w = if ($s -ge 256) { 0 } else { [byte]$s }
    $h = if ($s -ge 256) { 0 } else { [byte]$s }
    $data = $pngStreams[$i].ToArray()

    $bw.Write([byte]$w)
    $bw.Write([byte]$h)
    $bw.Write([byte]0)       # Color count
    $bw.Write([byte]0)       # Reserved
    $bw.Write([UInt16]1)     # Color planes
    $bw.Write([UInt16]32)    # Bits per pixel
    $bw.Write([UInt32]$data.Length) # Image size
    $bw.Write([UInt32]$offset)      # Image offset

    $offset += $data.Length
}

for ($i = 0; $i -lt $sizes.Count; $i++) {
    $data = $pngStreams[$i].ToArray()
    $bw.Write($data)
    $pngStreams[$i].Dispose()
}

$bw.Flush()
$bw.Close()
$fs.Close()

Copy-Item $destIco $installerIco -Force
Write-Host "Successfully generated multi-res app.ico at: $destIco"
