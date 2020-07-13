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

            //try
            //{
            //    var resourceDirectory = "C:\\Workspace\\AML\\ERI\\ERI-Web\\ERI\\Resources";

            //    var exporter = new ResourceExporter(
            //        resourceDirectory
            //        , new List<ResourceExportLanguage>
            //        {
            //            new ResourceExportLanguage("en-CA", true),
            //            new ResourceExportLanguage("fr-CA", false, false)
            //        }
            //        , true
            //    );

            //    exporter.ExportResourcesToExcel();
            //}
            //catch (Exception ex)
            //{
            //    Console.WriteLine(ex);
            //}

            //return;
            try
            {
                var resourceDirectory = "C:\\Workspace\\AML\\ERI\\ERI-Web\\ERI\\Resources";

                //ResourceGenerator.ResetResourceFiles(resourceDirectory);

                var exporter = new ResourceImporter(
                    resourceDirectory
                    , new List<ResourceImportLanguage>
                    {
                        new ResourceImportLanguage("en-CA", UpdateType.Prompt),
                        new ResourceImportLanguage("fr-CA", UpdateType.Overwrite)
                    }
                    , true
                );
                exporter.ExportConflictResults = true;

                exporter.ImportResourcesFromExcel(
                    "C:\\Users\\Taylor Devereaux\\Downloads\\ERI_Resources_Export_2020-06-19-04-30-55.xlsx",
                    (string existingText, string newText) =>
                    {
                        Console.WriteLine(existingText);
                        Console.WriteLine(
                            newText);

                        string key = null;
                        do
                        {
                            Console.WriteLine("Resolve Conflict (1) Existing, (2) New: ");
                            key = Console.ReadLine();
                            if (key != "1" && key != "2")
                            {
                                Console.WriteLine("Invalid Entry!");
                                key = null;
                            }
                        }
                        while (key == null);

                        return key == "1" ? existingText : newText;
                    }
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

        }
    }
}
