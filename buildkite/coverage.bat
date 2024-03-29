call .\buildkite\restore.bat cpvc-test\bin

REM Generate code coverage files for cpvc (C#).
%BUILD_NUGET% install OpenCover -Version 4.7.922
%BUILD_NUGET% install NUnit.ConsoleRunner -Version 3.11.1
".\OpenCover.4.7.922\tools\OpenCover.Console.exe" -target:".\NUnit.ConsoleRunner.3.11.1\tools\nunit3-console.exe" -targetargs:"cpvc-test\bin\x64\Debug\cpvc-test.dll" -filter:"+[cpvc]* +[cpvc-core-clr]* -[cpvc-test]*" -excludebyfile:"d:\agent\*";"c:\program files*";"*App.g.cs";"*.xaml.cs";"*\obj\*";"*.xaml";"*.Designer.cs";"*Socket.cs";"*Display.cs";"*MachineBitmap.cs" -hideskipped:All -threshold:100 -register:user -output:cpvc-coverage.xml

REM Generate ReportGenerator reports from the original Cobertura files.
%BUILD_NUGET% install ReportGenerator -Version 4.3.6
".\ReportGenerator.4.3.6\tools\net47\ReportGenerator.exe" -targetdir:coverage-report-xml -reporttypes:Xml -sourcedirs:. -reports:cpvc-coverage.xml
".\ReportGenerator.4.3.6\tools\net47\ReportGenerator.exe" -targetdir:. -reporttypes:Cobertura -sourcedirs:. -reports:cpvc-coverage.xml

REM Get rid of this file as it seems to cause duplicates in Coveralls.
del coverage-report-xml\Summary.xml

REM Get the commit message.
git show -s --format=%%B %BUILDKITE_COMMIT% > commitmsg.txt
set /P COMMIT_MESSAGE=< commitmsg.txt
REM Escape the double quotes.
set COMMIT_MESSAGE=%COMMIT_MESSAGE:"=\"%

REM Upload to Coveralls!
%BUILD_COVERALLS% -i coverage-report-xml --reportgenerator --useRelativePaths --serviceName buildkite --serviceNumber %BUILDKITE_BUILD_NUMBER% --commitMessage "%COMMIT_MESSAGE%" --commitAuthor=%BUILDKITE_BUILD_CREATOR% --commitId %BUILDKITE_COMMIT% --commitBranch %BUILDKITE_BRANCH% --commitEmail %BUILDKITE_BUILD_CREATOR_EMAIL% --jobId=%BUILDKITE_JOB_ID% --repoToken=%CPVC_COVERALLS_REPO_TOKEN%

REM Upload to Codecov!
REM %BUILD_NUGET% install Codecov -Version 1.12.0
REM ".\Codecov.1.12.0\tools\win7-x86\codecov.exe" --branch %BUILDKITE_BRANCH% --build %BUILDKITE_BUILD_NUMBER% --sha %BUILDKITE_COMMIT% --file "Cobertura.xml" --token %CPVC_CODECOV_TOKEN% --required --verbose
