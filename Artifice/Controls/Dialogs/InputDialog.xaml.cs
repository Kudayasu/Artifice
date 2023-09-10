using HandyControl.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
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
using System.Windows.Shapes;

namespace Artifice.Controls.Dialogs
{
    /// <summary>
    /// Interaction logic for InputDialog.xaml
    /// </summary>
    public partial class InputDialog : GlowWindow
    {
        public InputDialog(string title, string message)
        {
            InitializeComponent();
            Title = title;
            lblMessage.Text = message;
        }

        public string Result { get; private set; }

        private void enterHandler(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Return) btnContinue_Click(sender, e);
        }

        private void ValidateInput()
        {
            Result = txtInput.Text;
            Close();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        public void btnContinue_Click(object sender, RoutedEventArgs e)
        {
            ValidateInput();
        }
    }
}
