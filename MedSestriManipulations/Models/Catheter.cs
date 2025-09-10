namespace MedSestriManipulations.Models
{
    public class Catheter
    {
        public int Id { get; set; }
        public string ClientName { get; set; }
        public string PhoneNumber { get; set; }
        public DateTime Date { get; set; }
        public string Address { get; set; }
        public bool IsOverdue { get; set; }
        public bool IsChecked { get; set; }
    }
}
