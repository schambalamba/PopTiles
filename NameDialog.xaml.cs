using System.Windows;

namespace TaskbarGruppen
{
    public partial class NameDialog : Window
    {
        public string GroupName => NameBox.Text.Trim();

        public NameDialog(string suggestion)
        {
            InitializeComponent();
            SourceInitialized += (s, e) => Theme.ApplyTitleBar(this);
            NameBox.Text = suggestion;
            Loaded += (s, e) =>
            {
                NameBox.Focus();
                NameBox.SelectAll();
            };
        }

        void Create_Click(object sender, RoutedEventArgs e)
        {
            if (GroupName.Length > 0)
                DialogResult = true;
        }
    }
}
