param(
    [Parameter(Mandatory = $true)]
    [string] $ImagePath
)

$ErrorActionPreference = 'Stop'

Add-Type -AssemblyName System.Runtime.WindowsRuntime
$null = [Windows.Storage.StorageFile, Windows.Storage, ContentType = WindowsRuntime]
$null = [Windows.Storage.FileAccessMode, Windows.Storage, ContentType = WindowsRuntime]
$null = [Windows.Storage.Streams.IRandomAccessStream, Windows.Storage.Streams, ContentType = WindowsRuntime]
$null = [Windows.Graphics.Imaging.BitmapDecoder, Windows.Graphics.Imaging, ContentType = WindowsRuntime]
$null = [Windows.Graphics.Imaging.SoftwareBitmap, Windows.Graphics.Imaging, ContentType = WindowsRuntime]
$null = [Windows.Media.Ocr.OcrEngine, Windows.Foundation, ContentType = WindowsRuntime]
$null = [Windows.Media.Ocr.OcrResult, Windows.Foundation, ContentType = WindowsRuntime]

function Await-Operation {
    param(
        [Parameter(Mandatory = $true)]
        [object] $Operation,

        [Parameter(Mandatory = $true)]
        [type] $ResultType
    )

    $method = [System.WindowsRuntimeSystemExtensions].GetMethods() |
        Where-Object {
            $_.Name -eq 'AsTask' -and
            $_.IsGenericMethodDefinition -and
            $_.GetParameters().Count -eq 1
        } |
        Select-Object -First 1

    $task = $method.MakeGenericMethod($ResultType).Invoke($null, @($Operation))
    $task.Wait()
    return $task.Result
}

$engine = [Windows.Media.Ocr.OcrEngine]::TryCreateFromUserProfileLanguages()
if ($null -eq $engine) {
    throw 'OCR do Windows nao encontrou idiomas instalados.'
}

$file = Await-Operation ([Windows.Storage.StorageFile]::GetFileFromPathAsync($ImagePath)) ([Windows.Storage.StorageFile])
$stream = Await-Operation ($file.OpenAsync([Windows.Storage.FileAccessMode]::Read)) ([Windows.Storage.Streams.IRandomAccessStream])
$decoder = Await-Operation ([Windows.Graphics.Imaging.BitmapDecoder]::CreateAsync($stream)) ([Windows.Graphics.Imaging.BitmapDecoder])
$bitmap = Await-Operation ($decoder.GetSoftwareBitmapAsync()) ([Windows.Graphics.Imaging.SoftwareBitmap])
$result = Await-Operation ($engine.RecognizeAsync($bitmap)) ([Windows.Media.Ocr.OcrResult])

$items = @()
$lineIndex = 0
foreach ($line in $result.Lines) {
    if (-not [string]::IsNullOrWhiteSpace($line.Text)) {
        $words = @($line.Words)
        if ($words.Count -gt 0) {
            $left = ($words | ForEach-Object { $_.BoundingRect.X } | Measure-Object -Minimum).Minimum
            $top = ($words | ForEach-Object { $_.BoundingRect.Y } | Measure-Object -Minimum).Minimum
            $right = ($words | ForEach-Object { $_.BoundingRect.X + $_.BoundingRect.Width } | Measure-Object -Maximum).Maximum
            $bottom = ($words | ForEach-Object { $_.BoundingRect.Y + $_.BoundingRect.Height } | Measure-Object -Maximum).Maximum

            $items += [pscustomobject]@{
                kind = 'line'
                text = $line.Text
                x = [double]$left
                y = [double]$top
                width = [double]($right - $left)
                height = [double]($bottom - $top)
                line = [int]$lineIndex
            }

            $wordIndex = 0
            foreach ($word in $words) {
                if (-not [string]::IsNullOrWhiteSpace($word.Text)) {
                    $items += [pscustomobject]@{
                        kind = 'word'
                        text = $word.Text
                        x = [double]$word.BoundingRect.X
                        y = [double]$word.BoundingRect.Y
                        width = [double]$word.BoundingRect.Width
                        height = [double]$word.BoundingRect.Height
                        line = [int]$lineIndex
                        word = [int]$wordIndex
                    }
                }
                $wordIndex++
            }
        }
    }
    $lineIndex++
}

ConvertTo-Json -InputObject @($items) -Compress
