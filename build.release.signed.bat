@IF [%1] == [] GOTO NOPARAM

SET VERSION=%1
@IF NOT [%2] == [] (
    SET SUFFIX=--version-suffix %2
    SET VERSION=%1-%2
)

@ECHO -- Building solution in release mode
dotnet pack -c Release -p:SignCertificateName="Open Source Developer%%2c Artiom Chilaru" %SUFFIX%
@IF ERRORLEVEL 1 goto ERROR

@ECHO -- Signing the nuget package
nuget sign src\FlexLabs.EntityFrameworkCore.Upsert\bin\Release\FlexLabs.EntityFrameworkCore.Upsert.%VERSION%.nupkg -CertificateSubjectName "Open Source Developer, Artiom Chilaru" -Timestamper http://timestamp.digicert.com
@IF ERRORLEVEL 1 goto ERROR

@EXIT /B %errorlevel%

:NOPARAM
@ECHO -- Version number not passed as an argument --
@EXIT /B 1

:ERROR
@ECHO -- Build FAILED --
@EXIT /B 1
