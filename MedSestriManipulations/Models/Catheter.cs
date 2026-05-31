namespace MedSestriManipulations.Models
{
    public class Catheter
    {
        public int Id { get; set; }
        public string ClientName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public string Address { get; set; } = string.Empty;
        public bool IsOverdue { get; set; }
        public bool IsChecked { get; set; }
    }
}
