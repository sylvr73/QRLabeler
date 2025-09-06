using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kentor.LabelGenerator.Models
{
    public class LabelSettings_Avery_5960 : LabelSettings
    {
        public LabelSettings_Avery_5960()
        {
            columnsPerPage = 3;
            rowsPerPage = 10;
            pageWidth = 216;
            pageHeight = 280; 
            labelBaseWidth = 67; // <- play with this to get horizonal alignment better
            labelBaseHeight = 25.5; //26 <- play with this to get vertial alignment better
            labelPaddingLeft = 0;
            labelPaddingTop = 1;
            labelPaddingRight = 0;
            labelPaddingBottom = 0;
            labelMarginTop = 13; // 6, 11 <- play with this to get vertial alignment better
            labelMarginLeft = 2; // <- play with this to get horizonal alignment better
            labelPositionX = 70.5;
            labelPositionY = 26;
            fontSize = 8;
            fontFamily = "Arial";
            maxCharactersPerRow = 45;
        }
    }
}
