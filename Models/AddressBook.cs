using System.Collections.Generic;

namespace LogisticsWeb.Models
{
    public class AddressEntry
    {
        public string Address { get; set; } = "";
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }

    public static class AddressBook
    {
        // Проверенный набор реальных адресов центра Москвы с координатами.
        public static readonly List<AddressEntry> Moscow =
        [
            new AddressEntry { Address = "г. Москва, ул. Тверская, д. 12", Latitude = 55.765171, Longitude = 37.605148 },
            new AddressEntry { Address = "г. Москва, ул. Арбат, д. 23", Latitude = 55.750809, Longitude = 37.597723 },
            new AddressEntry { Address = "г. Москва, ул. Мясницкая, д. 28", Latitude = 55.763331, Longitude = 37.632584 },
            new AddressEntry { Address = "г. Москва, ул. Пятницкая, д. 15", Latitude = 55.737157, Longitude = 37.625448 },
            new AddressEntry { Address = "г. Москва, ул. Большая Садовая, д. 5/1", Latitude = 55.765199, Longitude = 37.596395 },
            new AddressEntry { Address = "г. Москва, ул. Тверская-Ямская 1-я, д. 11", Latitude = 55.770404, Longitude = 37.600817 },
            new AddressEntry { Address = "г. Москва, ул. Кузнецкий Мост, д. 19", Latitude = 55.760839, Longitude = 37.624059 },
            new AddressEntry { Address = "г. Москва, ул. Никольская, д. 7", Latitude = 55.75731, Longitude = 37.625119 },
            new AddressEntry { Address = "г. Москва, ул. Покровка, д. 40", Latitude = 55.760298, Longitude = 37.649877 },
            new AddressEntry { Address = "г. Москва, ул. Маросейка, д. 6/8", Latitude = 55.757012, Longitude = 37.630494 },
            new AddressEntry { Address = "г. Москва, ул. Большая Дмитровка, д. 11", Latitude = 55.760631, Longitude = 37.613774 },
            new AddressEntry { Address = "г. Москва, ул. Неглинная, д. 12", Latitude = 55.76407, Longitude = 37.621949 },
            new AddressEntry { Address = "г. Москва, ул. Петровка, д. 20", Latitude = 55.766211, Longitude = 37.616385 },
            new AddressEntry { Address = "г. Москва, ул. Ильинка, д. 8", Latitude = 55.755809, Longitude = 37.623917 },
            new AddressEntry { Address = "г. Москва, ул. Варварка, д. 14", Latitude = 55.752494, Longitude = 37.627192 },
            new AddressEntry { Address = "г. Москва, ул. Большая Лубянка, д. 13", Latitude = 55.759011, Longitude = 37.626176 },
            new AddressEntry { Address = "г. Москва, ул. Старый Арбат, д. 36", Latitude = 55.749492, Longitude = 37.587489 },
            new AddressEntry { Address = "г. Москва, ул. Новый Арбат, д. 11", Latitude = 55.752423, Longitude = 37.58851 },
            new AddressEntry { Address = "г. Москва, ул. Солянка, д. 5", Latitude = 55.753097, Longitude = 37.636771 },
            new AddressEntry { Address = "г. Москва, ул. Остоженка, д. 10/2", Latitude = 55.741533, Longitude = 37.600139 },
            new AddressEntry { Address = "г. Москва, ул. Пречистенка, д. 17", Latitude = 55.741326, Longitude = 37.59136 },
            new AddressEntry { Address = "г. Москва, ул. Лесная, д. 43", Latitude = 55.772333, Longitude = 37.578612 },
            new AddressEntry { Address = "г. Москва, ул. Садовая-Кудринская, д. 23", Latitude = 55.758999, Longitude = 37.594248 },
            new AddressEntry { Address = "г. Москва, наб. Москва-реки Болотная, д. 9", Latitude = 55.746544, Longitude = 37.612043 },
            new AddressEntry { Address = "г. Москва, ул. Крымский Вал, д. 3", Latitude = 55.730579, Longitude = 37.602901 }
        ];
    }
}