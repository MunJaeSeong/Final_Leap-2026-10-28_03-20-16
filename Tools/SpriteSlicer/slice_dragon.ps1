Add-Type -AssemblyName System.Drawing
$src = (Get-ChildItem "C:\Users\awt48\Documents\GitHub\Final_Leap\Assets\Image" -Filter "*.png" | Where-Object { $_.Name -like "*빙결*" }).FullName
$sheet = New-Object System.Drawing.Bitmap $src
$dest = "C:\Users\awt48\Documents\GitHub\Final_Leap\Assets\Resources\Art\Dragon"
if (Test-Path $dest) { Get-ChildItem $dest -Filter "*.png" | Remove-Item -Force }
New-Item -ItemType Directory -Force -Path $dest | Out-Null

function Is-Bg([System.Drawing.Color]$c) {
  $lum = ($c.R + $c.G + $c.B) / 3.0
  $sat = [Math]::Max($c.R, [Math]::Max($c.G, $c.B)) - [Math]::Min($c.R, [Math]::Min($c.G, $c.B))
  return ($lum -ge 214 -and $sat -lt 28 -and $c.B -lt ($c.R + 16))
}

function Export-Set($prefix, $y0, $y1, $spans) {
  $frames = New-Object System.Collections.Generic.List[object]
  foreach ($span in $spans) {
    $x0 = [int]$span[0]
    $x1 = [int]$span[1]
    $w = $x1 - $x0 + 1
    $h = $y1 - $y0 + 1
    $crop = New-Object System.Drawing.Bitmap $w, $h, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $sumX = 0.0; $sumY = 0.0; $count = 0
    for ($y = 0; $y -lt $h; $y++) {
      for ($x = 0; $x -lt $w; $x++) {
        $c = $sheet.GetPixel($x0 + $x, $y0 + $y)
        if (Is-Bg $c) { continue }
        $crop.SetPixel($x, $y, [System.Drawing.Color]::FromArgb(255, $c.R, $c.G, $c.B))
        $lum = ($c.R + $c.G + $c.B) / 3.0
        if ($lum -lt 150) { $sumX += $x; $sumY += $y; $count++ }
      }
    }
    if ($count -lt 30) { $crop.Dispose(); continue }
    $frames.Add([pscustomobject]@{ Image = $crop; Cx = ($sumX / $count); Cy = ($sumY / $count) })
  }
  $left = 1.0; $right = 1.0; $up = 1.0; $down = 1.0
  foreach ($f in $frames) {
    $left = [Math]::Max($left, $f.Cx)
    $right = [Math]::Max($right, $f.Image.Width - $f.Cx)
    $up = [Math]::Max($up, $f.Cy)
    $down = [Math]::Max($down, $f.Image.Height - $f.Cy)
  }
  $cw = [int][Math]::Ceiling($left + $right + 12)
  $ch = [int][Math]::Ceiling($up + $down + 12)
  $ox = [int](6 + $left)
  $oy = [int](6 + $up)
  for ($i = 0; $i -lt $frames.Count; $i++) {
    $f = $frames[$i]
    $canvas = New-Object System.Drawing.Bitmap $cw, $ch, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($canvas)
    $dx = [int]($ox - $f.Cx)
    $dy = [int]($oy - $f.Cy)
    $g.DrawImage($f.Image, $dx, $dy)
    $g.Dispose()
    $file = Join-Path $dest ("{0}_{1:00}.png" -f $prefix, $i)
    $canvas.Save($file, [System.Drawing.Imaging.ImageFormat]::Png)
    $canvas.Dispose()
    $f.Image.Dispose()
  }
  Write-Output "$prefix $($frames.Count) ${cw}x${ch}"
}

Export-Set "d_fly" 58 256 @(
  @(40,264), @(278,504), @(510,700), @(704,892), @(886,1064),
  @(1068,1240), @(1238,1412), @(1410,1590), @(1594,1754)
)
Export-Set "d_dash" 300 450 @(
  @(40,280), @(282,500), @(500,700), @(702,905), @(906,1110),
  @(1112,1330), @(1332,1585), @(1588,1768)
)
Export-Set "d_roar" 488 676 @(
  @(24,275), @(290,535), @(540,760), @(770,985), @(988,1178),
  @(1185,1385), @(1386,1562), @(1564,1724)
)
Export-Set "d_breath" 705 862 @(
  @(24,290), @(292,538), @(530,785), @(775,1035),
  @(1028,1295), @(1296,1768)
)
$sheet.Dispose()
