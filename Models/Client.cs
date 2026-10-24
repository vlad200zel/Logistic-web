using System;

namespace LogisticsWeb.Models
{
    public class Client
    {
        public int Id { get; set; }

        public string FullName { get; set; } = "";

        public string Address { get; set; } = "";

        public string Coordinates { get; set; } = "";

        public decimal OrderAmount { get; set; }

        public double Latitude
        {
            get
            {
                var parts = ParseCoordinates();
                return parts.lat;
            }
        }

        public double Longitude
        {
            get
            {
                var parts = ParseCoordinates();
                return parts.lon;
            }
        }

        private (double lat, double lon) ParseCoordinates()
        {
            if (string.IsNullOrWhiteSpace(Coordinates))
            {
                return (0, 0);
            }

            var parts = Coordinates.Split(';');
            if (parts.Length != 2)
            {
                return (0, 0);
            }

            if (double.TryParse(parts[0].Trim(), System.Globalization.CultureInfo.InvariantCulture, out double lat) &&
                double.TryParse(parts[1].Trim(), System.Globalization.CultureInfo.InvariantCulture, out double lon))
            {
                return (lat, lon);
            }

            return (0, 0);
        }
    }
}