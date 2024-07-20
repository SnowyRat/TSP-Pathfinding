using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClosedXML.Excel;
using DocumentFormat.OpenXml.Bibliography;

namespace ASAIP1
{
    public class CityRouteSampler
    {
        private Random rand = new Random(1234);

        public List<City> SampleConnectedCities(List<City> allCities, List<Route> allRoutes, int sampleSize)
        {
            if (allCities.Count == 0 || sampleSize <= 0) return new List<City>();

            // Start from a random city
            City startCity = allCities[rand.Next(allCities.Count)];

            // Prepare for BFS
            Queue<City> queue = new Queue<City>();
            HashSet<string> visited = new HashSet<string>();
            List<City> sampledCities = new List<City>();

            queue.Enqueue(startCity);
            visited.Add(startCity.name);

            // Perform BFS until we collect enough cities or exhaust the graph
            while (queue.Count > 0 && sampledCities.Count < sampleSize)
            {
                City current = queue.Dequeue();
                sampledCities.Add(current);

                // Get all neighboring cities
                var neighbors = allRoutes.Where(r => r.origin == current.name && !visited.Contains(r.destination))
                                         .Select(r => allCities.Find(c => c.name == r.destination));

                foreach (var neighbor in neighbors)
                {
                    if (!visited.Contains(neighbor.name))
                    {
                        queue.Enqueue(neighbor);
                        visited.Add(neighbor.name);
                        if (sampledCities.Count >= sampleSize) break;
                    }
                }
            }

            return sampledCities;
        }

        public List<Route> FilterRoutes(List<Route> allRoutes, List<City> sampledCities)
        {
            HashSet<string> cityNames = new HashSet<string>(sampledCities.Select(c => c.name));
            return allRoutes.Where(route => cityNames.Contains(route.origin) && cityNames.Contains(route.destination)).ToList();
        }

        public void PrintCityConnections(List<City> cities, List<Route> routes)
        {
            foreach (var city in cities)
            {
                var outgoingRoutes = routes.Where(r => r.origin == city.name).ToList();
                if (outgoingRoutes.Count > 0)
                {
                    Console.WriteLine($"City {city.name} has connections to:");
                    foreach (var route in outgoingRoutes)
                    {
                        Console.WriteLine($"  -> {route.destination} (Cost: {route.cost}, Time: {route.timeTaken})");
                    }
                }
                else
                {
                    Console.WriteLine($"City {city.name} has no outgoing connections.");
                }
            }
        }
        public void GenerateGraphDOT(List<City> cities, List<Route> routes, string filePath)
        {
            using (StreamWriter sw = new StreamWriter(filePath))
            {
                sw.WriteLine("digraph CityConnections {");
                sw.WriteLine("  node [shape=circle];");

                foreach (var city in cities)
                {
                    var outgoingRoutes = routes.Where(r => r.origin == city.name).ToList();
                    if (outgoingRoutes.Count > 0)
                    {
                        foreach (var route in outgoingRoutes)
                        {
                            sw.WriteLine($"  \"{city.name}\" -> \"{route.destination}\" [label=\"Cost: {route.cost}, Time: {route.timeTaken}\"];");
                        }
                    }
                    else
                    {
                        sw.WriteLine($"  \"{city.name}\";");
                    }
                }

                sw.WriteLine("}");
            }
        }
        public void RenderGraph(string dotPath, string imagePath)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = "dot",
                Arguments = $"-Tpng {dotPath} -o {imagePath}",
                UseShellExecute = false
            };

