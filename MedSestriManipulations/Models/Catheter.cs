namespace MedSestriManipulations.Models
{
    public class Catheter
    {
        public string ClientName { get; set; }
        public string PhoneNumber { get; set; }
        public DateTime Date { get; set; }
        public string Address { get; set; }
        public bool IsOverdue => Date < DateTime.Today.AddMonths(-1);
        public bool IsChecked { get; set; }
    }
}
