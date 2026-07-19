param(
    [Parameter(Mandatory=$true)][string]$VideoPath,
    [Parameter(Mandatory=$true)][string]$OutputDir
)

Add-Type -AssemblyName PresentationCore
Add-Type -AssemblyName WindowsBase

if (-not (Test-Path -LiteralPath $OutputDir)) {
    New-Item -ItemType Directory -Path $OutputDir | Out-Null
}

$mediaOpened = $false
$mediaFailed = $false
$errorMessage = $null

$player = New-Object System.Windows.Media.MediaPlayer
$player.ScrubbingEnabled = $true
$player.add_MediaOpened({
    $script:mediaOpened = $true
})
$player.add_MediaFailed({
    param($sender, $args)
    $script:errorMessage = $args.ErrorException.Message
    $script:mediaFailed = $true
})

$player.Open((New-Object System.Uri($VideoPath)))

$deadline = (Get-Date).AddSeconds(20)
while (-not $mediaOpened -and -not $mediaFailed -and (Get-Date) -lt $deadline) {
    [System.Windows.Threading.Dispatcher]::CurrentDispatcher.Invoke(
        [Action]{},
        [System.Windows.Threading.DispatcherPriority]::Background
    )
    Start-Sleep -Milliseconds 100
}

if (-not $mediaOpened) {
    if ($mediaFailed) {
        throw "Media failed to open: $errorMessage"
    }
    throw "Timed out while opening media."
}

$duration = $player.NaturalDuration.TimeSpan.TotalSeconds
$width = [Math]::Max(1, $player.NaturalVideoWidth)
$height = [Math]::Max(1, $player.NaturalVideoHeight)

$frameTimes = @(
    [Math]::Max(0.5, $duration * 0.15),
    [Math]::Max(1.0, $duration * 0.50),
    [Math]::Max(1.5, $duration * 0.85)
)

$saved = @()
$positions = @()
for ($i = 0; $i -lt $frameTimes.Count; $i++) {
    $seconds = [Math]::Min($duration - 0.25, $frameTimes[$i])
    $player.Pause()
    $player.Position = [TimeSpan]::FromSeconds($seconds)
    [System.Windows.Threading.Dispatcher]::CurrentDispatcher.Invoke(
        [Action]{},
        [System.Windows.Threading.DispatcherPriority]::Background
    )
    Start-Sleep -Milliseconds 500
    $player.Play()
    $frameDeadline = (Get-Date).AddSeconds(2)
    while ((Get-Date) -lt $frameDeadline) {
        [System.Windows.Threading.Dispatcher]::CurrentDispatcher.Invoke(
            [Action]{},
            [System.Windows.Threading.DispatcherPriority]::Background
        )
        Start-Sleep -Milliseconds 100
    }
    $player.Pause()
    [System.Windows.Threading.Dispatcher]::CurrentDispatcher.Invoke(
        [Action]{},
        [System.Windows.Threading.DispatcherPriority]::Background
    )

    $positions += [Math]::Round($player.Position.TotalSeconds, 2)
    $drawingVisual = New-Object System.Windows.Media.DrawingVisual
    $drawingContext = $drawingVisual.RenderOpen()
    $rect = New-Object System.Windows.Rect(0, 0, $width, $height)
    $drawingContext.DrawVideo($player, $rect)
    $drawingContext.Close()

    $bitmap = New-Object System.Windows.Media.Imaging.RenderTargetBitmap($width, $height, 96, 96, [System.Windows.Media.PixelFormats]::Pbgra32)
    $bitmap.Render($drawingVisual)

    $encoder = New-Object System.Windows.Media.Imaging.PngBitmapEncoder
    $encoder.Frames.Add([System.Windows.Media.Imaging.BitmapFrame]::Create($bitmap))
    $outPath = Join-Path $OutputDir ("video_frame_{0}.png" -f ($i + 1))
    $stream = [System.IO.File]::Create($outPath)
    try {
        $encoder.Save($stream)
    } finally {
        $stream.Dispose()
    }
    $saved += $outPath
}

$player.Close()

[PSCustomObject]@{
    DurationSeconds = [Math]::Round($duration, 2)
    Width = $width
    Height = $height
    CapturedPositions = $positions
    Frames = $saved
} | ConvertTo-Json -Depth 3
