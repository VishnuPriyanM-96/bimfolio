using System.Collections.Generic;
using System.Windows;

namespace MultipleParameterNested
{
    public partial class ParameterPicker : Window
    {
        public ParamPreview SelectedParam { get; private set; }

        public ParameterPicker(List<ParamPreview> parameters)
        {
            InitializeComponent(); // this works because XAML and .cs are linked
            ParamListBox.ItemsSource = parameters;
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            SelectedParam = ParamListBox.SelectedItem as ParamPreview;
            this.DialogResult = true;
            this.Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
