$msbuild = "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe"
& $msbuild ".\Extension\PlayniteInsightsExporter.csproj" /p:Configuration=Debug
