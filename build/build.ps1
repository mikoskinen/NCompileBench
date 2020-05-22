echo dotnet tool update -g minver-cli
echo dotnet tool update -g dotnet-property
$curVersion = minver -m 1.0 -t v
$suffix = Get-Date -format "yyMMddHHmmss"

dotnet-property ..\src\NCompileBench\NCompileBench.csproj Version:"$curVersion"

dotnet pack ..\src\NCompileBench\NCompileBench.csproj -c Release

mkdir packages -Force
move ..\src\NCompileBench\nupkg\*.nupkg packages -Force