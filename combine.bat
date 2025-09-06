@echo off
echo Used if the scan failed and the app names an output file unknown...pdf
echo.
echo Manually rename the file to the judging or entry number, add a "-1.pdf" or 
echo "-2.pdf" etc. if needed.  Then run this command and a merged directory will 
echo be created with all files, even the ones which were good in the first place.  
echo Upload the files in the merged directory.  In the following example, it is 
echo assumed that the files with corrections are in the Out directory.
echo.
echo e.g. QRLabeler.exe -c -d Wortapalooza_Entries_Paid_All_2025-09-06.csv -o Out
echo.
QRLabeler.exe -c -d %1 -o Out
