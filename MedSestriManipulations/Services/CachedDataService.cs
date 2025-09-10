using MedSestriManipulations.ApiHandler;
using MedSestriManipulations.Models;

namespace MedSestriManipulations.Services
{
    public class CachedDataService
    {
        private API _api;

        public CachedDataService(API api)
        {
            _api = api;
        }

        private List<BloodTest> _bloodTests;
        private bool _isBloodTestsLoaded = false;
        public async Task<List<BloodTest>> GetBloodTestsAsync()
        {
            if (!_isBloodTestsLoaded)
            {
                _bloodTests = await _api.GetAllBloodTest();
                _isBloodTestsLoaded = true;
            }

            return _bloodTests;
        }


        public List<Patient> _patients;
        public bool _isPatientsLoaded = false;
        public async Task<List<Patient>> GetPatientsAsync()
        {
            if (!_isPatientsLoaded)
            {
                _patients = await _api.GetAllPatientsHistory();
                _isPatientsLoaded = true;
            }

            return _patients;
        }


        public List<Catheter> _catheters;
        public bool _isCathetersLoaded = false;
        public async Task<List<Catheter>> GetCathetersAsync()
        {
            if (!_isCathetersLoaded)
            {
                _catheters = await _api.GetAllCatheterAppointments();
                _isCathetersLoaded = true;
            }

            return _catheters;
        }
    }
}
