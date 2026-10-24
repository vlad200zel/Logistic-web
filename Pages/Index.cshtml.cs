using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using LogisticsWeb.Data;
using LogisticsWeb.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace LogisticsWeb.Pages
{
    public class IndexModel : PageModel
    {
        private const decimal CostPerKm = 900m; // 900 ₽ за км
        private static readonly HttpClient _httpClient = new HttpClient();
        private static readonly Random _random = new Random();
        private const string SessionKeyPoints = "GeneratedPointsData";
        private readonly IClientRepository _clientRepository;

        public IndexModel(IClientRepository clientRepository)
        {
            _clientRepository = clientRepository;
        }

        public List<RouteStepViewModel> RouteSteps { get; private set; } = new();
        public List<MapPointViewModel> MapPoints { get; private set; } = new();

        public double TotalDistance { get; private set; }
        public decimal TotalRevenue { get; private set; }
        public decimal TravelCost { get; private set; }
        public decimal NetProfit { get; private set; }
        public bool IsCalculated { get; private set; }

        public void OnGet()
        {
            MapPoints = GetPointsFromSession();
        }

        public IActionResult OnPostGeneratePoints()
        {
            MapPoints = GenerateRandomPoints();
            SavePointsToSession(MapPoints);

            IsCalculated = false;
            return Page();
        }

        public async Task<IActionResult> OnPostImportClientsAsync()
        {
            var clients = await _clientRepository.GetAllAsync();

            var points = new List<MapPointViewModel>
            {
                new MapPointViewModel { Name = "Базовый Склад", Latitude = 55.751244, Longitude = 37.618423, Revenue = 0, IsBase = true }
            };

            foreach (var c in clients)
            {
                if (c.Latitude == 0 && c.Longitude == 0) continue;

                points.Add(new MapPointViewModel
                {
                    Name = c.FullName,
                    Latitude = c.Latitude,
                    Longitude = c.Longitude,
                    Revenue = c.OrderAmount,
                    IsVisited = false
                });
            }

            SavePointsToSession(points);

            MapPoints = points;
            IsCalculated = false;
            return Page();
        }

        public async Task<IActionResult> OnPostCalculateRouteAsync()
        {
            MapPoints = GetPointsFromSession();

            if (MapPoints == null || MapPoints.Count <= 1)
            {
                MapPoints = GenerateRandomPoints();
                SavePointsToSession(MapPoints);
            }

            double[,] distances = await BuildRoadDistanceMatrixAsync(MapPoints);

            // Находим наилучшую комбинацию точек
            List<int> bestRoute = FindMostProfitableRouteDijkstra(MapPoints, distances);

            TotalRevenue = 0;
            TotalDistance = 0;
            int previousPoint = 0;

            RouteSteps.Add(new RouteStepViewModel
            {
                Status = "Старт",
                Name = MapPoints[0].Name,
                Revenue = 0,
                LegDistanceKm = 0,
                LegCost = 0,
                LegProfit = 0
            });

            // Сбрасываем флаги посещений
            foreach (var p in MapPoints)
            {
                p.IsVisited = false;
                p.RouteOrder = 0;
            }

            for (int i = 0; i < bestRoute.Count; i++)
            {
                int currentPoint = bestRoute[i];
                double legDistance = distances[previousPoint, currentPoint];
                decimal legCost = (decimal)legDistance * CostPerKm;
                decimal legProfit = MapPoints[currentPoint].Revenue - legCost;

                MapPoints[currentPoint].IsVisited = true;
                MapPoints[currentPoint].RouteOrder = i + 1;

                TotalDistance += legDistance;
                TotalRevenue += MapPoints[currentPoint].Revenue;

                RouteSteps.Add(new RouteStepViewModel
                {
                    Status = $"Остановка {i + 1}",
                    Name = MapPoints[currentPoint].Name,
                    Revenue = MapPoints[currentPoint].Revenue,
                    LegDistanceKm = legDistance,
                    LegCost = legCost,
                    LegProfit = legProfit
                });

                previousPoint = currentPoint;
            }

            // Возврат на склад
            double returnDistance = distances[previousPoint, 0];
            decimal returnCost = (decimal)returnDistance * CostPerKm;
            TotalDistance += returnDistance;

            RouteSteps.Add(new RouteStepViewModel
            {
                Status = "Возврат",
                Name = MapPoints[0].Name,
                Revenue = 0,
                LegDistanceKm = returnDistance,
                LegCost = returnCost,
                LegProfit = -returnCost
            });

            TravelCost = (decimal)TotalDistance * CostPerKm;
            NetProfit = TotalRevenue - TravelCost;

            SavePointsToSession(MapPoints);

            IsCalculated = true;
            return Page();
        }

        #region Вспомогательные методы

        private void SavePointsToSession(List<MapPointViewModel> points)
        {
            var json = JsonSerializer.Serialize(points);
            HttpContext.Session.SetString(SessionKeyPoints, json);
        }

        private List<MapPointViewModel> GetPointsFromSession()
        {
            var json = HttpContext.Session.GetString(SessionKeyPoints);
            if (string.IsNullOrEmpty(json)) return new List<MapPointViewModel>();

            return JsonSerializer.Deserialize<List<MapPointViewModel>>(json) ?? new List<MapPointViewModel>();
        }

        private static List<MapPointViewModel> GenerateRandomPoints()
        {
            double baseLat = 55.751244;
            double baseLon = 37.618423;

            var points = new List<MapPointViewModel>
            {
                new MapPointViewModel { Name = "Базовый Склад", Latitude = baseLat, Longitude = baseLon, Revenue = 0, IsBase = true }
            };

            if (AddressBook.Moscow.Count > 0)
            {
                // Случайная выборка реальных адресов Москвы из справочника
                var candidates = AddressBook.Moscow.ToList();
                int pointsCount = _random.Next(5, 8);

                for (int i = 0; i < pointsCount && candidates.Count > 0; i++)
                {
                    int pick = _random.Next(candidates.Count);
                    var address = candidates[pick];
                    candidates.RemoveAt(pick); // не повторяем адреса

                    decimal revenue = _random.Next(1, 36) * 1000m;

                    string addressLabel = address.Address.Replace("г. Москва, ", "");
                    points.Add(new MapPointViewModel
                    {
                        Name = $"Клиент: {addressLabel}",
                        Latitude = address.Latitude,
                        Longitude = address.Longitude,
                        Revenue = revenue,
                        IsVisited = false
                    });
                }
            }

            return points;
        }

        private static async Task<double[,]> BuildRoadDistanceMatrixAsync(List<MapPointViewModel> points)
        {
            int count = points.Count;
            var matrix = new double[count, count];

            for (int i = 0; i < count; i++)
            {
                for (int j = 0; j < count; j++)
                {
                    if (i == j)
                    {
                        matrix[i, j] = 0;
                    }
                    else
                    {
                        matrix[i, j] = await GetRoadDistanceKmAsync(points[i], points[j]);
                    }
                }
            }

            return matrix;
        }

        private static async Task<double> GetRoadDistanceKmAsync(MapPointViewModel p1, MapPointViewModel p2)
        {
            try
            {
                string p1Lon = p1.Longitude.ToString(CultureInfo.InvariantCulture);
                string p1Lat = p1.Latitude.ToString(CultureInfo.InvariantCulture);
                string p2Lon = p2.Longitude.ToString(CultureInfo.InvariantCulture);
                string p2Lat = p2.Latitude.ToString(CultureInfo.InvariantCulture);

                string url = $"https://router.project-osrm.org/route/v1/driving/{p1Lon},{p1Lat};{p2Lon},{p2Lat}?overview=false";

                using var response = await _httpClient.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(json);
                    var routes = doc.RootElement.GetProperty("routes");
                    if (routes.GetArrayLength() > 0)
                    {
                        double meters = routes[0].GetProperty("distance").GetDouble();
                        return meters / 1000.0;
                    }
                }
            }
            catch { }

            return HaversineDistanceKm(p1.Latitude, p1.Longitude, p2.Latitude, p2.Longitude) * 1.25;
        }

        public static double DijkstraShortestPath(double[,] graph, int source, int destination)
        {
            int n = graph.GetLength(0);
            double[] dist = new double[n];
            bool[] visited = new bool[n];

            for (int i = 0; i < n; i++)
            {
                dist[i] = double.MaxValue;
                visited[i] = false;
            }

            dist[source] = 0;

            for (int count = 0; count < n - 1; count++)
            {
                int u = -1;
                double min = double.MaxValue;

                for (int v = 0; v < n; v++)
                {
                    if (!visited[v] && dist[v] <= min)
                    {
                        min = dist[v];
                        u = v;
                    }
                }

                if (u == -1) break;
                visited[u] = true;

                for (int v = 0; v < n; v++)
                {
                    if (!visited[v] && graph[u, v] > 0 && dist[u] != double.MaxValue
                        && dist[u] + graph[u, v] < dist[v])
                    {
                        dist[v] = dist[u] + graph[u, v];
                    }
                }
            }

            return dist[destination];
        }

        private static List<int> FindMostProfitableRouteDijkstra(List<MapPointViewModel> points, double[,] distances)
        {
            var bestRoute = new List<int>();
            decimal bestProfit = 0;
            double bestDistance = 0;

            var currentRoute = new List<int>();
            var used = new bool[points.Count];
            used[0] = true;

            SearchRoutesDijkstra(0, 0, 0, currentRoute, used, points, distances, ref bestProfit, ref bestDistance, ref bestRoute);

            return bestRoute;
        }

        private static void SearchRoutesDijkstra(
            int currentPoint,
            double currentDistance,
            decimal currentRevenue,
            List<int> currentRoute,
            bool[] used,
            List<MapPointViewModel> points,
            double[,] distances,
            ref decimal bestProfit,
            ref double bestDistance,
            ref List<int> bestRoute)
        {
            double returnDistance = DijkstraShortestPath(distances, currentPoint, 0);
            double fullDistance = currentDistance + returnDistance;
            decimal tripCost = (decimal)fullDistance * CostPerKm;
            decimal profit = currentRevenue - tripCost;

            if (profit > bestProfit || (profit == bestProfit && fullDistance < bestDistance && currentRoute.Count > 0))
            {
                bestProfit = profit;
                bestDistance = fullDistance;
                bestRoute = new List<int>(currentRoute);
            }

            for (int nextPoint = 1; nextPoint < points.Count; nextPoint++)
            {
                if (used[nextPoint]) continue;

                used[nextPoint] = true;
                currentRoute.Add(nextPoint);

                double legDist = DijkstraShortestPath(distances, currentPoint, nextPoint);

                SearchRoutesDijkstra(
                    nextPoint,
                    currentDistance + legDist,
                    currentRevenue + points[nextPoint].Revenue,
                    currentRoute,
                    used,
                    points,
                    distances,
                    ref bestProfit,
                    ref bestDistance,
                    ref bestRoute
                );

                currentRoute.RemoveAt(currentRoute.Count - 1);
                used[nextPoint] = false;
            }
        }

        private static double HaversineDistanceKm(double lat1, double lon1, double lat2, double lon2)
        {
            const double r = 6371.0;
            double dLat = ToRadians(lat2 - lat1);
            double dLon = ToRadians(lon2 - lon1);
            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                       Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                       Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            return r * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        }

        private static double ToRadians(double deg) => deg * Math.PI / 180.0;

        #endregion
    }

    public class MapPointViewModel
    {
        public string Name { get; set; } = "";
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public decimal Revenue { get; set; }
        public bool IsBase { get; set; }
        public bool IsVisited { get; set; }
        public int RouteOrder { get; set; }
    }

    public class RouteStepViewModel
    {
        public string Status { get; set; } = "";
        public string Name { get; set; } = "";
        public decimal Revenue { get; set; }
        public double LegDistanceKm { get; set; }
        public decimal LegCost { get; set; }   // Затраты на этот участок
        public decimal LegProfit { get; set; } // Прибыль за этот этап
    }
}