# Erzeugt Assets\app.ico (Motiv "Flyout": Kachel ueber der Taskleiste).
# Bis 24 Pixel wird das Motiv vereinfacht, damit es klein lesbar bleibt.
param([string]$PreviewPath)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

function New-RoundedPath([single]$x, [single]$y, [single]$w, [single]$h, [single]$r) {
  $d = 2 * $r
  $path = New-Object System.Drawing.Drawing2D.GraphicsPath
  $path.AddArc($x, $y, $d, $d, 180, 90)
  $path.AddArc($x + $w - $d, $y, $d, $d, 270, 90)
  $path.AddArc($x + $w - $d, $y + $h - $d, $d, $d, 0, 90)
  $path.AddArc($x, $y + $h - $d, $d, $d, 90, 90)
  $path.CloseFigure()
  return $path
}

function Fill-Rounded($g, [string]$color, [single]$x, [single]$y, [single]$w, [single]$h, [single]$r) {
  $brush = New-Object System.Drawing.SolidBrush ([System.Drawing.ColorTranslator]::FromHtml($color))
  $path = New-RoundedPath $x $y $w $h $r
  $g.FillPath($brush, $path)
  $path.Dispose(); $brush.Dispose()
}

function New-IconBitmap([int]$size) {
  $bitmap = New-Object System.Drawing.Bitmap $size, $size
  $g = [System.Drawing.Graphics]::FromImage($bitmap)
  $g.SmoothingMode = 'AntiAlias'
  $g.PixelOffsetMode = 'HighQuality'
  # Alle Masse beziehen sich auf eine 64er-Zeichenflaeche.
  $s = $size / 64.0
  $g.ScaleTransform($s, $s)

  Fill-Rounded $g '#1E2A44' 0 0 64 64 14

  if ($size -le 24) {
    Fill-Rounded $g '#EAF1FF' 8 6 48 36 6
    Fill-Rounded $g '#378ADD' 13 11 17 12 3
    Fill-Rounded $g '#1D9E75' 34 11 17 12 3
    Fill-Rounded $g '#EF9F27' 13 25 17 12 3
    Fill-Rounded $g '#D4537E' 34 25 17 12 3
    Fill-Rounded $g '#7F9FDB' 8 47 48 10 5
  } else {
    Fill-Rounded $g '#EAF1FF' 12 8 40 30 5
    Fill-Rounded $g '#378ADD' 17 13 13 9 2.5
    Fill-Rounded $g '#1D9E75' 34 13 13 9 2.5
    Fill-Rounded $g '#EF9F27' 17 25 13 9 2.5
    Fill-Rounded $g '#D4537E' 34 25 13 9 2.5
    $arrow = New-Object System.Drawing.SolidBrush ([System.Drawing.ColorTranslator]::FromHtml('#EAF1FF'))
    $g.FillPolygon($arrow, [System.Drawing.PointF[]]@(
      (New-Object System.Drawing.PointF 27, 37.5),
      (New-Object System.Drawing.PointF 37, 37.5),
      (New-Object System.Drawing.PointF 32, 44)))
    $arrow.Dispose()
    Fill-Rounded $g '#5B7BB5' 8 48 48 8 4
    Fill-Rounded $g '#FFFFFF' 29.5 49.5 5 5 2.5
  }
  $g.Dispose()
  return $bitmap
}

$sizes = 16, 20, 24, 32, 40, 48, 64, 256
$images = foreach ($size in $sizes) {
  $bitmap = New-IconBitmap $size
  $stream = New-Object IO.MemoryStream
  $bitmap.Save($stream, [System.Drawing.Imaging.ImageFormat]::Png)
  $bitmap.Dispose()
  , $stream.ToArray()
}

$writer = New-Object IO.BinaryWriter ([IO.File]::Create((Join-Path $PSScriptRoot 'app.ico')))
$writer.Write([int16]0); $writer.Write([int16]1); $writer.Write([int16]$sizes.Count)
$offset = 6 + 16 * $sizes.Count
for ($i = 0; $i -lt $sizes.Count; $i++) {
  $dimension = if ($sizes[$i] -ge 256) { 0 } else { $sizes[$i] }
  $writer.Write([byte]$dimension); $writer.Write([byte]$dimension); $writer.Write([byte]0); $writer.Write([byte]0)
  $writer.Write([int16]1); $writer.Write([int16]32); $writer.Write([int]$images[$i].Length); $writer.Write([int]$offset)
  $offset += $images[$i].Length
}
foreach ($image in $images) { $writer.Write([byte[]]$image) }
$writer.Close()

if ($PreviewPath) {
  $sheet = New-Object System.Drawing.Bitmap 760, 300
  $g = [System.Drawing.Graphics]::FromImage($sheet)
  $g.Clear([System.Drawing.ColorTranslator]::FromHtml('#1B1B1B'))
  $g.FillRectangle([System.Drawing.Brushes]::WhiteSmoke, 0, 270, 760, 30)
  $g.InterpolationMode = 'NearestNeighbor'
  $x = 12
  foreach ($size in 256, 48, 32, 24, 20, 16) {
    $bitmap = New-IconBitmap $size
    $g.DrawImage($bitmap, $x, 8, $size, $size)
    if ($size -le 24) { $g.DrawImage($bitmap, $x, 90, $size * 4, $size * 4) }
    $g.DrawImage($bitmap, $x, 274, [Math]::Min($size, 24), [Math]::Min($size, 24))
    $bitmap.Dispose()
    $x += [Math]::Max($size, 96) + 14
  }
  $g.Dispose()
  $sheet.Save($PreviewPath, [System.Drawing.Imaging.ImageFormat]::Png)
  $sheet.Dispose()
}
