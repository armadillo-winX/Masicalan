using Masicalan.VaultVfs;
using Microsoft.Win32;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;

namespace Masicalan.VaultVfs.GuiSample
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string _entropyName = "Masicalan.VaultVFS.GraphicalManager";

        public MainWindow()
        {
            InitializeComponent();
        }

        private void CreateNewButtonOnClick(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new()
            {
                Filter = "Masicalan Vault VFS Archive| *.masiv"
            };
            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    VfsManager.Create(
                        saveFileDialog.FileName, this._entropyName
                        );
                    PathBox.Text = saveFileDialog.FileName;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        this, ex.Message, "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void OpenButtonOnClick(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new()
            {
                Filter = "Masicalan Vault VFS Archive| *.masiv"
            };
            if (openFileDialog.ShowDialog() == true)
            {
                PathBox.Text = openFileDialog.FileName;
            }
        }

        private void AddScriptButtonOnClick(object sender, RoutedEventArgs e)
        {
            if (
                !string.IsNullOrWhiteSpace(PathBox.Text)
                && File.Exists(PathBox.Text))
            {
                try
                {
                    OpenFileDialog openFileDialog = new()
                    {
                        Filter = "Masicalan スクリプトファイル|*.masis"
                    };
                    if (openFileDialog.ShowDialog() == true)
                    {
                        string scriptFile = openFileDialog.FileName;
                        string script = File.ReadAllText(scriptFile);
                        VfsIO.Add(
                            PathBox.Text, this._entropyName, "scripts/", Path.GetFileName(scriptFile), script, VfsAttribute.Executable
                            );

                        string[] files = VfsIO.GetScriptFiles(PathBox.Text, this._entropyName);
                        foreach (string file in files)
                        {
                            ScriptFilesListBox.Items.Add(file);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, ex.Message, "エラー",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}