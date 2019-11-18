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

REM Run unit tests.
"C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\Common7\IDE\CommonExtensions\Microsoft\TestWindow\vstest.console.exe" /platform:x64 cpvc-test\bin\x64\Release\cpvc-test.dll
x64\Release\cpvc-core-test.exe

REM Generate code coverage files for cpvc-core (C++) and cpvc (C#).
"C:\Program Files\OpenCppCoverage\OpenCppCoverage.exe" --modules cpvc-core-test --sources=cpvc-core --excluded_sources=cpvc-core-test --export_type cobertura:cpvc-core-coverage.xml "x64\Debug\cpvc-core-test.exe"
"C:\Tools\OpenCover\OpenCover.Console.exe" -target:"c:\Program Files (x86)\NUnit.org\nunit-console\nunit3-console.exe" -targetargs:"cpvc-test\bin\x64\Debug\cpvc-test.dll" -filter:"+[cpvc]* +[cpvc-core-clr]* -[cpvc-test]*" -excludebyfile:"d:\agent\*";"c:\program files*";"*App.g.cs" -hideskipped:All -register:user -output:cpvc-coverage.xml

REM Generate ReportGenerator reports from the original Cobertura files.
".\packages\ReportGenerator.4.3.6\tools\net47\ReportGenerator.exe" -targetdir:coverage-report-xml -reporttypes:Xml -sourcedirs:. -reports:cpvc-coverage.xml;cpvc-core-coverage.xml

REM Get rid of this file as it seems to cause duplicates in Coveralls.
del coverage-report-xml\Summary.xml

REM Before uploading to Coveralls, correct the casing of all the filenames. The XML reports all have lowercased filenames, due to
REM the way they're stored in the PDB files. This results in Coveralls not being able to show the source code. Note that this only
REM seems to happen with C++ code, not C# code.
dir /b /s /a:-D .\cpvc-core\*.cpp .\cpvc-core\*.h > tokens.txt
dir /b /a:-D .\cpvc-core\*.cpp .\cpvc-core\*.h >> tokens.txt
dir /b /s /a:-D coverage-report-xml\*.xml > files.txt
"C:\Tools\CorrectCase\CorrectCase.exe" files.txt tokens.txt

REM Upload to Coveralls!
"C:\Tools\Coveralls.NET\csmacnz.Coveralls.exe" -i coverage-report-xml --reportgenerator --useRelativePaths --jobId=%BUILDKITE_JOB_ID%
