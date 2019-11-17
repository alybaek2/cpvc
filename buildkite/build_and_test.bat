"C:\Tools\NuGet\nuget.exe" restore

del roms\os6128.rom
fsutil file createnew roms\os6128.rom 16384
del roms\amsdos6128.rom
fsutil file createnew roms\amsdos6128.rom 16384
del roms\basic6128.rom
fsutil file createnew roms\basic6128.rom 16384

"C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe" cpvc.sln /t:Rebuild /p:Configuration=Release /p:Platform="x64"
"C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe" cpvc.sln /t:Rebuild /p:Configuration=Debug /p:Platform="x64"

"C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\Common7\IDE\CommonExtensions\Microsoft\TestWindow\vstest.console.exe" /platform:x64 cpvc-test\bin\x64\Release\cpvc-test.dll
x64\Release\cpvc-core-test.exe

OpenCppCoverage.exe --modules cpvc-core-test --export_type cobertura --sources cpvc "x64\Debug\cpvc-core-test.exe"
