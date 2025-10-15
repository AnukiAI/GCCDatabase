@echo off
echo Fixing build issues by cleaning problematic folders...

REM Map to shorter path
subst X: "%CD%"

REM Delete using shorter path
if exist X:\publish-accuweb.old (
    echo Deleting publish-accuweb.old...
    rmdir /s /q X:\publish-accuweb.old
)

if exist X:\obj (
    echo Deleting obj folder...
    rmdir /s /q X:\obj
)

if exist X:\bin (
    echo Deleting bin folder...
    rmdir /s /q X:\bin
)

REM Unmap drive
subst X: /d

echo.
echo Done! Now run: dotnet build
echo.
