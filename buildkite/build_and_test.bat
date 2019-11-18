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

REM OpenCppCoverage.exe --modules cpvc-core-test --export_type html:cpvc-core-coverage --sources cpvc "x64\Debug\cpvc-core-test.exe"
REM "C:\Program Files\WinRAR\Rar.exe" a -ep1 -r -y "cpvc-core-coverage.rar" ".\cpvc-core-coverage\"
REM OpenCppCoverage.exe --modules cpvc-core-test --export_type cobertura:cpvc-core-coverage.xaml --sources cpvc "x64\Debug\cpvc-core-test.exe"

"C:\Tools\OpenCover\OpenCover.Console.exe" -target:"c:\Program Files (x86)\NUnit.org\nunit-console\nunit3-console.exe" -targetargs:"cpvc-test\bin\x64\Debug\cpvc-test.dll" -filter:"+[cpvc]* +[cpvc-core-clr]* -[cpvc-test]*" -register:user -output:cpvc-coverage.xml

".\packages\ReportGenerator.4.3.6\tools\net47\ReportGenerator.exe" -targetdir:report -reporttypes:HtmlInline -sourcedirs:. -reports:cpvc-coverage.xml
REM ".\packages\ReportGenerator.4.3.6\tools\net47\ReportGenerator.exe" -targetdir:report -reporttypes:MHtml -sourcedirs:D:\GitHub\cpvc -reports:cpvc-core-coverage.xml
