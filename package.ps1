.\build.ps1
Remove-Item ".\Dist\*" -Recurse -Force
~\AppData\Local\Playnite\Toolbox.exe pack ".\Extension\bin\Debug\" ".\Dist"
