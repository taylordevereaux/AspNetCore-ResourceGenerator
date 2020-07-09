using System;
using System.Collections.Generic;
using System.IO;

namespace AspNetCore.ResourceGenerator.Cli
{
    class Program
    {
        static void Main(string[] args)
        {
            //var projectDirectory = "C:\\Workspace\\AML\\ERI\\ERI-Web\\ERI";

            //var generator = new ResourceGenerator(
            //    projectDirectory
            //    , "Resources"
            //    , new List<ResourceFileLanguage>
            //    {
            //        new ResourceFileLanguage("en-CA", true),
            //        new ResourceFileLanguage("fr-CA", "FR ", null)
            //    }
            //);

            //try
            //{
            //    var viewData = generator.ParseViews();

            //    var controllerData = generator.ParseControllers();

            //    var modelData = generator.ParseModels();

            //    generator.GenerateResourceFiles(viewData);
            //    generator.GenerateResourceFiles(controllerData);
            //    generator.GenerateResourceFiles(modelData);
            //}
            //catch (Exception ex)
            //{
            //    Console.WriteLine(ex);
            //}

            try
            {
                var resourceDirectory = "C:\\Workspace\\AML\\ERI\\ERI-Web\\ERI\\Resources";

                var exporter = new ResourceExporter(
                    resourceDirectory
                    , new List<ResourceExportLanguage>
                    {
                        new ResourceExportLanguage("en-CA", true),
                        new ResourceExportLanguage("fr-CA", false, false)
                    }
                    , true
                );

                exporter.ExportResourcesToExcel();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

        }
    }
}
