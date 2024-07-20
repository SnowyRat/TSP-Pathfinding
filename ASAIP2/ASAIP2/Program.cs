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
using DocumentFormat.OpenXml.Drawing.Charts;
using DocumentFormat.OpenXml.VariantTypes;
using LiveCharts.Defaults;

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
        private static Random _rand = new Random();
        private static List<City> _cities;
        private static double[,] _matrix;

        public static double Fitness(int[] tour, double[,] matrix)
        {
            double cost = 0.0;
            for (int i = 0; i < tour.Length - 1; i++)
            {
                double segmentCost = matrix[tour[i], tour[i + 1]];
                if (segmentCost == double.PositiveInfinity) return double.PositiveInfinity;
                cost += segmentCost;
            }
            double returnCost = matrix[tour[tour.Length - 1], tour[0]];
            if (returnCost == double.PositiveInfinity) return double.PositiveInfinity;
            cost += returnCost;
            return cost;
        }

        public static List<int[]> InitializePopulation(int populationSize, double[,] matrix)
        {
            List<int[]> population = new List<int[]>();
            int n = matrix.GetLength(0);

            while (population.Count < populationSize)
            {
                int[] tour = GenerateRandomTour(n);
                if (FixTour(tour, matrix) && Fitness(tour, matrix) < double.PositiveInfinity)
                {
                    population.Add(tour);
                }
            }

            return population;
        }

        private static int[] GenerateRandomTour(int n)
        {
            return Enumerable.Range(0, n).OrderBy(x => _rand.Next()).ToArray();
        }

        private static bool FixTour(int[] tour, double[,] matrix)
        {
            int n = matrix.GetLength(0);

            for (int i = 0; i < n - 1; i++)
            {
                if (matrix[tour[i], tour[i + 1]] == double.PositiveInfinity)
                {
                    // Find a valid segment to swap
                    bool fixedSegment = false;
                    for (int j = i + 2; j < n; j++)
                    {
                        if (matrix[tour[i], tour[j]] != double.PositiveInfinity && matrix[tour[j], tour[i + 1]] != double.PositiveInfinity)
                        {
                            // Swap to fix the segment
                            int temp = tour[i + 1];
                            tour[i + 1] = tour[j];
                            tour[j] = temp;
                            fixedSegment = true;
                            break;
                        }
                    }

                    if (!fixedSegment)
                    {
                        // No valid segment found, return false
                        return false;
                    }
                }
            }

            return matrix[tour[n - 1], tour[0]] != double.PositiveInfinity;
        }

        public static int[] SelectParent(List<int[]> population, double[,] matrix)
        {
            Random rand = new Random();
            int tournamentSize = 5;
            List<int[]> tournament = new List<int[]>();

            for (int i = 0; i < tournamentSize; i++)
            {
                tournament.Add(population[rand.Next(population.Count)]);
            }

            return tournament.OrderBy(t => Fitness(t, matrix)).First();
        }

        public static int[] Crossover(int[] parent1, int[] parent2, double[,] matrix)
        {
            Random rand = new Random();
            int length = parent1.Length;
            int[] child = new int[length];
            bool[] visited = new bool[length];

            int start = rand.Next(length);
            int end = rand.Next(length);

            if (start > end)
            {
                int temp = start;
                start = end;
                end = temp;
            }

            for (int i = start; i <= end; i++)
            {
                child[i] = parent1[i];
                visited[child[i]] = true;
            }

            int currentIndex = (end + 1) % length;

            for (int i = 0; i < length; i++)
            {
                int gene = parent2[(end + 1 + i) % length];
                if (!visited[gene])
                {
                    child[currentIndex] = gene;
                    currentIndex = (currentIndex + 1) % length;
                }
            }

            // Fix any infeasible paths
            if (!FixTour(child, matrix) || Fitness(child, matrix) == double.PositiveInfinity)
            {
                return parent1;
            }

            return child;
        }

        public static void Mutate(int[] tour, double[,] matrix)
        {
            Random rand = new Random();
            int index1 = rand.Next(tour.Length);
            int index2 = rand.Next(tour.Length);

            int temp = tour[index1];
            tour[index1] = tour[index2];
            tour[index2] = temp;

            // Fix any infeasible paths
            if (!FixTour(tour, matrix) || Fitness(tour, matrix) == double.PositiveInfinity)
            {
                // Revert mutation
                tour[index2] = tour[index1];
                tour[index1] = temp;
            }
        }
        public static double[,] CreateAdjacencyMatrix(List<City> cities, List<Route> routes)
        {
            int n = cities.Count;
            double[,] matrix = new double[n, n];
            Dictionary<string, int> cityIndex = cities.Select((city, index) => new { city.name, index }).ToDictionary(x => x.name, x => x.index);

            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    matrix[i, j] = double.PositiveInfinity;
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
        public static void Main(string[] args)
        {
            List<Route> routes = LoadData("..\\..\\..\\places.xlsx");
            List<City> cities = LoadCityGoodness("..\\..\\..\\eval.xlsx");
            var sampler = new CityRouteSampler();
            var sampledCities = sampler.SampleConnectedCities(cities, routes, 40);
            var filteredRoutes = sampler.FilterRoutes(routes, sampledCities);
            _cities = sampledCities;
            double[,] matrix = CreateAdjacencyMatrix(sampledCities, filteredRoutes);
            double[,] distanceMatrix = CreateAdjacencyMatrix(cities, routes);

            _matrix = CreateAdjacencyMatrix(_cities, filteredRoutes);

            int populationSize = 10;
            int generations = 5000;
            double mutationRate = 0.05;
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            Console.WriteLine("pop creating");
            List<int[]> population = InitializePopulation(populationSize, _matrix);
            Console.WriteLine("pop created");
            for (int generation = 0; generation < generations; generation++)
            {
                List<int[]> newPopulation = new List<int[]>();

                for (int i = 0; i < populationSize; i++)
                {
                    int[] parent1 = SelectParent(population, _matrix);
                    int[] parent2 = SelectParent(population, _matrix);

                    int[] child = Crossover(parent1, parent2, _matrix);

                    if (_rand.NextDouble() < mutationRate)
                    {
                        Mutate(child, _matrix);
                    }

                    newPopulation.Add(child);
                }

                population = newPopulation;

                if (generation % 100 == 0)
                {
                    int[] bestTour = population.OrderBy(t => Fitness(t, _matrix)).First();
                    double bestFitness = Fitness(bestTour, _matrix);
                    if (bestFitness < double.PositiveInfinity)
                    {
                        Console.WriteLine($"Generation {generation}: {bestFitness}");
                    }
                    else
                    {
                        Console.WriteLine($"Generation {generation}: No valid path found.");
                    }
                }
            }

            int[] finalTour = population.OrderBy(t => Fitness(t, _matrix)).First();
            double finalFitness = Fitness(finalTour, _matrix);
            if (finalFitness < double.PositiveInfinity)
            {
                Console.WriteLine("Best tour found:");
                Console.WriteLine(string.Join(" -> ", finalTour.Select(i => _cities[i].name)));
                Console.WriteLine($"Cost: {finalFitness}");
            }
            else
            {
                Console.WriteLine("No valid tour found.");
            }
            stopwatch.Stop();
            Console.WriteLine("Time taken: " + stopwatch.ElapsedMilliseconds + "ms");
            AssignRandomCoordinates(_cities);
            SaveTourToFile(finalTour, _cities, "..\\..\\..\\final_tour.csv");


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