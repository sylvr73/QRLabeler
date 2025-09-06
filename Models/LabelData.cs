using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QRLabeler.Data
{
    public class LabelData
    {
        public string TableName { get; set; }
        public string EntryNumber { get; set; }
        public string JudgingNumber
        {
            get { return JudgingNumberInt.ToString(); }
            set { JudgingNumberInt = int.Parse(value); }
        }
        public string Style { get; set; }
        public string Category { get; set; }
        public string Subcategory { get; set; }
        public string RequiredInfo { get; set; }
        public string Sweetness { get; set; }
        public string Carbonation { get; set; }
        public string Strength { get; set; }
        public int TableNumber { get; set; }
        public int JudgingNumberInt { get; set; }
        public Bitmap QRCode { get; set; }
    }
}
