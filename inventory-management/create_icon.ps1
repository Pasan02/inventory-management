[System.Reflection.Assembly]::LoadWithPartialName('System.Drawing') | Out-Null

$originalPng = 'C:\Users\Upeka\.gemini\antigravity-ide\brain\907e651f-c738-4ebe-829c-1f0ad1c52a00\media__1780777162642.png'
$outIcoPath = 'c:\Users\Upeka\Desktop\Projects\inventory-management\inventory-management\Resources\app.ico'

# Ensure Resources directory exists
$resourcesDir = 'c:\Users\Upeka\Desktop\Projects\inventory-management\inventory-management\Resources'
if (!(Test-Path $resourcesDir)) {
    New-Item -ItemType Directory -Path $resourcesDir | Out-Null
}

try {
    # 1. Load original image (1024x465)
    $srcImg = [System.Drawing.Image]::FromFile($originalPng)
    $width = $srcImg.Width
    $height = $srcImg.Height # 465

    # 2. Create extended height canvas (1024x560) to draw the text at the bottom
    $extendedHeight = 560
    $extendedBmp = New-Object System.Drawing.Bitmap($width, $extendedHeight)
    $g1 = [System.Drawing.Graphics]::FromImage($extendedBmp)
    $g1.Clear([System.Drawing.Color]::White)
    
    # Set high quality graphics
    $g1.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $g1.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
    $g1.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    
    # Draw original image
    $g1.DrawImage($srcImg, 0, 0, $width, $height)
    
    # Draw text "INVENTORY MANAGEMENT SYSTEM"
    # Font style
    $fontFamily = New-Object System.Drawing.FontFamily("Arial")
    $font = New-Object System.Drawing.Font($fontFamily, 28, [System.Drawing.FontStyle]::Bold)
    $brush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 15, 23, 42)) # Elegant near-black slate
    
    # Layout rectangle for text
    $textRect = New-Object System.Drawing.RectangleF(0, 465, $width, 95)
    $sf = New-Object System.Drawing.StringFormat
    $sf.Alignment = [System.Drawing.StringAlignment]::Center
    $sf.LineAlignment = [System.Drawing.StringAlignment]::Center
    
    $g1.DrawString("INVENTORY MANAGEMENT SYSTEM", $font, $brush, $textRect, $sf)
    
    # Clean up g1
    $g1.Dispose()
    $font.Dispose()
    $fontFamily.Dispose()
    $brush.Dispose()

    # 3. Create square canvas (1024x1024) and center the 1024x560 logo inside it
    $squareSize = 1024
    $squareBmp = New-Object System.Drawing.Bitmap($squareSize, $squareSize)
    $g2 = [System.Drawing.Graphics]::FromImage($squareBmp)
    $g2.Clear([System.Drawing.Color]::White)
    
    # Draw the logo centered vertically: Y = (1024 - 560) / 2 = 232
    $yOffset = [int](($squareSize - $extendedHeight) / 2)
    $g2.DrawImage($extendedBmp, 0, $yOffset, $width, $extendedHeight)
    $g2.Dispose()

    # 4. Resize the square image to 256x256 (standard high-res Windows icon size)
    $iconSize = 256
    $finalBmp = New-Object System.Drawing.Bitmap($iconSize, $iconSize)
    $g3 = [System.Drawing.Graphics]::FromImage($finalBmp)
    $g3.Clear([System.Drawing.Color]::White)
    $g3.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $g3.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
    $g3.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    
    $g3.DrawImage($squareBmp, 0, 0, $iconSize, $iconSize)
    $g3.Dispose()

    # Save to a temporary PNG
    $tempPng = Join-Path $resourcesDir "temp_logo.png"
    $finalBmp.Save($tempPng, [System.Drawing.Imaging.ImageFormat]::Png)

    # Clean up bitmaps
    $srcImg.Dispose()
    $extendedBmp.Dispose()
    $squareBmp.Dispose()
    $finalBmp.Dispose()

    # 5. Convert PNG to ICO format
    $pngBytes = [System.IO.File]::ReadAllBytes($tempPng)
    $stream = New-Object System.IO.FileStream($outIcoPath, [System.IO.FileMode]::Create)
    
    # ICO Header (6 bytes)
    $stream.WriteByte(0); $stream.WriteByte(0) # Reserved
    $stream.WriteByte(1); $stream.WriteByte(0) # Type (1 = Icon)
    $stream.WriteByte(1); $stream.WriteByte(0) # Count (1 image)
    
    # Directory Entry (16 bytes)
    $stream.WriteByte(0) # Width (256 -> 0)
    $stream.WriteByte(0) # Height (256 -> 0)
    $stream.WriteByte(0) # Color count (0 for >=8bpp)
    $stream.WriteByte(0) # Reserved
    $stream.WriteByte(1); $stream.WriteByte(0) # Color planes (1)
    $stream.WriteByte(32); $stream.WriteByte(0) # Bits per pixel (32)
    
    # Size of PNG data (4 bytes)
    $size = $pngBytes.Length
    $stream.WriteByte([byte]($size -band 0xFF))
    $stream.WriteByte([byte](($size -shr 8) -band 0xFF))
    $stream.WriteByte([byte](($size -shr 16) -band 0xFF))
    $stream.WriteByte([byte](($size -shr 24) -band 0xFF))
    
    # Offset of PNG data (4 bytes, 22 bytes from start)
    $stream.WriteByte(22); $stream.WriteByte(0); $stream.WriteByte(0); $stream.WriteByte(0)
    
    # Write PNG bytes
    $stream.Write($pngBytes, 0, $pngBytes.Length)
    $stream.Close()

    # Remove temporary PNG
    Remove-Item $tempPng -Force
    
    Write-Host "Success: Generated icon at $outIcoPath"
} catch {
    Write-Error "Failed to generate icon: $_"
}
