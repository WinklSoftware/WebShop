﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using ShopBase;

namespace DesktopApp
{
    public class Converter2 : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            object o = null;

            switch (value)
            {
                case List<Position>:
                    o = (value as List<Position>).Count;
                    break;
                default:
                    break;
            }


            return o;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
    }
}
