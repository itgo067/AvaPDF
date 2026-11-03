param(
    [string]$AssetsPath = (Join-Path $PSScriptRoot '../src/AvaTools.Desktop/Assets')
)

Add-Type -AssemblyName System.Drawing

function New-RoundedRectangle([float]$x, [float]$y, [float]$width, [float]$height, [float]$radius) {
    $path = New-Object System.Drawing.Drawing2D.GraphicsPath
    $diameter = $radius * 2
    $path.AddArc($x, $y, $diameter, $diameter, 180, 90)
    $path.AddArc($x + $width - $diameter, $y, $diameter, $diameter, 270, 90)
    $path.AddArc($x + $width - $diameter, $y + $height - $diameter, $diameter, $diameter, 0, 90)
    $path.AddArc($x, $y + $height - $diameter, $diameter, $diameter, 90, 90)
    $path.CloseFigure()
    return $path
}

function Render-Logo([int]$size, [bool]$markOnly = $false) {
    $bitmap = New-Object System.Drawing.Bitmap $size, $size, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $graphics.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality
    $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $graphics.ScaleTransform($size / 512.0, $size / 512.0)

    if (-not $markOnly) {
        $backgroundPath = New-RoundedRectangle 16 16 480 480 112
        $backgroundBrush = New-Object System.Drawing.Drawing2D.LinearGradientBrush(
            ([System.Drawing.RectangleF]::new(16, 16, 480, 480)),
            ([System.Drawing.ColorTranslator]::FromHtml('#282154')),
            ([System.Drawing.ColorTranslator]::FromHtml('#10182C')),
            42.0)
        $graphics.FillPath($backgroundBrush, $backgroundPath)
        $borderPen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(36, 185, 174, 255)), 3
        $graphics.DrawPath($borderPen, $backgroundPath)
        $borderPen.Dispose()
        $backgroundBrush.Dispose()
        $backgroundPath.Dispose()
    }

    $accentBrush = New-Object System.Drawing.Drawing2D.LinearGradientBrush(
        ([System.Drawing.RectangleF]::new(128, 90, 260, 290)),
        ([System.Drawing.ColorTranslator]::FromHtml('#A996FF')),
        ([System.Drawing.ColorTranslator]::FromHtml('#52D6D0')),
        28.0)

    if ($size -ge 24) {
        $pagePen = New-Object System.Drawing.Pen $accentBrush, 22
        $pagePen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
        $pagePen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
        $pagePen.LineJoin = [System.Drawing.Drawing2D.LineJoin]::Round
        $pagePen.Color = [System.Drawing.Color]::FromArgb(184, 139, 124, 255)
        $page = New-Object System.Drawing.Drawing2D.GraphicsPath
        $page.AddLines([System.Drawing.PointF[]]@(
            [System.Drawing.PointF]::new(180, 104),
            [System.Drawing.PointF]::new(309, 104),
            [System.Drawing.PointF]::new(350, 145),
            [System.Drawing.PointF]::new(350, 300)))
        $graphics.DrawPath($pagePen, $page)
        $graphics.DrawLines($pagePen, [System.Drawing.PointF[]]@(
            [System.Drawing.PointF]::new(309, 104),
            [System.Drawing.PointF]::new(309, 145),
            [System.Drawing.PointF]::new(350, 145)))
        $page.Dispose()
        $pagePen.Dispose()
    }

    $paperBrush = New-Object System.Drawing.Drawing2D.LinearGradientBrush(
        ([System.Drawing.RectangleF]::new(128, 120, 258, 256)),
        [System.Drawing.Color]::White,
        ([System.Drawing.ColorTranslator]::FromHtml('#E9E9FF')),
        32.0)
    $letter = New-Object System.Drawing.Drawing2D.GraphicsPath
    $letter.FillMode = [System.Drawing.Drawing2D.FillMode]::Alternate
    $letter.AddLines([System.Drawing.PointF[]]@(
        [System.Drawing.PointF]::new(128, 376),
        [System.Drawing.PointF]::new(218, 142),
        [System.Drawing.PointF]::new(225, 126),
        [System.Drawing.PointF]::new(238, 120),
        [System.Drawing.PointF]::new(249, 120),
        [System.Drawing.PointF]::new(263, 120),
        [System.Drawing.PointF]::new(276, 124),
        [System.Drawing.PointF]::new(288, 135),
        [System.Drawing.PointF]::new(294, 142),
        [System.Drawing.PointF]::new(386, 376),
        [System.Drawing.PointF]::new(320, 376),
        [System.Drawing.PointF]::new(299, 320),
        [System.Drawing.PointF]::new(213, 320),
        [System.Drawing.PointF]::new(192, 376),
        [System.Drawing.PointF]::new(128, 376)))
    $letter.AddPolygon([System.Drawing.PointF[]]@(
        [System.Drawing.PointF]::new(236, 264),
        [System.Drawing.PointF]::new(276, 264),
        [System.Drawing.PointF]::new(256, 207)))
    $graphics.FillPath($paperBrush, $letter)

    $crossbar = New-Object System.Drawing.Drawing2D.GraphicsPath
    $crossbar.AddPolygon([System.Drawing.PointF[]]@(
        [System.Drawing.PointF]::new(213, 320),
        [System.Drawing.PointF]::new(229, 272),
        [System.Drawing.PointF]::new(283, 272),
        [System.Drawing.PointF]::new(299, 320)))
    $graphics.FillPath($accentBrush, $crossbar)

    if ($size -ge 48) {
        $dotBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.ColorTranslator]::FromHtml('#66E3DC'))
        $graphics.FillEllipse($dotBrush, 365, 81, 20, 20)
        $dotBrush.Dispose()
    }

    $crossbar.Dispose()
    $letter.Dispose()
    $paperBrush.Dispose()
    $accentBrush.Dispose()
    $graphics.Dispose()
    return $bitmap
}

