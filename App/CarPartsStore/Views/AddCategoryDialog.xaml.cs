using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace CarPartsStore.Views
{
    public partial class AddCategoryDialog : UserControl
    {
        public AddCategoryDialog()
        {
            InitializeComponent();
            Loaded += (_, _) => CategoryNameTextBox.Focus();
        }

        public string CategoryName => CategoryNameTextBox.Text?.Trim() ?? string.Empty;
    }
}

