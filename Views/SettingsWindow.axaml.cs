using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace ActiveRest.Views
{
    public partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            InitializeComponent();
            Opened += (_, _) => WindowPlacement.ClampToWorkArea(this);
        }

        private void Close_Click(object? sender, RoutedEventArgs e) => Close();

        private void OnKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape) Close();
        }

        private void OnHeaderPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
                BeginMoveDrag(e);
        }
    }
}
