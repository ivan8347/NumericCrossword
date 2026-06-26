using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace NumericCrossword
{
    public class PlayersConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var list = value as List<string>;
            if (list == null || list.Count == 0)
                return "нет";

            // Убираем первого игрока (создателя)
            var others = list.Skip(1).ToList();

            if (others.Count == 0)
                return "никто";

            return string.Join(", ", others);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

}
