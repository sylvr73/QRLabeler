using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kentor.LabelGenerator.Models
{
    public class LabelSettings_Avery_8167 : LabelSettings
    {
        public LabelSettings_Avery_8167()
        {
            columnsPerPage = 4;
            rowsPerPage = 20;
            pageWidth = 216;
            pageHeight = 280; 
            labelBaseWidth = 44;
            labelBaseHeight = 13; // think this could be more like 12.75 next time and perhaps change margin top to 10
            labelPaddingLeft = 2;
            labelPaddingTop = 6;
            labelPaddingRight = 2;
            labelPaddingBottom = 2;
            labelMarginTop = 8; 
            labelMarginLeft = 0;
            labelPositionX = 54;
            labelPositionY = 0; 
            fontSize = 10;
            fontFamily = "Arial";
            maxCharactersPerRow = 45;
        }
    }
}
