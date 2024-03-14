using NeoCortex;
using NeoCortexApi;
using NeoCortexApi.Encoders;
using NeoCortexApi.Entities;
using NeoCortexApi.Network;
using NeoCortexApi.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Drawing;
using System.IO;
using System.Linq;

namespace NeoCortexApiSample
{
    /// <summary>
    /// Implements an experiment that demonstrates how to learn spatial patterns.
    /// SP will learn every presented input in multiple iterations.
    /// </summary>
    public class SpatialPatternLearning
    {
        private const int OutImgSize = 1024;
        public void Run()
        {
            Console.WriteLine($"Hello NeocortexApi! Experiment {nameof(SpatialPatternLearning)}");

            // Used as a boosting parameters
            // that ensure homeostatic plasticity effect.
            double minOctOverlapCycles = 1.0;
            double maxBoost = 5.0;

            // We will use 200 bits to represent an input vector (pattern).
            int inputBits = 200;

            // We will build a slice of the cortex with the given number of mini-columns
            int numColumns = 1024;

            //
            // This is a set of configuration parameters used in the experiment.
            HtmConfig cfg = new HtmConfig(new int[] { inputBits }, new int[] { numColumns })
            {
                CellsPerColumn = 10,
                MaxBoost = maxBoost,
                DutyCyclePeriod = 100,
                MinPctOverlapDutyCycles = minOctOverlapCycles,

                GlobalInhibition = false,
                NumActiveColumnsPerInhArea = 0.02 * numColumns,
                PotentialRadius = (int)(0.15 * inputBits),
                LocalAreaDensity = -1,
                ActivationThreshold = 10,
                
                MaxSynapsesPerSegment = (int)(0.01 * numColumns),
                Random = new ThreadSafeRandom(42),
                StimulusThreshold=10,
            };

            double max = 100;

            //
            // This dictionary defines a set of typical encoder parameters.
            Dictionary<string, object> settings = new Dictionary<string, object>()
            {
                { "W", 15},
                { "N", inputBits},
                { "Radius", -1.0},
                { "MinVal", 0.0},
                { "Periodic", false},
                { "Name", "scalar"},
                { "ClipInput", false},
                { "MaxVal", max}
            };


            EncoderBase encoder = new ScalarEncoder(settings);

            //
            // We create here 100 random input values.
            List<double> inputValues = new List<double>();

            for (int i = 0; i < (int)max; i++)
            {
                inputValues.Add((double)i);
            }

            var sp = RunExperiment(cfg, encoder, inputValues);

            RunRustructuringExperiment(sp, encoder, inputValues);
        }

       

