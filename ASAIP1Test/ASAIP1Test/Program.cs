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
using DocumentFormat.OpenXml.VariantTypes;

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
        public double x { get; set; }
        public double y { get; set; }
    }
    
    internal class Program
    {
        public static double[,] CreateAdjacencyMatrix(List<City> cities, List<Route> routes)
        {
            int n = cities.Count;
            double[,] matrix = new double[n, n];
            Dictionary<string, int> cityIndex = cities.Select((city, index) => new { city.name, index }).ToDictionary(x => x.name, x => x.index);

            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    matrix[i, j] = 0;
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
                    Console.Write("|" + formattedElement.PadRight(maxColWidths[j] + 1));
                }
                Console.WriteLine();
            }
        }
        // set up path
        static void SetFinalPath(int[] curPath)
        {
            lock (endPath)
            {
                for (int i = 0; i < N; i++)
                    endPath[i] = curPath[i];
                endPath[N] = curPath[0];
            }
        }

        public static double FirstMinEdge(double[,] adj, int i)
        {
            double min = Double.MaxValue;
            for (int k = 0; k < N; k++)
                if (adj[i, k] < min && i != k)
                    min = adj[i, k];
            return min;
        }

        public static double SecondMinEdge(double[,] adj, int i)
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

        public static void TSPRec(double[,] adj, double curBound, double curCost, int level, int[] curPath)
        {
            if (level == N)
            {
                if (adj[curPath[level - 1], curPath[0]] != 0)
                {
                    double curRes = curCost + adj[curPath[level - 1], curPath[0]];

                    if (curRes < endCost)
                    {
                        SetFinalPath(curPath);
                        endCost = curRes;
                    }
                }
                return;
            }

            for (int i = 0; i < N; i++)
            {
                if (adj[curPath[level - 1], i] != 0 && visited[i] == false)
                {
                    double temp = curBound;
                    curCost += adj[curPath[level - 1], i];

                    if (level == 1)
                        curBound -= (FirstMinEdge(adj, curPath[level - 1]) + FirstMinEdge(adj, i)) / 2;
                    else
                        curBound -= (SecondMinEdge(adj, curPath[level - 1]) + FirstMinEdge(adj, i)) / 2;

                    if (curBound + curCost < endCost)
                    {
                        curPath[level] = i;
                        visited[i] = true;
                        TSPRec(adj, curBound, curCost, level + 1, curPath);
                    }

                    curCost -= adj[curPath[level - 1], i];
                    curBound = temp;

                    Array.Fill(visited, false);
                    for (int j = 0; j <= level - 1; j++)
                        visited[curPath[j]] = true;
                }
            }
        }

        public static void Solve(double[,] adj)
        {
            N = adj.GetLength(0);
            endPath = new int[N + 1];
            visited = new bool[N];
            int[] curPath = new int[N + 1];
            double curBound = 0;
            Array.Fill(curPath, -1);
            Array.Fill(visited, false);

            for (int i = 0; i < N; i++)
                curBound += (FirstMinEdge(adj, i) + SecondMinEdge(adj, i));

            curBound = (curBound == 1) ? curBound / 2 + 1 : curBound / 2;
            visited[0] = true;
            curPath[0] = 0;

            List<int[]> firstLevelPaths = new List<int[]>();
            for (int i = 1; i < N; i++)
            {
                if (adj[0, i] != 0)
                {
                    int[] newPath = new int[N + 1];
                    Array.Copy(curPath, newPath, N + 1);
                    newPath[1] = i;
                    firstLevelPaths.Add(newPath);
                }
            }

            Parallel.ForEach(firstLevelPaths, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, path =>
            {
                double tempCost = adj[0, path[1]];
                double tempBound = curBound - (FirstMinEdge(adj, 0) + FirstMinEdge(adj, path[1])) / 2;
                bool[] tempVisited = new bool[N];
                Array.Copy(visited, tempVisited, N);
                tempVisited[path[1]] = true;
                TSPRec(adj, tempBound, tempCost, 2, path);
            });
        }
        static int N = 18;
        static int[] endPath = new int[N + 1];
        static bool[] visited = new bool[N];
        static double endCost = Double.MaxValue;
        public static void Main(string[] args)
        {
            List<Route> routes = LoadData("..\\..\\..\\places.xlsx");
            List<City> cities = LoadCityGoodness("..\\..\\..\\eval.xlsx");
            var sampler = new CityRouteSampler();
            var sampledCities = sampler.SampleConnectedCities(cities, routes, N);
            var filteredRoutes = sampler.FilterRoutes(routes, sampledCities);
            sampler.PrintCityConnections(sampledCities, filteredRoutes);
            sampler.GenerateGraphDOT(sampledCities, filteredRoutes, "output.dot");
            double[,] matrix = CreateAdjacencyMatrix(sampledCities, filteredRoutes);
            PrintMatrix(matrix);
            Stopwatch stopwatch = Stopwatch.StartNew();
            Solve(matrix);
            stopwatch.Stop();
            Console.WriteLine($"{stopwatch.ElapsedMilliseconds}");
            Console.WriteLine("Minimum cost : " + endCost);
            Console.Write("Path Taken : ");
            for (int i = 0; i <= N; i++)
            {
                Console.Write(endPath[i] + " ");
            }
            AssignRandomCoordinates(sampledCities);
            SaveTourToFile(endPath, sampledCities, "..\\..\\..\\final_tour.csv");
        }
        public static void SaveTourToFile(int[] tour, List<City> cities, string filePath)
        {
            using (StreamWriter sw = new StreamWriter(filePath))
            {
                sw.WriteLine("City,X,Y");
                foreach (int index in tour)
                {
                    City city = cities[index];
                    sw.WriteLine($"{city.name},{city.x},{city.y}");
                }
            }
        }
        public static void AssignRandomCoordinates(List<City> cities)
        {
            Random rand = new Random();
            HashSet<(double, double)> existingCoordinates = new HashSet<(double, double)>();

            foreach (var city in cities)
            {
                double x, y;
                do
                {
                    x = rand.NextDouble() * 100;
                    y = rand.NextDouble() * 100;
                } while (existingCoordinates.Contains((x, y)));

                city.x = x;
                city.y = y;
                existingCoordinates.Add((x, y));
            }
        }
    }
}
