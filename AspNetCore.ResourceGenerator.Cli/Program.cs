using System;
using System.Collections.Generic;
using System.IO;

namespace AspNetCore.ResourceGenerator.Cli
{
    class Program
    {
        static void Main(string[] args)
        {
            var projectDirectory = "C:\\Workspace\\AML\\ERI\\ERI-Web\\ERI";

            var generator = new ResourceGenerator(projectDirectory, "Resources", new List<string> { "en-CA", "fr-CA" }, false);

            try
            {
                var viewData = generator.ParseViews();

                generator.GenerateResourceFiles(viewData);
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
    }
}
