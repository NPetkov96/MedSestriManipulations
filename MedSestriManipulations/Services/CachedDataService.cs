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
        private MedSestriStatistics? _statistics;
        private bool _isBloodTestsLoaded;
        private bool _isPatientsLoaded;
        private bool _isCathetersLoaded;
        private bool _isStatisticsLoaded;

        private static readonly string _bloodTestsCacheFile =
            Path.Combine(FileSystem.AppDataDirectory, "bloodtests_cache.json");

        private static readonly string _patientsCacheFile =
            Path.Combine(FileSystem.AppDataDirectory, "patients_cache.json");

        private static readonly string _cathetersCacheFile =
            Path.Combine(FileSystem.AppDataDirectory, "catheters_cache.json");

        private static readonly string _statisticsCacheFile =
            Path.Combine(FileSystem.AppDataDirectory, "statistics_cache.json");

        private Task<List<BloodTest>>? _bloodTestsInFlight;
        private Task<List<Patient>>? _patientsInFlight;
        private Task<List<Catheter>>? _cathetersInFlight;
        private Task<MedSestriStatistics>? _statisticsInFlight;

        public int PatientsVersion { get; private set; }
        public int StatisticsVersion { get; private set; }

        public CachedDataService(API api)
        {
            _api = api;
        }

        public Task<List<BloodTest>> GetBloodTestsAsync()
        {
            if (_isBloodTestsLoaded)
                return Task.FromResult(_bloodTests);

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

                        _ = RefreshBloodTestsFromApiAsync();
                        return _bloodTests;
                    }
                }
                catch {  }
            }

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
            catch {  }
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
                            PatientsVersion++;

                            _ = RefreshPatientsFromApiAsync();
                            return _patients;
                        }
                    }
                    catch {  }
                }

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
                    PatientsVersion++;
                    var json = JsonSerializer.Serialize(fresh);
                    await File.WriteAllTextAsync(_patientsCacheFile, json);
                }
            }
            catch {  }
        }

        public Task<List<Catheter>> GetCathetersAsync()
        {
            if (_isCathetersLoaded)
                return Task.FromResult(_catheters);

            return _cathetersInFlight ??= LoadCathetersAsync();
        }

        private async Task<List<Catheter>> LoadCathetersAsync()
        {
            try
            {
                if (File.Exists(_cathetersCacheFile))
                {
                    try
                    {
                        var json = await File.ReadAllTextAsync(_cathetersCacheFile);
                        var cached = JsonSerializer.Deserialize<List<Catheter>>(json);
                        if (cached != null)
                        {
                            _catheters = cached;
                            _isCathetersLoaded = true;

                            _ = RefreshCathetersFromApiAsync();
                            return _catheters;
                        }
                    }
                    catch {  }
                }

                await RefreshCathetersFromApiAsync();
                return _catheters;
            }
            finally
            {
                _cathetersInFlight = null;
            }
        }

        private async Task RefreshCathetersFromApiAsync()
        {
            try
            {
                var fresh = await _api.GetAllCatheterAppointments();
                if (fresh != null)
                {
                    _catheters = fresh;
                    _isCathetersLoaded = true;
                    var json = JsonSerializer.Serialize(fresh);
                    await File.WriteAllTextAsync(_cathetersCacheFile, json);
                }
            }
            catch {  }
        }

        public Task<MedSestriStatistics> GetStatisticsAsync(bool forceRefresh = false)
        {
            if (forceRefresh)
            {
                _isStatisticsLoaded = false;
                TryDeleteCacheFile(_statisticsCacheFile);
            }

            if (_isStatisticsLoaded && _statistics != null)
                return Task.FromResult(_statistics);

            return _statisticsInFlight ??= LoadStatisticsAsync(forceRefresh);
        }

        private async Task<MedSestriStatistics> LoadStatisticsAsync(bool forceRefresh)
        {
            try
            {
                if (!forceRefresh && File.Exists(_statisticsCacheFile))
                {
                    try
                    {
                        var json = await File.ReadAllTextAsync(_statisticsCacheFile);
                        var cached = JsonSerializer.Deserialize<MedSestriStatistics>(json);
                        if (cached != null)
                        {
                            _statistics = cached;
                            _isStatisticsLoaded = true;
                            StatisticsVersion++;

                            _ = RefreshStatisticsInBackgroundAsync();
                            return cached;
                        }
                    }
                    catch { }
                }

                return await RefreshStatisticsFromApiAsync();
            }
            finally
            {
                _statisticsInFlight = null;
            }
        }

        private async Task RefreshStatisticsInBackgroundAsync()
        {
            try
            {
                await RefreshStatisticsFromApiAsync();
            }
            catch
            {
                // The cached statistics remain available when the background refresh fails.
            }
        }

        private async Task<MedSestriStatistics> RefreshStatisticsFromApiAsync()
        {
            var fresh = await _api.GetStatisticsAsync();
            _statistics = fresh;
            _isStatisticsLoaded = true;
            StatisticsVersion++;

            var json = JsonSerializer.Serialize(fresh);
            await File.WriteAllTextAsync(_statisticsCacheFile, json);
            return fresh;
        }

        public void InvalidatePatients()
        {
            _isPatientsLoaded = false;
            PatientsVersion++;
            TryDeleteCacheFile(_patientsCacheFile);
            InvalidateStatistics();
        }

        public void InvalidateStatistics()
        {
            _isStatisticsLoaded = false;
            _statistics = null;
            StatisticsVersion++;
            TryDeleteCacheFile(_statisticsCacheFile);
        }

        private static void TryDeleteCacheFile(string path)
        {
            try
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
            catch {  }
        }

        public void InvalidateCatheters()
        {
            _isCathetersLoaded = false;
            TryDeleteCacheFile(_cathetersCacheFile);
        }
    }
}
