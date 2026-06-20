using System.Windows;

namespace Masicalan.VaultVfs.GuiSample
{
    /// <summary>
    /// TextViewerWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class TextViewerWindow : Window
    {
        private string VaultFilePath { get; set; }

        private string ScriptEntryPath { get; set; }

        private string VaultEntropyName { get; set; }

        public TextViewerWindow(
            string vaultFilePath,
            string vaultEntropyName,
            string scriptEntryPath)
        {
            InitializeComponent();

            this.VaultFilePath = vaultFilePath;
            this.VaultEntropyName = vaultEntropyName;
            this.ScriptEntryPath = scriptEntryPath;

            try
            {
                MainTextBox.Text = VfsIO.Read(vaultFilePath, vaultEntropyName, scriptEntryPath);
            }
            catch (Exception ex)
            {
                MainTextBox.Text = $"Error\n{ex.Message}";
            }
        }

        private void SaveButtonOnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                VfsIO.Edit(this.VaultFilePath, this.VaultEntropyName, this.ScriptEntryPath, MainTextBox.Text);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
