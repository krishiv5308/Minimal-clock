param([switch]$Test)
$src='work\Clock.cs','work\Faces.cs','work\Gallery.cs','work\TextFaces.cs','work\NewFaces.cs','work\LayeredClock.cs','work\ThemeSettings.cs','work\StyleEditor.cs','work\OrbitUI.cs'
$csc='C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
& $csc /nologo /target:winexe /platform:x86 /optimize+ /codepage:65001 /out:work\MinimalDateClock.exe '/win32icon:outputs\Orbit Clock.ico' /win32manifest:work\clock.manifest /reference:System.Windows.Forms.dll /reference:System.Drawing.dll $src
if($LASTEXITCODE -ne 0){exit $LASTEXITCODE}
if($Test){
    & $csc /nologo /target:exe /main:CustomCheck '/win32icon:outputs\Orbit Clock.ico' /platform:x86 /optimize+ /codepage:65001 /out:work\CustomCheck.exe /win32manifest:work\clock.manifest /reference:System.Windows.Forms.dll /reference:System.Drawing.dll $src work\CustomCheck.cs
    if($LASTEXITCODE -eq 0){ & .\work\CustomCheck.exe }
}
exit $LASTEXITCODE


