@%systemroot%\Microsoft.NET\Framework64\v4.0.30319\msbuild.exe /p:Configuration=Release /t:Rebuild %~dp0src\Elevate.sln


@if not exist "%~dp0Package" (
  @mkdir "%~dp0Package" > NUL
)

@%~dp0Build\nuget.exe pack %~dp0src\Elevate\Elevate.nuspec -Properties Configuration=Release -OutputDirectory %~dp0Package

@pause