using CommandLine;
using System.Collections.Generic;
using System.Linq;

namespace EDMXTrimmer
{
    public class Options
    {
        [Option(
            Required = true,
            HelpText = "EDMX source file")]
        public string EdmxFile { get; set; }

        [Option(
            Required = false,
            HelpText = "Enter the public name & collection name. All values to be seperated with commas. Supports ? and * wildcards.",
            Separator = ',')]
        public IEnumerable<string> EntitiesToKeep { get; set; }

        [Option(
            Required = false,
            HelpText = "Enter the public name & collection name. All values to be seperated with commas. Supports ? and * wildcards.",
            Separator = ',')]
        public IEnumerable<string> EntitiesToExclude { get; set; }

        [Option(
            Required = false,
            HelpText = "Verbose information",
            Default = false)]
        public bool Verbose { get; set; }

        [Option( 
            Required = false, 
            Default = "Output.edmx",
            HelpText = "Set name of file otherwise will be set to Output.EDMX")]
        public string OutputFileName { get; set; }

        [Option(
            Required = false,
            HelpText = "Entities to keep or exclude are interpreted as regular expressions",
            Default = false)]
        public bool EntitiesAreRegularExpressions { get; set; }

    }
    class Program
    {
        static void Main(string[] args)
        {
            Options opt = new Options();
            CommandLine.Parser.Default.ParseArguments<Options>(args)
                .WithParsed<Options>(opts => opt = opts)
                .WithNotParsed<Options>((errs) => { Environment.Exit(160); }); // Exit code 160 is used to indicate that a command line argument was not valid.

            EdmxTrimmer trimmer = new EdmxTrimmer(
                opt.EdmxFile, 
                opt.OutputFileName, 
                verbose:opt.Verbose, 
                entitiesToKeep:opt.EntitiesToKeep.ToList(), 
                entitiesToExclude:opt.EntitiesToExclude.ToList(),
                entitiesAreRegularExpressions:opt.EntitiesAreRegularExpressions);
            
            trimmer.AnalyzeFile();
        }
    }
}
