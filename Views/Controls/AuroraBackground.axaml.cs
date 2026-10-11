using Avalonia;
using Avalonia.Controls;

namespace ActiveRest.Views.Controls
{
    public partial class AuroraBackground : UserControl
    {
        public AuroraBackground()
        {
            InitializeComponent();
        }

        /// <summary>The blobs are decoration only: they must never enlarge the window they sit in.</summary>
        protected override Size MeasureOverride(Size availableSize)
        {
            base.MeasureOverride(availableSize);
            return default;
        }
    }
}
