using System.Windows;

namespace Masicalan.VaultVfs.GuiSample
{
    /// <summary>
    /// TextViewerWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class TextViewerWindow : Window
    {
        public TextViewerWindow(string content)
        {
            InitializeComponent();

            MainTextBox.Text = content;
        }
    }
}
