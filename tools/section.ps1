param([string]$Src, [string]$Dst, [int]$X, [int]$Y, [int]$W, [int]$H, [int]$Scale = 6)
Add-Type -AssemblyName System.Drawing
$img = [System.Drawing.Image]::FromFile($Src)
$bmp = New-Object System.Drawing.Bitmap(($W * $Scale), ($H * $Scale))
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
$g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::Half
$g.Clear([System.Drawing.Color]::Magenta)
$srcRect = New-Object System.Drawing.Rectangle($X, $Y, $W, $H)
$dstRect = New-Object System.Drawing.Rectangle(0, 0, ($W * $Scale), ($H * $Scale))
$g.DrawImage($img, $dstRect, $srcRect, [System.Drawing.GraphicsUnit]::Pixel)
$pen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(150, 255, 0, 0), 1)
$font = New-Object System.Drawing.Font("Consolas", 9)
$brush = [System.Drawing.Brushes]::Blue
for ($gx = 0; $gx -le $W; $gx += 16) { $g.DrawLine($pen, ($gx * $Scale), 0, ($gx * $Scale), ($H * $Scale)) }
for ($gy = 0; $gy -le $H; $gy += 16) { $g.DrawLine($pen, 0, ($gy * $Scale), ($W * $Scale), ($gy * $Scale)) }
for ($gx = 0; $gx -lt $W; $gx += 32) {
    for ($gy = 0; $gy -lt $H; $gy += 32) {
        $g.DrawString("$($X + $gx),$($Y + $gy)", $font, $brush, ($gx * $Scale + 2), ($gy * $Scale + 2))
    }
}
$g.Dispose()
$bmp.Save($Dst, [System.Drawing.Imaging.ImageFormat]::Png)
$bmp.Dispose()
$img.Dispose()
Write-Host "Saved $Dst"