function Save-Png([int]$size, [string]$path, [bool]$markOnly = $false) {
    $bitmap = Render-Logo $size $markOnly
    $bitmap.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
    $bitmap.Dispose()
}

$resolvedAssets = [System.IO.Path]::GetFullPath($AssetsPath)
[System.IO.Directory]::CreateDirectory($resolvedAssets) | Out-Null
Save-Png 512 (Join-Path $resolvedAssets 'AppIcon.png')
Save-Png 512 (Join-Path $resolvedAssets 'LogoMark.png') $true

$iconSizes = @(16, 24, 32, 48, 64, 128, 256)
$images = @()
foreach ($size in $iconSizes) {
    $bitmap = Render-Logo $size
    $stream = New-Object System.IO.MemoryStream
    $bitmap.Save($stream, [System.Drawing.Imaging.ImageFormat]::Png)
    $images += ,@{ Size = $size; Bytes = $stream.ToArray() }
    $stream.Dispose()
    $bitmap.Dispose()
}

$iconPath = Join-Path $resolvedAssets 'AppIcon.ico'
$file = [System.IO.File]::Create($iconPath)
$writer = New-Object System.IO.BinaryWriter $file
$writer.Write([uint16]0)
$writer.Write([uint16]1)
$writer.Write([uint16]$images.Count)
$offset = 6 + (16 * $images.Count)
foreach ($image in $images) {
    $dimension = if ($image.Size -eq 256) { 0 } else { $image.Size }
    $writer.Write([byte]$dimension)
    $writer.Write([byte]$dimension)
    $writer.Write([byte]0)
    $writer.Write([byte]0)
    $writer.Write([uint16]1)
    $writer.Write([uint16]32)
    $writer.Write([uint32]$image.Bytes.Length)
    $writer.Write([uint32]$offset)
    $offset += $image.Bytes.Length
}
foreach ($image in $images) {
    $writer.Write($image.Bytes)
}
$writer.Flush()
$writer.Dispose()
$file.Dispose()

Write-Output "Generated AvaTools logo assets in $resolvedAssets"
