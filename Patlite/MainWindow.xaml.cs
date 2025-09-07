using System.Diagnostics;
using System.Windows;

namespace Patlite
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow(IMainVM vm)
        {
            InitializeComponent();
            DataContext = vm;
            if (LogList != null)
            {
                vm.LogLines.CollectionChanged += (_, __) =>
                {
                    if (LogList.Items.Count > 0)
                        LogList.ScrollIntoView(LogList.Items[^1]);
                };
            }
        }
    }
}