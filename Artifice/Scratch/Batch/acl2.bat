REM Ensure we start with a clean slate
D:\DevelopmentFiles\icacls.exe "D:\DevelopmentFiles\SSH2" /reset /T /C

REM Remove inheritance from the directory and its contents
D:\DevelopmentFiles\icacls.exe "D:\DevelopmentFiles\SSH2" /inheritance:d /T /C

REM Set an acceptable ACL for Sshd
D:\DevelopmentFiles\icacls.exe "D:\DevelopmentFiles\SSH2" /grant:r Administrators:(OI)(CI)F
D:\DevelopmentFiles\icacls.exe "D:\DevelopmentFiles\SSH2" /grant:r SYSTEM:(OI)(CI)F
D:\DevelopmentFiles\icacls.exe "D:\DevelopmentFiles\SSH2" /grant:r "Authenticated Users":(OI)(CI)(RX)

REM Remove Everyone and ALL APPLICATION PACKAGES
D:\DevelopmentFiles\icacls.exe "D:\DevelopmentFiles\SSH2" /remove:g Everyone /t /c
D:\DevelopmentFiles\icacls.exe "D:\DevelopmentFiles\SSH2" /remove:g "ALL APPLICATION PACKAGES" /t /c

REM Modify permissions for Authenticated Users on the files within the root folder (critical)
for /R "D:\DevelopmentFiles\SSH2" %%f in (*) do 
(
    D:\DevelopmentFiles\icacls.exe "%%f" /grant:r Administrators:F
    D:\DevelopmentFiles\icacls.exe "%%f" /grant:r SYSTEM:F
    D:\DevelopmentFiles\icacls.exe "%%f" /grant:r "Authenticated Users":RX
)