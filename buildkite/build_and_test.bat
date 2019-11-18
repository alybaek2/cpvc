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

"C:\Program Files\OpenCppCoverage\OpenCppCoverage.exe" --modules cpvc-core-test --export_type cobertura:cpvc-core-coverage.xml --sources cpvc "x64\Debug\cpvc-core-test.exe"
"C:\Tools\OpenCover\OpenCover.Console.exe" -target:"c:\Program Files (x86)\NUnit.org\nunit-console\nunit3-console.exe" -targetargs:"cpvc-test\bin\x64\Debug\cpvc-test.dll" -filter:"+[cpvc]* +[cpvc-core-clr]* -[cpvc-test]*" -excludebyfile:"d:\agent\*";"c:\program files*";"*App.g.cs" -hideskipped:All -register:user -output:cpvc-coverage.xml

".\packages\ReportGenerator.4.3.6\tools\net47\ReportGenerator.exe" -targetdir:coverage-report -reporttypes:Html -sourcedirs:. -reports:cpvc-coverage.xml;cpvc-core-coverage.xml

"C:\Program Files\WinRAR\Rar.exe" a -ep1 -r -y "coverage-report.rar" ".\coverage-report\"

"C:\Tools\Coveralls.NET\csmacnz.Coveralls.exe" -i cpvc-coverage.xml --opencover --useRelativePaths --commitAuthor=%BUILDKITE_BUILD_CREATOR% --commitId %BUILDKITE_COMMIT% --commitBranch %BUILDKITE_BRANCH% --commitEmail %BUILDKITE_BUILD_CREATOR_EMAIL% --jobId=%BUILDKITE_JOB_ID%

