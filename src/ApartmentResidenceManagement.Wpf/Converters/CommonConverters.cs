using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ApartmentResidenceManagement.Wpf.Converters;

public class IntToBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int intVal && parameter != null)
        {
            if (int.TryParse(parameter.ToString(), out int paramVal))
            {
                return intVal == paramVal;
            }
        }
        return false;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolVal && boolVal && parameter != null)
        {
            if (int.TryParse(parameter.ToString(), out int paramVal))
            {
                return paramVal;
            }
        }
        return Binding.DoNothing;
    }
}

public class IntToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int intVal && parameter != null)
        {
            if (int.TryParse(parameter.ToString(), out int paramVal))
            {
                return intVal == paramVal ? Visibility.Visible : Visibility.Collapsed;
            }
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class ZeroToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int count)
        {
            return count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class StringToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var text = value as string;
        return !string.IsNullOrWhiteSpace(text) ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
