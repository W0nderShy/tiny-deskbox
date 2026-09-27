using System.Windows;

namespace DeskBox.App.Views;

public partial class RenameDialog : Window
{
    public string BoxName => NameBox.Text.Trim();

    public RenameDialog(string current, Window owner)
    {
        InitializeComponent();
        Owner = owner;
        NameBox.Text = current;
        NameBox.SelectAll();
        NameBox.Focus();
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(NameBox.Text))
        {
            return;
        }

        DialogResult = true;
    }
}
