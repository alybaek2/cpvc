"C:\Tools\NuGet\nuget.exe" restore

REM Generate dummy rom files.
del roms\os6128.rom
fsutil file createnew roms\os6128.rom 16384
del roms\amsdos6128.rom
fsutil file createnew roms\amsdos6128.rom 16384
del roms\basic6128.rom
fsutil file createnew roms\basic6128.rom 16384

REM Build Debug and Release (x64).
"C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe" cpvc.sln /t:Rebuild /p:Configuration=Release /p:Platform="x64"
"C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe" cpvc.sln /t:Rebuild /p:Configuration=Debug /p:Platform="x64"

call .\buildkite\backup.bat cpvc-test\bin
call .\buildkite\backup.bat x64
