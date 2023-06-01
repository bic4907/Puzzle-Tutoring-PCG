using System;
using System.Collections;
using System.Collections.Generic;
using Unity.MLAgentsExamples;
using Unity.MLAgents.Integrations.Match3;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Unity.MLAgentsExamples
{

    
    public class GeneticAlgorithm
    {
        private static GeneticAlgorithm _Instance = null;

        public static GeneticAlgorithm Instance { get {
            if (_Instance == null)
            {
                _Instance = new GeneticAlgorithm();
            }
            return _Instance;
        }}

     
        private int ChromosomeLength = 0;
        private int PopulationSize = 100;

        private SelectMethod m_SelectMethod = SelectMethod.RouletteWheel;


        public GeneticAlgorithm()
        {

        }

        public bool FillEmpty(Match3Board board, SkillKnowledge knowledge, int playerDepthLimit = 1)
        {
            int _emptyCellCount = board.GetEmptyCellCount();
            ChromosomeLength = _emptyCellCount;
        }

   private List<Chromosome> Crossover(List<Chromosome> offspring, double prob = 1)
    {
        offspring = offspring.Select(x => new Chromosome(x.Genes)).ToList();
        List<int> idx = Enumerable.Range(0, offspring.Count).ToList();
        List<Chromosome> shuffledOffspring = new List<Chromosome>();
        Shuffle(idx.ToArray());

        foreach (int i in idx)
        {
            shuffledOffspring.Add(offspring[i]);
        }

        offspring = shuffledOffspring;

        const int RECT_SIZE = 4;
        const int MAX_RANGE = 10 - RECT_SIZE;

        int median = offspring.Count / 2;

        for (int offspring_i = 0; offspring_i < median; offspring_i++)
        {
            if (new Random().NextDouble() <= prob)
            {
                int x = new Random().Next(0, MAX_RANGE);
                int y = new Random().Next(0, MAX_RANGE);

                int[,] individual_1 = offspring[offspring_i].Genes.To2DArray(10, 10);
                int[,] individual_2 = offspring[offspring_i + median].Genes.To2DArray(10, 10);

                for (int i = y; i < y + RECT_SIZE; i++)
                {
                    for (int j = x; j < x + RECT_SIZE; j++)
                    {
                        int temp = individual_1[i, j];
                        individual_1[i, j] = individual_2[i, j];
                        individual_2[i, j] = temp;
                    }
                }

                offspring[offspring_i].Genes = individual_1.Flatten();
                offspring[offspring_i + median].Genes = individual_2.Flatten();
            }
        }

        return offspring;
    }

    private List<Chromosome> Mutation(List<Chromosome> offspring, double prob = 0.01)
    {
        offspring = offspring.Select(x => new Chromosome(x.Genes)).ToList();

        double[] weights = { 1.0, 1.0, 1.0, 1.0, 1.0, 1.0 };

        for (int individual_index = 0; individual_index < offspring.Count; individual_index++)
        {
            for (int gene_index = 0; gene_index < _geneSize; gene_index++)
            {
                if (new Random().NextDouble() <= prob)
                {
                    int rand_int = WeightedChoice(Enumerable.Range(0, 6).ToList(), weights);

                    offspring[individual_index].Genes[gene_index] = rand_int;
                }
            }
        }

        return offspring;
    }

    private double GetFitness(Chromosome individual)
    {
        return _fitnessFunc(individual);
    }

    private List<Chromosome> Selection()
    {
        if (m_SelectMethod == SelectMethod.RouletteWheel)
        {
            List<Chromosome> offspring = _population.Select(x => new Chromosome(x.Genes)).ToList();
            List<Chromosome> newPopulation = new List<Chromosome>();

            double[] fitnessValues = offspring.Select(GetFitness).ToArray();
            double minFitness = fitnessValues.Min();
            double maxFitness = fitnessValues.Max();
            fitnessValues = fitnessValues.Select(f => (f - minFitness) / (maxFitness - minFitness)).ToArray();

            double sumFitness = fitnessValues.Sum();
            double[] ratios = fitnessValues.Select(f => f / sumFitness).ToArray();

            for (int i = 0; i < _populationSize; i++)
            {
                double x = new Random().NextDouble();
                int k = 0;

                while (k < _populationSize - 1 && x > (ratios.Take(k + 1).Sum()))
                {
                    k++;
                }

                newPopulation.Add(offspring[k]);
            }

            return newPopulation;
        }
        else if (m_SelectMethod == SelectMethod.Tournament)
        {
            List<Chromosome> newPopulation = new List<Chromosome>();

            for (int candidate1 = 0; candidate1 < _populationSize; candidate1++)
            {
                int candidate2 = new Random().Next(0, _populationSize);

                Chromosome individual_1 = _population[candidate1];
                Chromosome individual_2 = _population[candidate2];

                double fitness_1 = GetFitness(individual_1);
                double fitness_2 = GetFitness(individual_2);

                if (fitness_1 > fitness_2)
                {
                    newPopulation.Add(individual_1);
                }
                else
                {
                    newPopulation.Add(individual_2);
                }
            }

            return newPopulation;
        }
        else
        {
            throw new Exception("Unknown selection method");
        }
    }

    private List<Chromosome> Sorting(List<Chromosome> offspring)
    {
        double[] fitnessValue = Enumerable.Range(0, 200).Select(i => GetFitness(offspring[i])).ToArray();
        List<double> sortedFitness = fitnessValue.OrderByDescending(f => f).ToList();

        List<Chromosome> sortedOffspring = new List<Chromosome>();

        for (int i = 0; i < offspring.Count; i++)
        {
            for (int j = 0; j < offspring.Count; j++)
            {
                if (sortedFitness[i] == fitnessValue[j])
                {
                    sortedOffspring.Add(offspring[j]);
                    break;
                }
            }
        }

        return sortedOffspring;
    }

    public void Evolution(int generation)
    {
        List<Chromosome> offspring2 = _population.Select(x => new Chromosome(x.Genes)).ToList();

        offspring2 = Sorting(offspring2);
        List<Chromosome> offspring = Selection();
        offspring = Crossover(offspring, 0.9);
        offspring = Mutation(offspring, 0.02);

        offspring = Sorting(offspring);
        for (int k = generation / 5; k < 200; k++)
        {
            offspring2[k] = new Chromosome(offspring[k - generation / 5].Genes);
        }
        offspring = offspring2.Select(x => new Chromosome(x.Genes)).ToList();
        _population = offspring;
    }

    public List<Chromosome> Population
    {
        get { return _population; }
    }

    public double GetAverageFitness()
    {
        double[] fitnessValues = _population.Select(GetFitness).ToArray();
        double sumFitness = fitnessValues.Sum();
        return sumFitness / _populationSize;
    }

    public double GetBestFitness()
    {
        double[] fitnessValues = _population.Select(GetFitness).ToArray();
        return fitnessValues.Max();
    }

    public int GetNthBestIndex(int n)
    {
        double[] fitnessValues = _population.Select(GetFitness).ToArray();
        int[] indices = Enumerable.Range(0, _populationSize).ToArray();
        Array.Sort(fitnessValues, indices);
        return indices[_populationSize - n];
    }

    public Chromosome GetBestIndividual()
    {
        int bestIndex = GetNthBestIndex(1);
        return _population[bestIndex];
    }

    private static void Shuffle<T>(T[] array)
    {
        Random rng = new Random();
        int n = array.Length;
        while (n > 1)
        {
            int k = rng.Next(n--);
            T temp = array[n];
            array[n] = array[k];
            array[k] = temp;
        }
    }

    private static T WeightedChoice<T>(List<T> population, double[] weights)
    {
        double totalWeight = weights.Sum();
        double randomValue = new Random().NextDouble() * totalWeight;
        double cumulativeWeight = 0;

        for (int i = 0; i < population.Count; i++)
        {
            cumulativeWeight += weights[i];
            if (randomValue <= cumulativeWeight)
            {
                return population[i];
            }
        }

        return population.Last();
    }

        public enum SelectMethod 
        {
            RouletteWheel,
            Tournament
        }


    }

    public class Chromosome
    {
        public List<int> Genes { get; set; }

        public Chromosome(List<int> genes)
        {
            Genes = genes;
        }
    }

    public static class Extensions
    {
        public static int[,] To2DArray(this List<int> list, int rows, int cols)
        {
            int[,] array = new int[rows, cols];
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    array[i, j] = list[i * cols + j];
                }
            }
            return array;
        }

        public static List<int> Flatten(this int[,] array)
        {
            List<int> list = new List<int>();
            int rows = array.GetLength(0);
            int cols = array.GetLength(1);
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    list.Add(array[i, j]);
                }
            }
            return list;
        }
    }


}