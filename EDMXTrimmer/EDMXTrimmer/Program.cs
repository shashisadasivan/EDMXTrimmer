using CommandLine;
using System;
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
            HelpText = "Enter the public name & collection name. All values to be sepearted with commas",
            Separator = ',')]
        public IEnumerable<string> EntitiesToKeep { get; set; }

        [Option(
            Required = false,
            HelpText = "Verbose information",
            Default = false)]
        public bool verbose { get; set; }

        //[Option(
        //    Required = false,
        //    HelpText = "If trimming then set name of file, otherwise will create default in executing directory")]
        //public string OutputFileName { get; set; }
    }
    class Program
    {
        static void Main(string[] args)
        {
            /*
            string edmxFile = @"C:\temp\testedmx.edmx";
            
            EdmxTrimmer trimmer = new EdmxTrimmer(edmxFile);
            //trimmer.Run();

            List<string> entitiesToKeep = new List<string>() { "CustomersV3", "CustomerV3" };
            trimmer.Trim(entitiesToKeep);
            */
            
            Options opt = new Options();
            CommandLine.Parser.Default.ParseArguments<Options>(args)
                .WithParsed<Options>(opts => opt = opts);

            EdmxTrimmer trimmer = new EdmxTrimmer(opt.EdmxFile, opt.verbose);
            if (opt.EntitiesToKeep.Count() > 0)
            {
                //TODO: use the output file name
                //trimmer.Trim(opt.EntitiesToKeep.ToList());
                trimmer.AnalyzeAndTrim(opt.EntitiesToKeep.ToList());
            }
            else
            {
                trimmer.Run();
            }
            
        }
    }
}
