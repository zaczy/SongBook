
param(
    [Parameter(Mandatory=$true)]
    [string]$pwd
)


$projFile = Get-ChildItem -Path . -Filter *.csproj -Recurse | Select-Object -First 1

Write-Host "$pwd" -ForegroundColor Green

if (-not $projFile) {

    throw "❌ Nie znaleziono pliku .csproj – przerwano działanie skryptu."
}


$proj = $projFile.FullName
#$proj = "C:\Users\Rafal.Zak\source\repos\Zaczy.SongBook\Zaczy.SongBook.MAUI\Zaczy.SongBook.MAUI.csproj"
$out  = "\\qnas\homes\zaczy\Android\"


dotnet publish $proj -f net9.0-android -c Release `
  -p:AndroidPackageFormat=apk `
  -p:AndroidKeyStore=true `
  -p:AndroidSigningKeyStore="C:\Users\Rafal.Zak\AppData\Local\Xamarin\Mono for Android\Keystore\zaczy\zaczy.keystore" `
  -p:AndroidSigningKeyAlias=zaczy `
  -p:AndroidSigningKeyPass=$pwd `
  -p:AndroidSigningStorePass=$pwd `
  -o $out


# 1) Pobierz wartość ApplicationDisplayVersion z MSBuild
$version = dotnet msbuild $proj -nologo -v:quiet `
  -p:Configuration=Debug `
  -p:TargetFramework=net9.0-android `
  -getProperty:ApplicationDisplayVersion

Write-Host "Odczytana wersja plikacji: $version"

# 2) Znajdź APK (załóżmy że jest tylko jedno .apk w katalogu publikacji)
$apk = Get-ChildItem -Path $out -Filter *Signed.apk -Recurse | Select-Object -First 1


# 3) Zmień nazwę
if ($apk -and $version) {
    $newName = Join-Path $apk.DirectoryName ("zaczy.songbook.maui-" + $version + ".apk")

    try {
        if (Test-Path -LiteralPath $newName) {
            Write-Host "Istnieje już: $newName — usuwam..." -ForegroundColor Yellow
            # Remove-Item z -Force i -ErrorAction Stop, żeby przerwać skrypt gdy nie uda się usunąć
            Remove-Item -LiteralPath $newName -Force -ErrorAction Stop
        }

        Rename-Item -LiteralPath $apk.FullName -NewName $newName -Force -ErrorAction Stop
        Write-Host "APK => $newName" -ForegroundColor Green

        $newName = Join-Path $apk.DirectoryName ("zaczy.songbook.maui.apk")
        Remove-Item -LiteralPath $newName -Force -ErrorAction Stop
        Write-Host "Usuwam $newName" -ForegroundColor Yellow

    }
    catch {
        Write-Error "Nie udało się przygotować pliku wynikowego: $newName. Szczegóły: $($_.Exception.Message)"
        throw
    }

} else {
    Write-Warning "Nie znaleziono APK lub wersji."
}