            using (Process process = Process.Start(startInfo))
            {
                process.WaitForExit();
            }
        }
    }
    public class Route
    {
        public string origin { get; set; }
        public string destination { get; set; }
        public double timeTaken { get; set; }
        public double cost { get; set; }
        public double goodness { get; set; }
    }
    public class City
    {
        public string name { get; set; }
        public int index { get; set; }
        public double goodness { get; set; }
    }
    public class GFG
    {

        int N = 4;
        public int[] final_path;
        bool[] visited;
        public double final_res = Double.MaxValue;
        public GFG(int n) 
        {
            N = n;
            visited = new bool[N];
            final_path = new int[N + 1];
        }

        private void copyToFinal(int[] curr_path)
        {
            for (int i = 0; i < N; i++)
                final_path[i] = curr_path[i];
            final_path[N] = curr_path[0];
        }

        private double firstMin(double[,] adj, int i)
        {
            double min = Double.MaxValue;
            for (int k = 0; k < N; k++)
                if (adj[i, k] < min && i != k)
                    min = adj[i, k];
            return min;
        }

        private double secondMin(double[,] adj, int i)
        {
            double first = Double.MaxValue, second = Double.MaxValue;
            for (int j = 0; j < N; j++)
            {
                if (i == j)
                    continue;

                if (adj[i, j] <= first)
                {
                    second = first;
                    first = adj[i, j];
                }
                else if (adj[i, j] <= second && adj[i, j] != first)
                    second = adj[i, j];
            }
            return second;
        }

        private void TSPRec(double[,] adj, double curr_bound, double curr_weight, int level, int[] curr_path)
        {
            if (level == N)
            {
                if (adj[curr_path[level - 1], curr_path[0]] != 0)
                {
                    double curr_res = curr_weight + adj[curr_path[level - 1], curr_path[0]];
                    if (curr_res < final_res)
                    {
                        copyToFinal(curr_path);
                        final_res = curr_res;
                    }
                }
                return;
            }
            for (int i = 0; i < N; i++)
            {
                if (adj[curr_path[level - 1], i] != 0 && !visited[i])
                {
                    double temp = curr_bound; 
                    curr_weight += adj[curr_path[level - 1], i];
                    if (level == 1)
                        curr_bound -= ((firstMin(adj, curr_path[level - 1]) + firstMin(adj, i)) / 2);
                    else
                        curr_bound -= ((secondMin(adj, curr_path[level - 1]) + firstMin(adj, i)) / 2);
                    if (curr_bound + curr_weight < final_res)
                    {
                        curr_path[level] = i;
                        visited[i] = true;
                        TSPRec(adj, curr_bound, curr_weight, level + 1, curr_path);
                    }
                    curr_weight -= adj[curr_path[level - 1], i];
                    curr_bound = temp;
                    for (int j = 0; j <= level - 1; j++)
                        visited[j] = curr_path[j] >= 0;
                }
            }
        }

        public void TSP(double[,] adj)
        {
            int[] curr_path = new int[N + 1];
            double curr_bound = 0;

            for (int i = 0; i < curr_path.Length; i++)
                curr_path[i] = -1;
            for (int i = 0; i < visited.Length; i++)
                visited[i] = false;

            for (int i = 0; i < N; i++)
                curr_bound += (firstMin(adj, i) + secondMin(adj, i));

            curr_bound = (curr_bound == 1) ? curr_bound / 2 + 1 : curr_bound / 2;

            visited[0] = true;
            curr_path[0] = 0;

            TSPRec(adj, curr_bound, 0, 1, curr_path);
        }
    }
    internal class Program {
        public static double[,] CreateAdjacencyMatrix(List<City> cities, List<Route> routes)
        {
            int n = cities.Count;
            double[,] matrix = new double[n, n];
            Dictionary<string, int> cityIndex = cities.Select((city, index) => new { city.name, index })
                                                      .ToDictionary(x => x.name, x => x.index);

            // Initialize the matrix with a non-possible weight to indicate no connection
            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    matrix[i, j] = double.PositiveInfinity; // Use PositiveInfinity to denote no route
                }
            }

            foreach (Route route in routes)
            {
                if (cityIndex.ContainsKey(route.origin) && cityIndex.ContainsKey(route.destination))
                {
                    int originIndex = cityIndex[route.origin];
                    int destinationIndex = cityIndex[route.destination];
                    matrix[originIndex, destinationIndex] = route.cost;
                }
            }

            return matrix;
        }

        public static double[,] LoadDataadj(string filePath)
        {
            var workbook = new XLWorkbook(filePath);
            var worksheet = workbook.Worksheet(1);
            Dictionary<string, int> cityIndex = new Dictionary<string, int>();
            List<string> cities = new List<string>();

            foreach (var row in worksheet.RangeUsed().RowsUsed())
            {
                if (row.RowNumber() > 1)
                {
                    string cityFrom = row.Cell(1).GetValue<string>();
                    string cityTo = row.Cell(2).GetValue<string>();
                    if (!cityIndex.ContainsKey(cityFrom))
                    {
                        cityIndex[cityFrom] = cities.Count;
                        cities.Add(cityFrom);
                    }
                    if (!cityIndex.ContainsKey(cityTo))
                    {
                        cityIndex[cityTo] = cities.Count;
                        cities.Add(cityTo);
                    }
                }
            }

            int n = cities.Count;
            double[,] costMatrix = new double[n, n];
            for (int i = 0; i < n; i++)
                for (int j = 0; j < n; j++)
                    costMatrix[i, j] = (i == j) ? 0 : double.PositiveInfinity;

            foreach (var row in worksheet.RangeUsed().RowsUsed())
            {
                if (row.RowNumber() > 1)
                {
                    string cityFrom = row.Cell(1).GetValue<string>();
                    string cityTo = row.Cell(2).GetValue<string>();
                    string costString = row.Cell(4).GetString();
                    double cost;
                    if (double.TryParse(costString, out cost))
                    {
                        int indexFrom = cityIndex[cityFrom];
                        int indexTo = cityIndex[cityTo];
                        costMatrix[indexFrom, indexTo] = cost;
                    }
                }
            }

            return costMatrix;
        }

        public static List<Route> LoadData(string routeFilePath)
        {
            var routes = new List<Route>();

            using (var workbook = new XLWorkbook(routeFilePath))
            {
                var ws = workbook.Worksheet(1);
                var rows = ws.RangeUsed().RowsUsed();
                foreach (var row in rows)
                {
                    if (row.RowNumber() > 1)
                    {
                        var route = new Route
                        {
                            origin = row.Cell(1).GetValue<string>(),
                            destination = row.Cell(2).GetValue<string>(),
                            timeTaken = row.Cell(3).GetValue<int>(),
                            cost = row.Cell(4).GetValue<double>(),
                            //goodness = cityGoodness.TryGetValue(destination, out var goodness) ? goodness : 0
                        };
                        routes.Add(route);
                    }
                }
            }
            return routes;
        }

        private static List<City> LoadCityGoodness(string filePath)
        {
            var cities = new List<City>();
            int i = 0;
            using (var workbook = new XLWorkbook(filePath))
            {
                var ws = workbook.Worksheet(1);
                var rows = ws.RangeUsed().RowsUsed();
                foreach (var row in rows)
                {
                    if (row.RowNumber() > 1)
                    {
                        var city = new City
                        {
                            name = row.Cell(1).GetValue<string>(),
                            goodness = row.Cell(2).GetValue<double>(),
                            index = i
                        };
                        i++;
                        cities.Add(city);
                    }
                }
            }
            return cities;
        }
        public static void PrintMatrix(double[,] matrix)
        {
            int rows = matrix.GetLength(0);
            int cols = matrix.GetLength(1);

            int[] maxColWidths = new int[cols];
            for (int j = 0; j < cols; j++)
            {
                for (int i = 0; i < rows; i++)
                {
                    string element = matrix[i, j].ToString("0.00");
                    maxColWidths[j] = Math.Max(maxColWidths[j], element.Length);
                }
            }
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    string formattedElement = matrix[i, j].ToString("0.00");
                    Console.Write("|"+formattedElement.PadRight(maxColWidths[j] + 1));
                }
                Console.WriteLine();
            }
        }
        public static void Main(string[] args)
        {
            List<Route> routes = LoadData("..\\..\\places.xlsx");
            List<City> cities = LoadCityGoodness("..\\..\\eval.xlsx");
            var sampler = new CityRouteSampler();
            //su 15 dydziu uzima ~6s; 16 ~32s.
            var sampledCities = sampler.SampleConnectedCities(cities, routes, 14);
            var filteredRoutes = sampler.FilterRoutes(routes, sampledCities);
            sampler.PrintCityConnections(sampledCities, filteredRoutes);
            sampler.GenerateGraphDOT(sampledCities, filteredRoutes, "output.dot");
            double[,] matrix = CreateAdjacencyMatrix(sampledCities,filteredRoutes);
            PrintMatrix(matrix);
            //sampler.RenderGraph("output.dot", "output.png");
            Console.WriteLine("BRANCH");
            GFG gfg = new GFG(matrix.GetLength(0));
            gfg.TSP(matrix);
            Console.WriteLine($"Min Cost:{gfg.final_res}");
            for(int i = 0; i <= matrix.GetLength(0); i++)
            {
                Console.Write(gfg.final_path[i] + " ");
            }
            //BranchAndBound(matrix);

            // Now you can pass this matrix to your TSP Branch and Bound solver
        }
    }
}
