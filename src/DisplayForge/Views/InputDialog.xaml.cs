using System.Windows;
using DisplayForge.Resources;

namespace DisplayForge.Views;

public partial class InputDialog : Window
{
    public string ResultText { get; private set; } = string.Empty;

    public InputDialog(string title, string prompt, string initialValue)
    {
        InitializeComponent();
        Title = title;
        PromptText.Text = prompt;
        BtnCancel.Content = Strings.Cancel;
        InputBox.Text = initialValue;
        InputBox.SelectAll();
        Loaded += (_, _) => InputBox.Focus();
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        ResultText = InputBox.Text;
        DialogResult = true;
    }
}
