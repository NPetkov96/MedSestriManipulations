using MedSestriManipulations.Models;
using Microsoft.Maui.Graphics;

namespace MedSestriManipulations.Controls
{
    internal static class StatisticsChartColors
    {
        public static readonly Color Text = Color.FromArgb("#201F1D");
        public static readonly Color Muted = Color.FromArgb("#8C201F1D");
        public static readonly Color Grid = Color.FromArgb("#29201F1D");
        public static readonly Color Accent = Color.FromArgb("#B68235");
        public static readonly Color AccentDark = Color.FromArgb("#7D5411");
        public static readonly Color DailyBars = Color.FromArgb("#597EA2");
    }

    public sealed class DailyStatisticsChartDrawable : IDrawable
    {
        private readonly IReadOnlyList<DailyStatistics> _data;
        private int? _selectedIndex;

        public DailyStatisticsChartDrawable(IReadOnlyList<DailyStatistics> data)
        {
            _data = data;
        }

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            if (_data.Count == 0)
            {
                DrawEmptyState(canvas, dirtyRect);
                return;
            }

            const float left = 36;
            const float top = 10;
            const float right = 8;
            const float bottom = 28;
            var plot = new RectF(left, top, Math.Max(1, dirtyRect.Width - left - right),
                Math.Max(1, dirtyRect.Height - top - bottom));
            var maximum = Math.Max(1d, _data.Max(point => Math.Max(point.RecordCount, (double)point.SevenDayAverage)));

            DrawYAxis(canvas, plot, maximum);

            canvas.StrokeColor = StatisticsChartColors.DailyBars;
            canvas.StrokeSize = Math.Max(1, Math.Min(3, plot.Width / Math.Max(1, _data.Count) * 0.65f));
            for (var index = 0; index < _data.Count; index++)
            {
                var x = GetX(plot, index, _data.Count);
                var y = GetY(plot, _data[index].RecordCount, maximum);
                canvas.DrawLine(x, plot.Bottom, x, y);
            }

            canvas.StrokeColor = StatisticsChartColors.Accent;
            canvas.StrokeSize = 2;
            var path = new PathF();
            for (var index = 0; index < _data.Count; index++)
            {
                var x = GetX(plot, index, _data.Count);
                var y = GetY(plot, (double)_data[index].SevenDayAverage, maximum);
                if (index == 0)
                    path.MoveTo(x, y);
                else
                    path.LineTo(x, y);
            }
            canvas.DrawPath(path);

            DrawDateLabels(canvas, plot, _data);
            DrawSelection(canvas, plot, maximum);
        }

        public DailyStatistics? SelectNearest(float touchX, float viewWidth)
        {
            const float left = 36;
            const float right = 8;

            if (_data.Count == 0 || viewWidth <= left + right)
                return null;

            var plotWidth = viewWidth - left - right;
            var relativeX = Math.Clamp(touchX - left, 0, plotWidth);
            var ratio = relativeX / plotWidth;
            _selectedIndex = Math.Clamp(
                (int)Math.Round(ratio * Math.Max(0, _data.Count - 1)),
                0,
                _data.Count - 1);

            return _data[_selectedIndex.Value];
        }

        private void DrawSelection(ICanvas canvas, RectF plot, double maximum)
        {
            if (_selectedIndex is not int index || index < 0 || index >= _data.Count)
                return;

            var x = GetX(plot, index, _data.Count);
            var recordY = GetY(plot, _data[index].RecordCount, maximum);
            var averageY = GetY(plot, (double)_data[index].SevenDayAverage, maximum);

            canvas.StrokeColor = StatisticsChartColors.Muted;
            canvas.StrokeSize = 1;
            canvas.DrawLine(x, plot.Top, x, plot.Bottom);

            canvas.FillColor = StatisticsChartColors.DailyBars;
            canvas.FillCircle(x, recordY, 4);
            canvas.FillColor = StatisticsChartColors.Accent;
            canvas.FillCircle(x, averageY, 4);
        }

        private static void DrawDateLabels(
            ICanvas canvas,
            RectF plot,
            IReadOnlyList<DailyStatistics> data)
        {
            canvas.FontColor = StatisticsChartColors.Muted;
            canvas.FontSize = 9;

            var indexes = new[] { 0, data.Count / 2, data.Count - 1 }.Distinct();
            foreach (var index in indexes)
            {
                var x = GetX(plot, index, data.Count);
                var label = data[index].Date.ToString("MM.yy");
                canvas.DrawString(label, x, plot.Bottom + 7, HorizontalAlignment.Center);
            }
        }

        internal static void DrawYAxis(ICanvas canvas, RectF plot, double maximum)
        {
            canvas.FontColor = StatisticsChartColors.Muted;
            canvas.FontSize = 9;
            canvas.StrokeColor = StatisticsChartColors.Grid;
            canvas.StrokeSize = 1;

            for (var step = 0; step <= 4; step++)
            {
                var ratio = step / 4f;
                var y = plot.Bottom - (plot.Height * ratio);
                canvas.DrawLine(plot.Left, y, plot.Right, y);
                var value = Math.Round(maximum * ratio);
                canvas.DrawString(value.ToString("0"), plot.Left - 7, y - 4, HorizontalAlignment.Right);
            }
        }

