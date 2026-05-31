using MedSestriManipulations.ApiHandler;
using MedSestriManipulations.Models;
using System.Text.Json;

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

        private static readonly string _bloodTestsCacheFile =
            Path.Combine(FileSystem.AppDataDirectory, "bloodtests_cache.json");

        private static readonly string _patientsCacheFile =
            Path.Combine(FileSystem.AppDataDirectory, "patients_cache.json");

        // Shared in-flight load so the splash preload and MainPage don't race.
        private Task<List<BloodTest>>? _bloodTestsInFlight;
        private Task<List<Patient>>? _patientsInFlight;

        public CachedDataService(API api)
        {
            _api = api;
        }

        public Task<List<BloodTest>> GetBloodTestsAsync()
        {
            if (_isBloodTestsLoaded)
                return Task.FromResult(_bloodTests);

            // If a load is already running (e.g. started by the splash), reuse it.
            return _bloodTestsInFlight ??= LoadBloodTestsAsync();
        }

        private async Task<List<BloodTest>> LoadBloodTestsAsync()
        {
            try
            {
                return await LoadBloodTestsCoreAsync();
            }
            finally
            {
                _bloodTestsInFlight = null;
            }
        }

        private async Task<List<BloodTest>> LoadBloodTestsCoreAsync()
        {
            if (_isBloodTestsLoaded)
                return _bloodTests;

            // Load from disk cache instantly if available
            if (File.Exists(_bloodTestsCacheFile))
            {
                try
                {
                    var json = await File.ReadAllTextAsync(_bloodTestsCacheFile);
                    var cached = JsonSerializer.Deserialize<List<BloodTest>>(json);
                    if (cached != null && cached.Count > 0)
                    {
                        _bloodTests = cached;
                        _isBloodTestsLoaded = true;

                        // Refresh from API in background without blocking
                        _ = RefreshBloodTestsFromApiAsync();
                        return _bloodTests;
                    }
                }
                catch { /* corrupt cache — fall through to API */ }
            }

            // No cache yet — fetch from API and save
            await RefreshBloodTestsFromApiAsync();
            return _bloodTests;
        }

        private async Task RefreshBloodTestsFromApiAsync()
        {
            try
            {
                var fresh = await _api.GetAllBloodTest();
                if (fresh != null && fresh.Count > 0)
                {
                    _bloodTests = fresh;
                    _isBloodTestsLoaded = true;
                    var json = JsonSerializer.Serialize(fresh);
                    await File.WriteAllTextAsync(_bloodTestsCacheFile, json);
                }
            }
            catch { /* network error — keep whatever we had */ }
        }

        public Task<List<Patient>> GetPatientsAsync()
        {
            if (_isPatientsLoaded)
                return Task.FromResult(_patients);

            return _patientsInFlight ??= LoadPatientsAsync();
        }

        private async Task<List<Patient>> LoadPatientsAsync()
        {
            try
            {
                // Load from disk cache instantly if available
                if (File.Exists(_patientsCacheFile))
                {
                    try
                    {
                        var json = await File.ReadAllTextAsync(_patientsCacheFile);
                        var cached = JsonSerializer.Deserialize<List<Patient>>(json);
                        if (cached != null && cached.Count > 0)
                        {
                            _patients = cached;
                            _isPatientsLoaded = true;

                            // Refresh from API in background without blocking
                            _ = RefreshPatientsFromApiAsync();
                            return _patients;
                        }
                    }
                    catch { /* corrupt cache — fall through to API */ }
                }

                // No cache yet — fetch from API and save
                await RefreshPatientsFromApiAsync();
                return _patients;
            }
            finally
            {
                _patientsInFlight = null;
            }
        }

        private async Task RefreshPatientsFromApiAsync()
        {
            try
            {
                var fresh = await _api.GetAllPatientsHistory();
                if (fresh != null)
                {
                    _patients = fresh;
                    _isPatientsLoaded = true;
                    var json = JsonSerializer.Serialize(fresh);
                    await File.WriteAllTextAsync(_patientsCacheFile, json);
                }
            }
            catch { /* network error — keep whatever we had */ }
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
            TryDeleteCacheFile(_patientsCacheFile);
        }

        private static void TryDeleteCacheFile(string path)
        {
            try
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
            catch { /* best effort — stale file will be overwritten on next refresh */ }
        }

        public void InvalidateCatheters()
        {
            _isCathetersLoaded = false;
        }
    }
}
