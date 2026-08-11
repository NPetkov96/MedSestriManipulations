using System.Globalization;

namespace MedSestriManipulations.Models
{
    public class MedSestriStatistics
    {
        public DateTime? FirstDate { get; set; }
        public DateTime? LastDate { get; set; }
        public int TotalDays { get; set; }
        public int ActiveDays { get; set; }
        public decimal AveragePerActiveDay { get; set; }
        public int MaximumPerDay { get; set; }
        public PeriodStatistics Last7Days { get; set; } = new();
        public PeriodStatistics Last30Days { get; set; } = new();
        public List<DailyStatistics> Daily { get; set; } = new();
        public List<MonthlyStatistics> Monthly { get; set; } = new();
        public List<WeekdayStatistics> ByWeekday { get; set; } = new();

        public string PeriodText => FirstDate.HasValue
            ? $"Данни от {FirstDate:dd.MM.yyyy} до днес"
            : "Все още няма налични данни";
    }

    public class PeriodStatistics
    {
        public int RecordCount { get; set; }
        public int PreviousRecordCount { get; set; }
        public decimal? ChangePercent { get; set; }

        public string ChangeText => ChangePercent.HasValue
            ? $"{ChangePercent.Value:+0.0;-0.0;0.0}%"
            : "N/A";
    }

    public class DailyStatistics
    {
        public DateTime Date { get; set; }
        public int RecordCount { get; set; }
        public decimal SevenDayAverage { get; set; }
    }

    public class MonthlyStatistics
    {
        private static readonly CultureInfo BulgarianCulture = new("bg-BG");

        public DateTime Month { get; set; }
        public int RecordCount { get; set; }
        public int ActiveDays { get; set; }
        public decimal AveragePerActiveDay { get; set; }
        public int MaximumPerDay { get; set; }

        public string MonthText
        {
            get
            {
                var value = Month.ToString("MMMM yyyy", BulgarianCulture);
                return BulgarianCulture.TextInfo.ToTitleCase(value);
            }
        }
    }

    public class WeekdayStatistics
    {
        public int DayNumber { get; set; }
        public string DayName { get; set; } = string.Empty;
        public int RecordCount { get; set; }
    }
}
