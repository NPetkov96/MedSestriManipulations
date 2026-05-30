using System.Globalization;

namespace MedSestriManipulations.Converters
{
    public class HighlightConverter : IMultiValueConverter
    {
        private static readonly Color TextColor = Color.FromArgb("#1A1A1A");
        private static readonly Color HighlightTextColor = Color.FromArgb("#0066CC");
        private static readonly Color HighlightBg = Color.FromArgb("#E8F0FF");

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            string name = values.ElementAtOrDefault(0) as string ?? string.Empty;
            string search = values.ElementAtOrDefault(1) as string ?? string.Empty;

            var fs = new FormattedString();

            if (string.IsNullOrEmpty(search))
            {
                fs.Spans.Add(MakeSpan(name, TextColor));
                return fs;
            }

            int idx = name.IndexOf(search, StringComparison.OrdinalIgnoreCase);
            if (idx < 0)
            {
                fs.Spans.Add(MakeSpan(name, TextColor));
                return fs;
            }

            if (idx > 0)
                fs.Spans.Add(MakeSpan(name[..idx], TextColor));

            fs.Spans.Add(new Span
            {
                Text = name.Substring(idx, search.Length),
                TextColor = HighlightTextColor,
                BackgroundColor = HighlightBg,
                FontSize = 14,
                FontAttributes = FontAttributes.Bold
            });

            if (idx + search.Length < name.Length)
                fs.Spans.Add(MakeSpan(name[(idx + search.Length)..], TextColor));

            return fs;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotImplementedException();

        private static Span MakeSpan(string text, Color color) => new()
        {
            Text = text,
            TextColor = color,
            FontSize = 14,
            FontAttributes = FontAttributes.Bold
        };
    }
}