        /// <summary>
        /// Implements the experiment.
        /// </summary>
        /// <param name="cfg"></param>
        /// <param name="encoder"></param>
        /// <param name="inputValues"></param>
        /// <returns>The trained bersion of the SP.</returns>
        private static SpatialPooler RunExperiment(HtmConfig cfg, EncoderBase encoder, List<double> inputValues)
        {
            // Creates the htm memory.
            var mem = new Connections(cfg);

            bool isInStableState = false;

            //
            // HPC extends the default Spatial Pooler algorithm.
            // The purpose of HPC is to set the SP in the new-born stage at the begining of the learning process.
            // In this stage the boosting is very active, but the SP behaves instable. After this stage is over
            // (defined by the second argument) the HPC is controlling the learning process of the SP.
            // Once the SDR generated for every input gets stable, the HPC will fire event that notifies your code
            // that SP is stable now.
            HomeostaticPlasticityController hpa = new HomeostaticPlasticityController(mem, inputValues.Count * 40,
                (isStable, numPatterns, actColAvg, seenInputs) =>
                {
                    // Event should only be fired when entering the stable state.
                    // Ideal SP should never enter unstable state after stable state.
                    if (isStable == false)
                    {
                        Debug.WriteLine($"INSTABLE STATE");
                        // This should usually not happen.
                        isInStableState = false;
                    }
                    else
                    {
                        Debug.WriteLine($"STABLE STATE");
                        // Here you can perform any action if required.
                        isInStableState = true;
                    }
                });

            // It creates the instance of Spatial Pooler Multithreaded version.
            SpatialPooler sp = new SpatialPooler(hpa);
            //sp = new SpatialPoolerMT(hpa);

            // Initializes the 
            sp.Init(mem, new DistributedMemory() { ColumnDictionary = new InMemoryDistributedDictionary<int, NeoCortexApi.Entities.Column>(1) });

            // mem.TraceProximalDendritePotential(true);

            // It creates the instance of the neo-cortex layer.
            // Algorithm will be performed inside of that layer.
            CortexLayer<object, object> cortexLayer = new CortexLayer<object, object>("L1");

            // Add encoder as the very first module. This model is connected to the sensory input cells
            // that receive the input. Encoder will receive the input and forward the encoded signal
            // to the next module.
            cortexLayer.HtmModules.Add("encoder", encoder);

            // The next module in the layer is Spatial Pooler. This module will receive the output of the
            // encoder.
            cortexLayer.HtmModules.Add("sp", sp);

            double[] inputs = inputValues.ToArray();

            // Will hold the SDR of every inputs.
            Dictionary<double, int[]> prevActiveCols = new Dictionary<double, int[]>();

            // Will hold the similarity of SDKk and SDRk-1 fro every input.
            Dictionary<double, double> prevSimilarity = new Dictionary<double, double>();

            //
            // Initiaize start similarity to zero.
            foreach (var input in inputs)
            {
                prevSimilarity.Add(input, 0.0);
                prevActiveCols.Add(input, new int[0]);
            }

            // Learning process will take 1000 iterations (cycles)
            int maxSPLearningCycles = 1000;

            int numStableCycles = 0;

            for (int cycle = 0; cycle < maxSPLearningCycles; cycle++)
            {
                Debug.WriteLine($"Cycle  ** {cycle} ** Stability: {isInStableState}");

                //
                // This trains the layer on input pattern.
                foreach (var input in inputs)
                {
                    double similarity;

                    // Learn the input pattern.
                    // Output lyrOut is the output of the last module in the layer.
                    // 
                    var lyrOut = cortexLayer.Compute((object)input, true) as int[];

                    // This is a general way to get the SpatialPooler result from the layer.
                    var activeColumns = cortexLayer.GetResult("sp") as int[];

                    var actCols = activeColumns.OrderBy(c => c).ToArray();

                    similarity = MathHelpers.CalcArraySimilarity(activeColumns, prevActiveCols[input]);

                    Debug.WriteLine($"[cycle={cycle.ToString("D4")}, i={input}, cols=:{actCols.Length} s={similarity}] SDR: {Helpers.StringifyVector(actCols)}");

                    prevActiveCols[input] = activeColumns;
                    prevSimilarity[input] = similarity;
                }

                if (isInStableState)
                {
                    numStableCycles++;
                }

                if (numStableCycles > 5)
                    break;
            }

            return sp;
        }

        //private void RunRustructuringExperiment(SpatialPooler sp, EncoderBase encoder, List<double> inputValues)
        //{
        //    //filePath for generating heatmaps


        //    //create a list for threshold permanence values
        //     Dictionary<int, double> allPermanenceValues = new Dictionary<int, double>();


        //    foreach (var input in inputValues)
        //    {
        //        var inpSdr = encoder.Encode(input);

        //        var actCols = sp.Compute(inpSdr, false);

        //       var probabilities = sp.Reconstruct(actCols);

        //        //print probabilities of active cols
        //        foreach (var kvp in probabilities)
        //        {
        //            Debug.WriteLine($"Column: {kvp.Key}, Probability: {kvp.Value}");
        //        }

        //        //getting keys, values of reconstructed Probabilities
        //        Dictionary<int, double> reconstructedProbabilities = sp.Reconstruct(actCols);


        //         foreach (var keys in reconstructedProbabilities)
        //        {
        //            int inputColumns = keys.Key;

        //            double permanceValues = keys.Value;

        //            allPermanenceValues[inputColumns] = permanceValues;

        //        }

        //         //max cols 200
        //         // make unactive cols values: 0

        //        for (int inputColumns = 0; inputColumns < 200; inputColumns++)
        //        {

        //            if (!reconstructedProbabilities.ContainsKey(inputColumns))
        //            {

        //                allPermanenceValues[inputColumns] = 0.0;

        //            }

        //        }

        //        //convert into list from dictionary
        //        List<double> permanenceValuesList = allPermanenceValues.Values.ToList();


