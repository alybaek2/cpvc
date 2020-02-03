%BUILD_NUGET% restore

REM Generate dummy rom files.
del roms\os6128.rom
fsutil file createnew roms\os6128.rom 16384
del roms\amsdos6128.rom
fsutil file createnew roms\amsdos6128.rom 16384
del roms\basic6128.rom
fsutil file createnew roms\basic6128.rom 16384

REM Build Debug and Release (x64).
%BUILD_MSBUILD% cpvc.sln /t:Rebuild /p:Configuration=Release /p:Platform="x64"
if %errorlevel% neq 0 exit /b %errorlevel%

%BUILD_MSBUILD% cpvc.sln /t:Rebuild /p:Configuration=Debug /p:Platform="x64"
if %errorlevel% neq 0 exit /b %errorlevel%

call .\buildkite\backup.bat cpvc-test\bin
call .\buildkite\backup.bat x64
