using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace MedSestriManipulations.Models
{
    public class Patient : INotifyPropertyChanged
    {
        public string FullName { get; set; } = "";
        public string Note { get; set; } = "";
        public string EGN { get; set; } = "";
        public string PhoneNumber { get; set; } = "";
        public string LabId { get; set; } = "";
        public string LabPassword { get; set; } = "";
        public DateTime Date { get; set; }
        public List<BloodTest> BloodTests { get; set; } = new();

        private bool isSelected;
        private decimal? _totalEuro;
        private string? _testsCountText;
        private string? _dateText;
        private string? _shortDateText;
        private string? _phoneFormatted;

        [JsonIgnore]
        public bool IsSelected
        {
            get => isSelected;
            set
            {
                if (isSelected != value)
                {
                    isSelected = value;
                    OnPropertyChanged();
                }
            }
        }

        [JsonIgnore]
        public decimal TotalEuro => _totalEuro ??= BloodTests?.Sum(b => b.EuroPrice) ?? 0;

        [JsonIgnore]
        public int TestsCount => BloodTests?.Count ?? 0;

        [JsonIgnore]
        public string TestsCountText => _testsCountText ??=
            TestsCount == 1 ? "1 изследване" : $"{TestsCount} изследвания";

        [JsonIgnore]
        public string DateText => _dateText ??= Date.ToString("dd.MM.yyyy  ·  HH:mm");

        [JsonIgnore]
        public string ShortDateText => _shortDateText ??= Date.ToString("dd.MM · HH:mm");

        // Компактен ред за История: ЕГН · dd.MM · HH:mm - изцяло цифри/символи,
        // затова целият ред използва обикновения шрифт (без нужда от FormattedString).
        [JsonIgnore]
        public string HistoryRowMetaText => $"{EGN} · {Date:dd.MM} · {Date:HH:mm}";

        // "N изследвания"/"1 изследване" с цифрата в обикновения шрифт, думата - в серифния,
        // както се показват сумите в Начало.
        [JsonIgnore]
        public FormattedString TestsCountFormatted
        {
            get
            {
                var resources = Application.Current!.Resources;
                var color = (Color)resources["WarmAccent700"];

                return new FormattedString
                {
                    Spans =
                    {
                        new Span { Text = TestsCount.ToString(), FontFamily = "OpenSansRegular", FontSize = 12, TextColor = color },
                        new Span { Text = TestsCount == 1 ? " изследване" : " изследвания", FontFamily = "LoraRegular", FontSize = 12, TextColor = color }
                    }
                };
            }
        }

        [JsonIgnore]
        public string PhoneFormatted
        {
            get
            {
                if (_phoneFormatted != null)
                    return _phoneFormatted;

                var p = PhoneNumber?.Trim() ?? string.Empty;
                if (p.Length == 10)
                    _phoneFormatted = $"{p.Substring(0, 4)} {p.Substring(4, 3)} {p.Substring(7, 3)}";
                else
                    _phoneFormatted = p;

                return _phoneFormatted;
            }
        }

        [JsonIgnore]
        public bool IsZeroPrice => TotalEuro == 0;

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }


    }
}
