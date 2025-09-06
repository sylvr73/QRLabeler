using CommandLine;
using System.Windows.Markup;

namespace QRLabeler
{
    public class CommandLineOptions
    {
        [Option('?', "help", Required = false, HelpText = "Get help!")]
        public bool Help { get; set; } = false;

        [Option('v', "verbose", Required = false, HelpText = "Set output to verbose messages.")]
        public bool Verbose { get; set; } = false;
        [Option('d', "data", Required = false, HelpText = "Specify data export file.")]
        public string DataExportCSV { get; set; }
        [Option('j', "judging", Required = false, HelpText = "Create judging labels.")]
        public bool JudgingLabels { get; set; } = false;
        [Option('c', "combine", Required = false, HelpText = "combine entry and judging.")]
        public bool Combine { get; set; } = false;
        [Option('b', "bottle", Required = false, HelpText = "Create bottle labels.")]
        public bool BottleLabels { get; set; } = false;
        [Option('r', "rename", Required = false, HelpText = "Scan PDF scoresheets in diretory and rename from QR codes.")]
        public string ScoreSheetDirectory { get; set; }
        [Option('o', "outputdir", Required = false, HelpText = "Destination directory for output.")]
        public string OutputDirectory { get; set; } = ".\\";
        [Option('n', "number", Required = false, HelpText = "Number of labels per entry.")]
        public int LabelsPerEntry { get; set; } = 1;
        [Option('f', "outputfile", Required = false, HelpText = "Name of the output file.")]
        public string OutputFile { get; set; } = "output.pdf";
        [Option('e', "entertoend", Required = false, HelpText = "Have 'Hit enter to end', mostly a debugging thing.")]
        public bool EnterToEnd { get; set; }
        [Option('k', "darken", Required = false, HelpText = "Darken a pdf.")]
        public bool Darken { get; set; } = false;
        [Option('t', "numbertype", Required = false, HelpText = "Use 'j' for judging, 'e' for entry numbers.")]
        public string NumberType{ get; set; } = null;
    }
}