        //        //xx stores the Threshold Probabilities
        //        //threshold value(0.52) has been selected randomly
        //        var thresholdValues = Helpers.ThresholdProbabilities(permanenceValuesList, 0.52);


        //        Debug.WriteLine($"Input: {input} SDR: {Helpers.StringifyVector(actCols)}");

        //        Debug.WriteLine($"Input: {input} SDR: {Helpers.StringifyVector(actCols)}");


        //        //print all allPermanence Values

        //        foreach (var keys in allPermanenceValues)
        //        {
        //            Debug.WriteLine($"AllPermanence Column: {keys.Key}, AllPermanence Values: {keys.Value}");
        //        }

        //        //print threshold values
        //        int temp = 0;
        //        foreach (var t in thresholdValues)
        //        {
        //            Debug.WriteLine($"threshold index:{temp} , threshold value:{t}");
        //            temp = temp + 1;
        //        }
        //        var colDims = new int[] { 64, 64 };
        //        //NeoCortexUtils.Draw1dHeatmaps(thresholdValues, filePath,1024,1024,60,127,20);
        //        // GeneralUnitTests.TestHeatmapCreation(thresholdValues);
        //        List<double[,]> twoDimenArray = ArrayUtils.Make2DArray<int>(thresholdValues, colDims[0], colDims[1]);
        //        twoDimenArray = ArrayUtils.Transpose(twoDimenArray);
        //        List<double[,]> arrays = new List<double[,]>();
        //        arrays.Add(twoDimenArray);
        //        arrays.Add(ArrayUtils.Transpose(ArrayUtils.Make2DArray<int>(thresholdValues, (int)Math.Sqrt(thresholdValues.Length), (int)Math.Sqrt(thresholdValues.Length))));
        //        string outFolder = $"{RunRustructuringExperiment}";

        //        Directory.CreateDirectory(outFolder);
        //        string outputImage = $"{outFolder}\\{input}";

        //        NeoCortexUtils.DrawHeatmaps(arrays, outputImage);

        //    }







        //    Console.ReadKey();
        //}

        private void RunRustructuringExperiment(SpatialPooler sp, EncoderBase encoder, List<double> inputValues)
        {
            foreach (var input in inputValues)
            {
                var inpSdr = encoder.Encode(input);
                var actCols = sp.Compute(inpSdr, false);
                var probabilities = sp.Reconstruct(actCols);

                // Create a list for threshold permanence values
                Dictionary<int, double> allPermanenceValues = new Dictionary<int, double>();

                // Get keys, values of reconstructed Probabilities
                foreach (var kvp in probabilities)
                {
                    allPermanenceValues[kvp.Key] = kvp.Value;
                }

                // Fill in missing keys with 0
                for (int inputColumns = 0; inputColumns < 200; inputColumns++)
                {
                    if (!probabilities.ContainsKey(inputColumns))
                    {
                        allPermanenceValues[inputColumns] = 0.0;
                    }
                }

                //print all allPermanence Values

                foreach (var keys in allPermanenceValues)
                {
                    Debug.WriteLine($"AllPermanence Column: {keys.Key}, AllPermanence Values: {keys.Value}");
                }

                // Convert dictionary values to list
                List<double> permanenceValuesList = allPermanenceValues.Values.ToList();

                // Get threshold values
                var thresholdValues = Helpers.ThresholdProbabilities(permanenceValuesList, 0.52);

                var colDims = new int[] { 64, 64 };

                List<double[,]> arrays = new List<double[,]>();
                arrays.Add(ArrayUtils.Make2DArray(thresholdValues, colDims[0], colDims[1]));

                string outFolder = $"{nameof(RunRustructuringExperiment)}";
                Directory.CreateDirectory(outFolder);
                string outputImage = $"{outFolder}\\{input}";


                // print all threshold values
                int temp = 0;
                foreach (var t in thresholdValues)
                {
                    Debug.WriteLine($"threshold index:{temp} , threshold value:{t}");
                    temp = temp + 1;
                }

                // calling drawheatmaps class
                NeoCortexUtils.DrawHeatmaps(arrays, $"{outputImage}_threshold_heatmap.png", 1024, 1024, 200, 127, 20);
            }

            Console.ReadKey();
        }


    }
}
