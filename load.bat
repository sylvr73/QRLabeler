@echo off
echo After scanning the stacks of score sheets, put them in a directory.  In the 
echo following example, that directory is In.  Also pre-create the Out directory.
echo that is where the application will put the split up files.
echo.
echo If the application can't read a QR code, it will name the file "unknown...pdf".
echo In this case, you will rename the the file to the judging or entry number 
echo manually, using a "-1.pdf" or "-2.pdf" etc, if needed.  Then proceed to use the
echo combine mode, shown in combine.bat
echo.
echo e.g. QRLabeler.exe -r In -o Out -d Wortapalooza_Entries_Paid_All_2025-06-14.csv
echo.
QRLabeler.exe -r In -o Out -d %1
