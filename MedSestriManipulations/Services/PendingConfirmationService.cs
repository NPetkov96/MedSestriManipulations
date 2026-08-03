using MedSestriManipulations.Models;

namespace MedSestriManipulations.Services
{
    // Transient hand-off of a just-submitted patient from PatientDetailsPage to RequestConfirmationPage.
    public static class PendingConfirmationService
    {
        public static Patient? LastSubmitted { get; set; }
    }
}
