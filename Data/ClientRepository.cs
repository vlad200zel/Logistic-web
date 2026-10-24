using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using LogisticsWeb.Models;

namespace LogisticsWeb.Data
{
    public interface IClientRepository
    {
        Task<List<Client>> GetAllAsync();
        Task<Client?> GetByIdAsync(int id);
        Task AddAsync(Client client);
        Task UpdateAsync(Client client);
        Task DeleteAsync(int id);
    }

    public class JsonClientRepository : IClientRepository
    {
        private readonly string _filePath;
        private readonly object _lock = new object();

        public JsonClientRepository(IWebHostEnvironment env)
        {
            string appDataPath = Path.Combine(env.ContentRootPath, "App_Data");
            Directory.CreateDirectory(appDataPath);
            _filePath = Path.Combine(appDataPath, "clients.json");
        }

        public Task<List<Client>> GetAllAsync()
        {
            lock (_lock)
            {
                return Task.FromResult(ReadAll());
            }
        }

        public Task<Client?> GetByIdAsync(int id)
        {
            lock (_lock)
            {
                return Task.FromResult(ReadAll().FirstOrDefault(c => c.Id == id));
            }
        }

        public Task AddAsync(Client client)
        {
            lock (_lock)
            {
                var all = ReadAll();
                client.Id = all.Count > 0 ? all.Max(c => c.Id) + 1 : 1;
                all.Add(client);
                WriteAll(all);
            }
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Client client)
        {
            lock (_lock)
            {
                var all = ReadAll();
                var index = all.FindIndex(c => c.Id == client.Id);
                if (index >= 0)
                {
                    all[index] = client;
                    WriteAll(all);
                }
            }
            return Task.CompletedTask;
        }

        public Task DeleteAsync(int id)
        {
            lock (_lock)
            {
                var all = ReadAll().Where(c => c.Id != id).ToList();
                WriteAll(all);
            }
            return Task.CompletedTask;
        }

        private List<Client> ReadAll()
        {
            if (!File.Exists(_filePath))
            {
                return new List<Client>();
            }

            var json = File.ReadAllText(_filePath);
            return JsonSerializer.Deserialize<List<Client>>(json) ?? new List<Client>();
        }

        private void WriteAll(List<Client> clients)
        {
            var json = JsonSerializer.Serialize(clients, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_filePath, json);
        }
    }
}