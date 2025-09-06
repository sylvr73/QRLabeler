@echo off
echo Making 6 labels, two rows, to provide extras when there are three judges 
echo and a sticker gets ruined, better to have extra than not, sheets are three 
echo per row.  It's good to have one beer per row for cutting up the sheets for 
echo the stewards.
echo.
echo e.g. QRLabeler.exe -j -d Wortapalooza_Entries_Paid_All_2025-06-14.csv -f 061425
echo.
QRLabeler.exe -j -d %1 -f %2_judging.pdf -n 6
echo.
echo Making 4 bottle labels, two bottles, two stickers each
echo.
echo e.g. QRLabeler.exe -b -d Wortapalooza_Entries_Paid_All_2025-06-14.csv -f 061425
echo.
QRLabeler.exe -b -d %1 -f %2_bottles.pdf -n 4
