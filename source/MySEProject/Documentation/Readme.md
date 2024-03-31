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







