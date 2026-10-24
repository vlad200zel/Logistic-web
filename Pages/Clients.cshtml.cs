using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LogisticsWeb.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using LogisticsWeb.Data;

namespace LogisticsWeb.Pages
{
    public class ClientsModel : PageModel
    {
        private readonly IClientRepository _repository;

        public ClientsModel(IClientRepository repository)
        {
            _repository = repository;
        }

        [BindProperty]
        public Client InputClient { get; set; } = new Client();

        public List<Client> Clients { get; set; } = new List<Client>();

        public List<AddressEntry> AddressSuggestions { get; set; } = AddressBook.Moscow;

        public async Task OnGetAsync()
        {
            Clients = await _repository.GetAllAsync();
        }

        public async Task<IActionResult> OnGetSuggestAsync(string term)
        {
            var termNormalized = (term ?? "").Trim().ToLowerInvariant();

            var suggestions = AddressBook.Moscow
                .Where(a => string.IsNullOrEmpty(termNormalized) || a.Address.ToLowerInvariant().Contains(termNormalized))
                .Select(a => new
                {
                    address = a.Address,
                    latitude = a.Latitude,
                    longitude = a.Longitude
                })
                .ToList();

            return new JsonResult(suggestions);
        }

        public async Task<IActionResult> OnPostAddAsync()
        {
            if (!string.IsNullOrWhiteSpace(InputClient.Address))
            {
                // Попытка найти адрес в справочнике и подставить координаты автоматически
                var match = AddressBook.Moscow
                    .FirstOrDefault(a => a.Address.Equals(InputClient.Address.Trim(), StringComparison.OrdinalIgnoreCase));

                if (match != null && string.IsNullOrWhiteSpace(InputClient.Coordinates))
                {
                    InputClient.Coordinates = $"{match.Latitude.ToString(System.Globalization.CultureInfo.InvariantCulture)};{match.Longitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
                }

                await _repository.AddAsync(InputClient);
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            await _repository.DeleteAsync(id);
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostUpdateAsync(int id)
        {
            var target = await _repository.GetByIdAsync(id);
            if (target == null)
            {
                return RedirectToPage();
            }

            target.FullName = InputClient.FullName;
            target.Address = InputClient.Address;
            target.OrderAmount = InputClient.OrderAmount;

            if (!string.IsNullOrWhiteSpace(InputClient.Coordinates))
            {
                target.Coordinates = InputClient.Coordinates;
            }

            await _repository.UpdateAsync(target);
            return RedirectToPage();
        }
    }
}