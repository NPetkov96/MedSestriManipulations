using MedSestriManipulations.Models;

namespace MedSestriManipulations.Services
{
    public static class SelectedPatientService
    {
        // Consumed by MainPage.OnAppearing to offer loading the previous blood-test selection.
        public static Patient? PatientToReuse { get; set; }

        // Consumed by PatientDetailsPage.OnAppearing to prefill Име/ЕГН/Телефон - a separate
        // slot from PatientToReuse so each page can independently read-and-clear its own part
        // of the "Използвай" hand-off without racing the other.
        public static Patient? ContactInfoToReuse { get; set; }
    }
}