        internal static float GetX(RectF plot, int index, int count) => count <= 1
            ? plot.Center.X
            : plot.Left + (plot.Width * index / (count - 1f));

        internal static float GetY(RectF plot, double value, double maximum) =>
            plot.Bottom - (float)(plot.Height * value / maximum);

        internal static void DrawEmptyState(ICanvas canvas, RectF dirtyRect)
        {
            canvas.FontColor = StatisticsChartColors.Muted;
            canvas.FontSize = 13;
            canvas.DrawString("Няма налични данни", dirtyRect.Center.X, dirtyRect.Center.Y,
                HorizontalAlignment.Center);
        }
    }

    public sealed class MonthlyStatisticsChartDrawable : IDrawable
    {
        private readonly IReadOnlyList<MonthlyStatistics> _data;

        public MonthlyStatisticsChartDrawable(IReadOnlyList<MonthlyStatistics> data)
        {
            _data = data;
        }

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            if (_data.Count == 0)
            {
                DailyStatisticsChartDrawable.DrawEmptyState(canvas, dirtyRect);
                return;
            }

            var plot = new RectF(36, 10, Math.Max(1, dirtyRect.Width - 44),
                Math.Max(1, dirtyRect.Height - 38));
            var maximum = Math.Max(1, _data.Max(point => point.RecordCount));
            DailyStatisticsChartDrawable.DrawYAxis(canvas, plot, maximum);

            var slotWidth = plot.Width / _data.Count;
            var barWidth = Math.Max(3, slotWidth * 0.62f);
            canvas.FillColor = StatisticsChartColors.AccentDark;

            for (var index = 0; index < _data.Count; index++)
            {
                var height = plot.Height * _data[index].RecordCount / maximum;
                var x = plot.Left + (slotWidth * index) + ((slotWidth - barWidth) / 2);
                canvas.FillRoundedRectangle(x, plot.Bottom - height, barWidth, height, 2);
            }

            canvas.FontColor = StatisticsChartColors.Muted;
            canvas.FontSize = 9;
            var labelStep = Math.Max(1, (int)Math.Ceiling(_data.Count / 5d));
            for (var index = 0; index < _data.Count; index += labelStep)
            {
                var x = plot.Left + slotWidth * (index + 0.5f);
                canvas.DrawString(_data[index].Month.ToString("MM.yy"), x, plot.Bottom + 7,
                    HorizontalAlignment.Center);
            }
        }
    }

    public sealed class WeekdayStatisticsChartDrawable : IDrawable
    {
        private static readonly string[] DayNames =
            ["Понеделник", "Вторник", "Сряда", "Четвъртък", "Петък", "Събота", "Неделя"];
        private readonly IReadOnlyList<WeekdayStatistics> _data;

        public WeekdayStatisticsChartDrawable(IReadOnlyList<WeekdayStatistics> data)
        {
            _data = data;
        }

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            if (_data.Count == 0)
            {
                DailyStatisticsChartDrawable.DrawEmptyState(canvas, dirtyRect);
                return;
            }

            var values = Enumerable.Range(1, 7)
                .Select(day => _data.FirstOrDefault(value => value.DayNumber == day)?.RecordCount ?? 0)
                .ToArray();
            const float labelWidth = 82;
            const float valueWidth = 34;
            const float horizontalPadding = 8;
            var plot = new RectF(
                labelWidth,
                horizontalPadding,
                Math.Max(1, dirtyRect.Width - labelWidth - valueWidth - horizontalPadding),
                Math.Max(1, dirtyRect.Height - horizontalPadding * 2));
            var maximum = Math.Max(1, values.Max());
            var rowHeight = plot.Height / 7;
            var barHeight = Math.Min(22, rowHeight * 0.58f);

            for (var index = 0; index < values.Length; index++)
            {
                var centerY = plot.Top + rowHeight * (index + 0.5f);
                var barTop = centerY - barHeight / 2;
                var width = plot.Width * values[index] / maximum;

                canvas.FillColor = StatisticsChartColors.Grid;
                canvas.FillRoundedRectangle(plot.Left, barTop, plot.Width, barHeight, 4);
                canvas.FillColor = index >= 5
                    ? StatisticsChartColors.Accent
                    : StatisticsChartColors.DailyBars;
                canvas.FillRoundedRectangle(plot.Left, barTop, width, barHeight, 4);

                canvas.FontColor = StatisticsChartColors.Text;
                canvas.FontSize = 11;
                canvas.DrawString(DayNames[index], plot.Left - 7, centerY - 6,
                    HorizontalAlignment.Right);

                canvas.FontColor = index >= 5
                    ? StatisticsChartColors.AccentDark
                    : StatisticsChartColors.Text;
                canvas.FontSize = 11;
                canvas.DrawString(values[index].ToString(), plot.Right + 7, centerY - 6,
                    HorizontalAlignment.Left);
            }
        }
    }
}
