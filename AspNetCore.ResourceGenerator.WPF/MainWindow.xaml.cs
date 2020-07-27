using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;


namespace AspNetCore.ResourceGenerator
{
    public class AppCache
    {
        internal const string FILE_NAME = "appcache.json";
        public string ProjectDirectory { get; set; }
        public string ResourceDirectory { get; set; }
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public AppCache Cache { get; set; }



        public MainWindow()
        {
            InitializeComponent();
            Cache = new AppCache();
            LoadSettings();
            RefreshFromCache();

            //var generator = new ResourceGenerator(
            //    projectDirectory
            //    , "Resources"
            //    , new List<ResourceFileLanguage>
            //    {
            //        new ResourceFileLanguage("en-CA", true),
            //        new ResourceFileLanguage("fr-CA", "FR ", null)
            //    }
            //);

            //var exporter = new ResourceExporter(
            //    resourceDirectory
            //    , new List<ResourceExportLanguage>
            //    {
            //        new ResourceExportLanguage("en-CA", true),
            //        new ResourceExportLanguage("fr-CA", false, false)
            //    }
            //    , true
            //);

            //var exporter = new ResourceImporter(
            //    resourceDirectory
            //    , new List<ResourceImportLanguage>
            //    {
            //        new ResourceImportLanguage("en-CA", UpdateType.Prompt),
            //        new ResourceImportLanguage("fr-CA", UpdateType.Overwrite)
            //    }
            //    , true
            //);
            //exporter.ExportConflictResults = true;
        }

        private void RefreshFromCache()
        {
            ProjectDirectory_TextBox.Text = Cache.ProjectDirectory;
            ResourceDirectory_TextBox.Text = Cache.ResourceDirectory;
        }

        private void ProjectDirectory_TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                if (!String.IsNullOrWhiteSpace(ProjectDirectory_TextBox.Text))
                {
                    dialog.SelectedPath = ProjectDirectory_TextBox.Text;
                }

                DialogResult result = dialog.ShowDialog();

                if (result == System.Windows.Forms.DialogResult.OK && !string.IsNullOrWhiteSpace(dialog.SelectedPath))
                {
                    Cache.ProjectDirectory =
                    ProjectDirectory_TextBox.Text = dialog.SelectedPath;

                    var directories = Directory.GetDirectories(dialog.SelectedPath);

                    var resourceDirectory = directories.SingleOrDefault(x => x.EndsWith("\\Resources"));
                    if (resourceDirectory != null)
                    {
                        Cache.ResourceDirectory =
                        ResourceDirectory_TextBox.Text = resourceDirectory;
                    }
                }
            }
        }

        private void NormalizeResources_Button_Click(object sender, RoutedEventArgs e)
        {
            ResourceGenerator.Normalize
        }

        private void LoadSettings()
        {
            try
            {

                string filePath = System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, AppCache.FILE_NAME);
                //if (!File.Exists(filePath))
                //{
                //    File.Create(filePath);
                //}

                using (StreamReader reader = new StreamReader(File.Open(filePath, FileMode.OpenOrCreate)))
                {
                    string settings = reader.ReadToEnd();

                    if (!String.IsNullOrEmpty(settings))
                    {
                        Cache = JsonConvert.DeserializeObject<AppCache>(settings);
                    }
                }
            }
            catch (Exception ex)
            {
            }
        }
        private void SaveSettings()
        {
            try
            {

                string filePath = System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, AppCache.FILE_NAME);
                if (!File.Exists(filePath))
                {
                    File.Create(filePath);
                }

                using (StreamWriter writer = new StreamWriter(File.Open(filePath, FileMode.Truncate)))
                {
                    string settings = JsonConvert.SerializeObject(Cache);
                    if (!String.IsNullOrEmpty(settings))
                    {
                        writer.Write(settings);
                    }
                }
            }
            catch (Exception ex)
            {
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            SaveSettings();
        }

    }
}
