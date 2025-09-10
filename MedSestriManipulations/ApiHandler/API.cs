using MedSestriManipulations.Models;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace MedSestriManipulations.ApiHandler
{
    public class API
    {
        private readonly HttpClient _httpClient;
        private readonly string ErrorMessage = "Сървиса не работи.\nОбади се на НИКИ!";

        public API(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _httpClient.BaseAddress = new Uri("https://homeserver.ngrok.pro/");
        }

        //
        //
        //                                      BLOOD TEST
        public async Task<List<BloodTest>> GetAllBloodTest()
        {
            var response = await _httpClient.GetAsync("api/Bodimed/allBloodTests");
            var result = new List<BloodTest>();
            if (response.IsSuccessStatusCode)
            {
                result = await response.Content.ReadFromJsonAsync<List<BloodTest>>();
            }
            else
            {
                throw new ArgumentException(ErrorMessage);
            }
            return result!;
        }

        //
        //
        //                                      PATIENTS
        public async Task<HttpResponseMessage> CreateNewPatient(Patient model)
        {
            string json = JsonSerializer.Serialize(model, new JsonSerializerOptions { WriteIndented = true });


            var response = await _httpClient.PostAsJsonAsync("api/Bodimed/createPatient", model);
            return response;
        }

        public async Task<List<Patient>> GetAllPatientsHistory()
        {
            var response = await _httpClient.GetAsync("api/Bodimed/allPatientsHistory");
            var result = new List<Patient>();
            if (response.IsSuccessStatusCode)
            {
                result = await response.Content.ReadFromJsonAsync<List<Patient>>();
            }
            else
            {
                throw new ArgumentException(ErrorMessage);
            }
            return result!;
        }

        public async Task<HttpResponseMessage> DeletePatient(DateTime date)
        {
            var json = JsonSerializer.Serialize(date);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            return await _httpClient.PostAsync("api/Bodimed/deletePatient", content);
        }

        //
        //
        //                                      CATHETERS
        public async Task<HttpResponseMessage> CreateCatheterappointment(Catheter model)
        {
            string json = JsonSerializer.Serialize(model, new JsonSerializerOptions { WriteIndented = true });

            var resposne = await _httpClient.PostAsJsonAsync("api/Bodimed/createCatheterAppointment", model);
            return resposne;
        }

        public async Task<List<Catheter>> GetAllCatheterAppointments()
        {
            var response = await _httpClient.GetAsync("api/Bodimed/getAllCatheterAppointments");
            var result = new List<Catheter>();
            if(response.IsSuccessStatusCode)
            {
                result = await response.Content.ReadFromJsonAsync<List<Catheter>>();
            }

            return result!.OrderBy(p=>p.Date).ToList();
        }

        public async Task CheckCatheterAppointment(Catheter model)
        {
            string json = JsonSerializer.Serialize(model, new JsonSerializerOptions { WriteIndented = true });

            await _httpClient.PutAsJsonAsync("api/Bodimed/checkCatheterAppointment", model);
        }

        public async Task UpdateCatheterAppointment(Catheter model)
        {
            string json = JsonSerializer.Serialize(model, new JsonSerializerOptions { WriteIndented = true });

            await _httpClient.PutAsJsonAsync("api/Bodimed/updateCatheterAppointment", model);
        }
    }
}
