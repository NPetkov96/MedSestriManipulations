namespace MedSestriManipulations.Models
{
    public class PatientProcedureRow
    {
        public int Number { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal EuroPrice { get; set; }
        public bool HasDivider { get; set; }
    }
}
