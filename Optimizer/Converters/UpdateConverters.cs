using System;
using System.Globalization;
using System.Windows.Data;

namespace Optimizer.Converters
{
    /// <summary>
    /// Convertit une progression (0.0 → 1.0) en largeur en pixels
    /// en fonction de la largeur du conteneur parent.
    /// Usage : MultiBinding avec UpdateProgress + ActualWidth du conteneur.
    /// </summary>
    public class ProgressToWidthConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 2)
                return 0.0;

            if (values[0] is not double progress || values[1] is not double containerWidth)
                return 0.0;

            return Math.Max(0, Math.Min(containerWidth, containerWidth * progress));
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    /// <summary>
    /// Convertit une progression (0.0 → 1.0) en pourcentage (0 → 100).
    /// </summary>
    public class ProgressToPercentConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double progress)
                return progress * 100.0;
            return 0.0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}