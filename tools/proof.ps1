param([string]$ListFile, [string]$Dst, [int]$Scale = 5)
Add-Type -AssemblyName System.Drawing
$lines = Get-Content $ListFile | Where-Object { $_.Trim() -ne '' }
$cell = 100
$cols = 8
$rows = [Math]::Ceiling($lines.Count / $cols)
$bmp = New-Object System.Drawing.Bitmap(($cols * $cell), ($rows * ($cell + 18)))
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
$g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::Half
$g.Clear([System.Drawing.Color]::FromArgb(40, 40, 40))
$font = New-Object System.Drawing.Font("Consolas", 8)
$brush = [System.Drawing.Brushes]::White
$cache = @{}
$i = 0
foreach ($line in $lines) {
    $p = $line.Split('|')
    $name = $p[0]; $file = $p[1]
    $x = [int]$p[2]; $y = [int]$p[3]; $w = [int]$p[4]; $h = [int]$p[5]
    if (-not $cache.ContainsKey($file)) {
        $cache[$file] = [System.Drawing.Image]::FromFile("D:\HungNDM\Game\Content\Ninja Adventure - Asset Pack\Backgrounds\Tilesets\$file.png")
    }
    $src = $cache[$file]
    $cx = ($i % $cols) * $cell
    $cy = [Math]::Floor($i / $cols) * ($cell + 18)
    $scaleFit = [Math]::Min([Math]::Floor($cell / $w), [Math]::Floor($cell / $h))
    if ($scaleFit -lt 1) { $scaleFit = 1 }
    $dw = $w * $scaleFit; $dh = $h * $scaleFit
    $destRect = New-Object System.Drawing.Rectangle(($cx + [int](($cell - $dw)/2)), ($cy + [int](($cell - $dh)/2)), $dw, $dh)
    $srcRect = New-Object System.Drawing.Rectangle($x, $y, $w, $h)
    $g.DrawImage($src, $destRect, $srcRect, [System.Drawing.GraphicsUnit]::Pixel)
    $g.DrawString($name, $font, $brush, $cx + 2, $cy + $cell + 2)
    $i++
}
$g.Dispose()
$bmp.Save($Dst, [System.Drawing.Imaging.ImageFormat]::Png)
$bmp.Dispose()
foreach ($img in $cache.Values) { $img.Dispose() }
Write-Host "Saved $Dst with $($lines.Count) crops"
