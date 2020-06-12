namespace OnlyR.Behaviours
{
    using System.Windows;

    internal static class VisibilityFocusBehaviour
    {
        public static readonly DependencyProperty IsFocusEnabledProperty;

        public static void SetIsFocusEnabled(DependencyObject dependencyObject, bool value)
        {
            dependencyObject.SetValue(IsFocusEnabledProperty, value);
        }

        public static bool GetIsFocusEnabled(DependencyObject dependencyObject)
        {
            return (bool)dependencyObject.GetValue(IsFocusEnabledProperty);
        }

        static VisibilityFocusBehaviour()
        {
            IsFocusEnabledProperty = DependencyProperty.RegisterAttached(
                "IsFocusEnabled",
                typeof(bool),
                typeof(VisibilityFocusBehaviour),
                new UIPropertyMetadata(false, IsFocusTurn));
        }

        private static void IsFocusTurn(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is bool && (bool)e.NewValue && sender is UIElement element)
            {
                element.IsVisibleChanged += ElementIsVisibleChanged;
            }
        }

        private static void ElementIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is UIElement visibilityElement && visibilityElement.IsVisible)
            {
                visibilityElement.Focus();
            }
        }
    }
}
