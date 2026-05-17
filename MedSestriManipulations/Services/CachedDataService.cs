using MedSestriManipulations.ApiHandler;
using MedSestriManipulations.Models;

namespace MedSestriManipulations.Services
{
    public class CachedDataService
    {
        private readonly API _api;
        private List<BloodTest> _bloodTests = new();
        private List<Patient> _patients = new();
        private List<Catheter> _catheters = new();
        private bool _isBloodTestsLoaded;
        private bool _isPatientsLoaded;
        private bool _isCathetersLoaded;

        public CachedDataService(API api)
        {
            _api = api;
        }

        public async Task<List<BloodTest>> GetBloodTestsAsync()
        {
            if (!_isBloodTestsLoaded)
            {
                _bloodTests = await _api.GetAllBloodTest();
                _isBloodTestsLoaded = true;
            }

            return _bloodTests;
        }


        public async Task<List<Patient>> GetPatientsAsync()
        {
            if (!_isPatientsLoaded)
            {
                _patients = await _api.GetAllPatientsHistory();
                _isPatientsLoaded = true;
            }

            return _patients;
        }


        public async Task<List<Catheter>> GetCathetersAsync()
        {
            if (!_isCathetersLoaded)
            {
                _catheters = await _api.GetAllCatheterAppointments();
                _isCathetersLoaded = true;
            }

            return _catheters;
        }

        public void InvalidatePatients()
        {
            _isPatientsLoaded = false;
        }

        public void InvalidateCatheters()
        {
            _isCathetersLoaded = false;
        }
    }
}
