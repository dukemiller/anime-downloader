using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using anime_downloader.ViewModels;
using GalaSoft.MvvmLight.Ioc;
using MahApps.Metro.Controls;

namespace anime_downloader.Views
{
    /// <summary>
    /// Interaction logic for Discover.xaml
    /// </summary>
    public partial class Discover
    {
        public Discover()
        {
            InitializeComponent();
        }

        private async void VisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            var isVisible = (bool) e.NewValue;
            (DataContext as DiscoverViewModel)?.VisibilityChanged(isVisible);

            while (SimpleIoc.Default.GetInstance<MainWindowViewModel>().IsShowing)
                await Task.Delay(100);

            if (isVisible)
            {
                var currentFlipView = (ParentTabControl.SelectedItem as MetroTabItem)?.Content as FlipView;

                if (currentFlipView == null)
                    return;

                await Dispatcher.BeginInvoke(
                    DispatcherPriority.Input,
                    new Action(() =>
                    {
                        currentFlipView.Focus(); // Set Logical Focus
                        Keyboard.Focus(currentFlipView); // Set Keyboard Focus
                    })
                );
            }
        }
    }
}