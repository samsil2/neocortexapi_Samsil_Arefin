Group Name: Samsil_Arefin(Individual)
<br>
Main Branch link: [Here](https://github.com/samsil2/neocortexapi_Samsil_Arefin/tree/master)
<br>
Program Links: <br>
[SpatialPatternLearning](https://github.com/samsil2/neocortexapi_Samsil_Arefin/blob/master/source/Samples/NeoCortexApiSample/SpatialPatternLearning.cs)
<br>
[Reconstructor](https://github.com/samsil2/neocortexapi_Samsil_Arefin/blob/master/source/NeoCortexApi/SPSdrReconstructor.cs)
<br>
[Helpers](https://github.com/samsil2/neocortexapi_Samsil_Arefin/blob/master/source/NeoCortexApi/Helpers.cs)
<br>
[NeoCortexUtils](https://github.com/samsil2/neocortexapi_Samsil_Arefin/blob/master/source/NeoCortexUtils/NeoCortexUtils.cs)
<br>
<br>
<b>Abstract</b>:
<br>
<br>
This project entails implementing the visualization of permanence values reconstructed by the neocortexapi's Reconstruct() method, which acts as the inverse function of the Spatial Pooler (SP). The experiment focuses on utilizing 2D heatmaps to visualize integer values, where 1 corresponds to red and 0 to green. The objective is to enhance the existing DrawHeatmaps method in the NeocortexUtils class to facilitate this visualization technique. By effectively representing the reconstructed values through color-coded heatmaps, this project aims to provide a clear depiction of the reconstruction process and its outcomes.
<br>
<br>
Introduction: 
<br>
<br>
Permanence value refers to a parameter used in algorithms related to machine learning and artificial intelligence, particularly in the context of neural networks and algorithms inspired by the human brain's neocortex. In systems like the Spatial Pooler (SP), which is a component of Hierarchical Temporal Memory (HTM) models, permanence values represent the strength or connectivity between neurons.

In the SP algorithm, permanence values are assigned to each connection between input neurons and columns in the cortical region. These values typically range between 0 and 1. They signify the likelihood or strength of a connection being considered active or "permanent" during the learning process. Permanence values are updated based on input patterns and play a crucial role in determining which columns become active in response to specific input patterns.

In essence, permanence values regulate the adaptability and stability of connections within neural networks, influencing the network's ability to learn and recognize patterns over time. They are fundamental to the functioning of algorithms like HTM, which aim to replicate certain aspects of the brain's learning and memory mechanisms.


Methodology:
<br>
<br>
Our methodology revolves around the precise reconstruction of the original input, initiated by providing numerical values ranging from 0 to 99. The encoder transforms these numerical values into int[] arrays, representing arrays of 0s and 1s, each consisting of 200 bits post-encoding. These encoded arrays become the sole input for our experiment.

Fig: Methodology Flowchart
<br>
![flowchart](https://github.com/samsil2/neocortexapi_Samsil_Arefin/blob/master/source/MySEProject/Documentation/Flowchart.png)

<br> 
HTM: The encoded int[] arrays undergo transformation using the HTM Spatial Pooler, generating Sparse Distributed Representations (SDRs). This pivotal step lays the groundwork for further exploration.
<br> 
<b>Running Reconstruct Method:</b> <br> 

    private void RunRustructuringExperiment(SpatialPooler sp, EncoderBase encoder, List<double> inputValues)
        {
            foreach (var input in inputValues)
            {
                var inpSdr = encoder.Encode(input);
                var actCols = sp.Compute(inpSdr, false);
                var probabilities = sp.Reconstruct(actCols);

                // Create a list for all permanence values
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

  <br>
  Implementation Details: <br>
  Reconstruct permanence values from active columns using the Spatial Pooler
  
    var probabilities = sp.Reconstruct(actCols);
<br>
Set the maximum input index 200 <br>
Note: According to the size of Encoded Inputs (200 bits)
<be>
<b>allPermanenceValues<b> contains all permanence values including active and inactive colums where inactive col values set to 0 and active cols values remain the same.
<br> 

<b>permanenceValuesList</b> is used to convert allPermanenceValues from dictionary to list.
<br>
Threshold values is selected 0.52 to do thresholding permanence values. it will make either 0 or 1.
<br>

                // Get threshold values
                var thresholdValues = Helpers.ThresholdProbabilities(permanenceValuesList, 0.52);
<br>
ThresholdProbabilities Class code:<br>

    public static double[] ThresholdProbabilities(IEnumerable<double> values, double threshold)
        {
            // Returning null for null input values
            if (values == null)
            {
                return null;
            }

            // Get the length of the values enumerable
            int length = values.Count();

            // Create a one-dimensional array to hold thresholded values
            double[] result = new double[length];

            int index = 0;
            foreach (var numericValue in values)
            {
                // Determine the thresholded value based on the threshold
                double thresholdedValue = (numericValue >= threshold) ? 1.0 : 0.0;

                // Assign the thresholded value to the result array
                result[index++] = thresholdedValue;
            }

            return result;
        }

<br> 
<b>Reconstruct() Method:</b> <br>
Utilizing the Neocortexapi's Reconstruct() method, we meticulously reverse the transformation of the encoded int[] arrays. The reconstructed representations are shaped by permanence values obtained from the Reconstruction method.
<br> 

    public Dictionary<int, double> Reconstruct(int[] activeMiniColumns)
     {
     if (activeMiniColumns == null)
     {
         throw new ArgumentNullException(nameof(activeMiniColumns));
     }

     var cols = connections.GetColumnList(activeMiniColumns);

     Dictionary<int, double> permancences = new Dictionary<int, double>();

    
     foreach (var col in cols)
     {
         col.ProximalDendrite.Synapses.ForEach(s =>
         {
             double currPerm = 0.0;

             
             if (permancences.TryGetValue(s.InputIndex, out currPerm))
             {
               
                 permancences[s.InputIndex] = s.Permanence + currPerm;
             }
             else
             {
              
                 permancences[s.InputIndex] = s.Permanence;
             }
         });
     }

     return permancences;
 }














