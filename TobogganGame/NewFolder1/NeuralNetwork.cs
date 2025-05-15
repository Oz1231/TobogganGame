using System;
using System.IO;

namespace TobogganGame
{
    /// <summary>
    /// Neural network with simplified architecture for better learning
    /// </summary>
    public class NeuralNetwork
    {
        // Layer sizes
        private int inputSize;
        private int hiddenSize;
        private int outputSize;

        // Weights and biases
        private double[,] weightsInputHidden;
        private double[,] weightsHiddenOutput;
        private double[] biasHidden;
        private double[] biasOutput;

        // Learning rate
        private double learningRate = 0.002;

        // Momentum for faster learning
        private double momentum = 0.2;
        private double[,] momentumInputHidden;
        private double[,] momentumHiddenOutput;

        // Random number generator
        private Random random;

        // Default file path
        private const string DefaultWeightsFilePath = "neural_network_weights.dat";

        /// <summary>
        /// Creates a new neural network
        /// </summary>
        /// <param name="inputSize">Number of input neurons</param>
        /// <param name="hiddenSize">Number of hidden neurons</param>
        /// <param name="outputSize">Number of output neurons</param>
        public NeuralNetwork(int inputSize, int hiddenSize, int outputSize)
        {
            this.inputSize = inputSize;
            this.hiddenSize = hiddenSize;
            this.outputSize = outputSize;

            random = new Random();

            // Initialize weights and biases
            weightsInputHidden = new double[inputSize, hiddenSize];
            weightsHiddenOutput = new double[hiddenSize, outputSize];
            biasHidden = new double[hiddenSize];
            biasOutput = new double[outputSize];

            // Initialize momentum arrays
            momentumInputHidden = new double[inputSize, hiddenSize];
            momentumHiddenOutput = new double[hiddenSize, outputSize];

            // Initialize weights
            InitializeWeights();
        }

        /// <summary>
        /// Initializes weights using He initialization
        /// </summary>
        private void InitializeWeights()
        {
            // Calculate scale factors for proper initialization
            double inputScale = Math.Sqrt(2.0 / inputSize);
            double hiddenScale = Math.Sqrt(2.0 / hiddenSize);

            // Input to hidden layer
            for (int i = 0; i < inputSize; i++)
            {
                for (int j = 0; j < hiddenSize; j++)
                {
                    weightsInputHidden[i, j] = (random.NextDouble() * 2 - 1) * inputScale;
                    momentumInputHidden[i, j] = 0.0;
                }
            }

            // Hidden to output layer
            for (int i = 0; i < hiddenSize; i++)
            {
                biasHidden[i] = 0.01 * (random.NextDouble() * 2 - 1);

                for (int j = 0; j < outputSize; j++)
                {
                    weightsHiddenOutput[i, j] = (random.NextDouble() * 2 - 1) * hiddenScale;
                    momentumHiddenOutput[i, j] = 0.0;
                }
            }

            // Output biases
            for (int i = 0; i < outputSize; i++)
            {
                biasOutput[i] = 0.0;
            }
        }

        /// <summary>
        /// ReLU activation function
        /// </summary>
        /// <param name="x">Input value</param>
        /// <returns>Activated value</returns>
        private double ReLU(double x)
        {
            return Math.Max(0, x);
        }

        /// <summary>
        /// Derivative of ReLU function
        /// </summary>
        /// <param name="x">Input value</param>
        /// <returns>Derivative value</returns>
        private double ReLUDerivative(double x)
        {
            return x > 0 ? 1.0 : 0.0;
        }

        /// <summary>
        /// Performs forward pass through network
        /// </summary>
        /// <param name="inputs">Input values</param>
        /// <returns>Output values</returns>
        public double[] FeedForward(double[] inputs)
        {
            if (inputs.Length != inputSize)
            {
                throw new ArgumentException($"Expected {inputSize} inputs, but got {inputs.Length}");
            }

            // Normalize inputs
            double[] normalizedInputs = new double[inputSize];
            for (int i = 0; i < inputSize; i++)
            {
                normalizedInputs[i] = Math.Max(-10.0, Math.Min(10.0, inputs[i]));
            }

            // Calculate hidden layer with ReLU activation
            double[] hiddenInputs = new double[hiddenSize];
            double[] hiddenOutputs = new double[hiddenSize];

            for (int i = 0; i < hiddenSize; i++)
            {
                hiddenInputs[i] = biasHidden[i];
                for (int j = 0; j < inputSize; j++)
                {
                    hiddenInputs[i] += normalizedInputs[j] * weightsInputHidden[j, i];
                }
                hiddenOutputs[i] = ReLU(hiddenInputs[i]);
            }

            // Calculate output layer (linear)
            double[] outputs = new double[outputSize];
            for (int i = 0; i < outputSize; i++)
            {
                outputs[i] = biasOutput[i];
                for (int j = 0; j < hiddenSize; j++)
                {
                    outputs[i] += hiddenOutputs[j] * weightsHiddenOutput[j, i];
                }
            }

            return outputs;
        }

        /// <summary>
        /// Trains the network using backpropagation
        /// </summary>
        /// <param name="inputs">Input values</param>
        /// <param name="targets">Target output values</param>
        public void Train(double[] inputs, double[] targets)
        {
            // Forward pass
            double[] hiddenInputs = new double[hiddenSize];
            double[] hiddenOutputs = new double[hiddenSize];

            for (int i = 0; i < hiddenSize; i++)
            {
                hiddenInputs[i] = biasHidden[i];
                for (int j = 0; j < inputSize; j++)
                {
                    hiddenInputs[i] += inputs[j] * weightsInputHidden[j, i];
                }
                hiddenOutputs[i] = ReLU(hiddenInputs[i]);
            }

            // Calculate output
            double[] outputs = new double[outputSize];
            for (int i = 0; i < outputSize; i++)
            {
                outputs[i] = biasOutput[i];
                for (int j = 0; j < hiddenSize; j++)
                {
                    outputs[i] += hiddenOutputs[j] * weightsHiddenOutput[j, i];
                }
            }

            // Backpropagation
            // Output layer errors with gradient clipping
            double[] outputErrors = new double[outputSize];
            for (int i = 0; i < outputSize; i++)
            {
                double error = targets[i] - outputs[i];
                // Clip error for stability
                outputErrors[i] = Math.Max(-2.0, Math.Min(2.0, error));
            }

            // Hidden layer errors
            double[] hiddenErrors = new double[hiddenSize];
            for (int i = 0; i < hiddenSize; i++)
            {
                hiddenErrors[i] = 0;
                for (int j = 0; j < outputSize; j++)
                {
                    hiddenErrors[i] += outputErrors[j] * weightsHiddenOutput[i, j];
                }
                hiddenErrors[i] *= ReLUDerivative(hiddenInputs[i]);
            }

            // Update output layer weights and biases with momentum
            for (int i = 0; i < outputSize; i++)
            {
                biasOutput[i] += learningRate * outputErrors[i];
                for (int j = 0; j < hiddenSize; j++)
                {
                    double delta = learningRate * outputErrors[i] * hiddenOutputs[j];

                    // Apply momentum
                    delta += momentum * momentumHiddenOutput[j, i];
                    momentumHiddenOutput[j, i] = delta;

                    weightsHiddenOutput[j, i] += delta;
                }
            }

            // Update hidden layer weights and biases with momentum
            for (int i = 0; i < hiddenSize; i++)
            {
                biasHidden[i] += learningRate * hiddenErrors[i];
                for (int j = 0; j < inputSize; j++)
                {
                    double delta = learningRate * hiddenErrors[i] * inputs[j];

                    // Apply momentum
                    delta += momentum * momentumInputHidden[j, i];
                    momentumInputHidden[j, i] = delta;

                    weightsInputHidden[j, i] += delta;
                }
            }
        }

        /// <summary>
        /// Trains the network for Q-Learning (updates only one output)
        /// </summary>
        /// <param name="state">Current state</param>
        /// <param name="action">Selected action</param>
        /// <param name="targetQ">Target Q-value</param>
        public void TrainQ(double[] state, int action, double targetQ)
        {
            // Get current Q values
            double[] predictions = FeedForward(state);

            // Create target array
            double[] targets = new double[outputSize];
            Array.Copy(predictions, targets, outputSize);

            // Update only the selected action
            targets[action] = targetQ;

            // Train with updated targets
            Train(state, targets);
        }

        /// <summary>
        /// Performs soft update of target network
        /// </summary>
        /// <param name="targetNetwork">Target network to update</param>
        /// <param name="tau">Update rate (0-1)</param>
        public void SoftUpdateTargetNetwork(NeuralNetwork targetNetwork, double tau)
        {
            // Update input-hidden weights
            for (int i = 0; i < inputSize; i++)
            {
                for (int j = 0; j < hiddenSize; j++)
                {
                    targetNetwork.weightsInputHidden[i, j] =
                        tau * this.weightsInputHidden[i, j] +
                        (1 - tau) * targetNetwork.weightsInputHidden[i, j];
                }
            }

            // Update hidden biases
            for (int i = 0; i < hiddenSize; i++)
            {
                targetNetwork.biasHidden[i] =
                    tau * this.biasHidden[i] +
                    (1 - tau) * targetNetwork.biasHidden[i];
            }

            // Update hidden-output weights
            for (int i = 0; i < hiddenSize; i++)
            {
                for (int j = 0; j < outputSize; j++)
                {
                    targetNetwork.weightsHiddenOutput[i, j] =
                        tau * this.weightsHiddenOutput[i, j] +
                        (1 - tau) * targetNetwork.weightsHiddenOutput[i, j];
                }
            }

            // Update output biases
            for (int i = 0; i < outputSize; i++)
            {
                targetNetwork.biasOutput[i] =
                    tau * this.biasOutput[i] +
                    (1 - tau) * targetNetwork.biasOutput[i];
            }
        }

        /// <summary>
        /// Creates a clone of this network
        /// </summary>
        /// <returns>Cloned neural network</returns>
        public NeuralNetwork Clone()
        {
            NeuralNetwork clone = new NeuralNetwork(inputSize, hiddenSize, outputSize);
            clone.learningRate = this.learningRate;
            clone.momentum = this.momentum;

            // Copy weights and biases
            for (int i = 0; i < inputSize; i++)
            {
                for (int j = 0; j < hiddenSize; j++)
                {
                    clone.weightsInputHidden[i, j] = this.weightsInputHidden[i, j];
                    clone.momentumInputHidden[i, j] = this.momentumInputHidden[i, j];
                }
            }

            for (int i = 0; i < hiddenSize; i++)
            {
                clone.biasHidden[i] = this.biasHidden[i];
                for (int j = 0; j < outputSize; j++)
                {
                    clone.weightsHiddenOutput[i, j] = this.weightsHiddenOutput[i, j];
                    clone.momentumHiddenOutput[i, j] = this.momentumHiddenOutput[i, j];
                }
            }

            for (int i = 0; i < outputSize; i++)
            {
                clone.biasOutput[i] = this.biasOutput[i];
            }

            return clone;
        }

        /// <summary>
        /// Sets the learning rate
        /// </summary>
        /// <param name="rate">New learning rate</param>
        public void SetLearningRate(double rate)
        {
            this.learningRate = rate;
        }

        /// <summary>
        /// Saves network weights to file
        /// </summary>
        /// <param name="filePath">Path to save weights</param>
        public void SaveWeights(string filePath = DefaultWeightsFilePath)
        {
            try
            {
                using (BinaryWriter writer = new BinaryWriter(File.Open(filePath, FileMode.Create)))
                {
                    // Network structure
                    writer.Write(inputSize);
                    writer.Write(hiddenSize);
                    writer.Write(outputSize);
                    writer.Write(learningRate);
                    writer.Write(momentum);

                    // Input-hidden weights
                    for (int i = 0; i < inputSize; i++)
                    {
                        for (int j = 0; j < hiddenSize; j++)
                        {
                            writer.Write(weightsInputHidden[i, j]);
                        }
                    }

                    // Hidden-output weights
                    for (int i = 0; i < hiddenSize; i++)
                    {
                        for (int j = 0; j < outputSize; j++)
                        {
                            writer.Write(weightsHiddenOutput[i, j]);
                        }
                    }

                    // Hidden biases
                    for (int i = 0; i < hiddenSize; i++)
                    {
                        writer.Write(biasHidden[i]);
                    }

                    // Output biases
                    for (int i = 0; i < outputSize; i++)
                    {
                        writer.Write(biasOutput[i]);
                    }

                    // Momentum arrays
                    for (int i = 0; i < inputSize; i++)
                    {
                        for (int j = 0; j < hiddenSize; j++)
                        {
                            writer.Write(momentumInputHidden[i, j]);
                        }
                    }

                    for (int i = 0; i < hiddenSize; i++)
                    {
                        for (int j = 0; j < outputSize; j++)
                        {
                            writer.Write(momentumHiddenOutput[i, j]);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving network weights: {ex.Message}");
            }
        }

        /// <summary>
        /// Loads network weights from file
        /// </summary>
        /// <param name="filePath">Path to load weights from</param>
        /// <returns>True if successfully loaded, false otherwise</returns>
        public bool LoadWeights(string filePath = DefaultWeightsFilePath)
        {
            if (!File.Exists(filePath))
            {
                return false;
            }

            try
            {
                using (BinaryReader reader = new BinaryReader(File.Open(filePath, FileMode.Open)))
                {
                    // Read network structure
                    int storedInputSize = reader.ReadInt32();
                    int storedHiddenSize = reader.ReadInt32();
                    int storedOutputSize = reader.ReadInt32();

                    // Verify network structure
                    if (storedInputSize != inputSize || storedOutputSize != outputSize)
                    {
                        throw new Exception("Network structure mismatch");
                    }

                    // Recreate arrays if hidden size is different
                    if (storedHiddenSize != hiddenSize)
                    {
                        hiddenSize = storedHiddenSize;
                        weightsInputHidden = new double[inputSize, hiddenSize];
                        weightsHiddenOutput = new double[hiddenSize, outputSize];
                        biasHidden = new double[hiddenSize];
                        momentumInputHidden = new double[inputSize, hiddenSize];
                        momentumHiddenOutput = new double[hiddenSize, outputSize];
                    }

                    // Read learning rate and momentum
                    try
                    {
                        learningRate = reader.ReadDouble();
                        momentum = reader.ReadDouble();
                    }
                    catch
                    {
                        // Use defaults for older files
                        learningRate = 0.002;
                        momentum = 0.2;

                        // Reset file position
                        reader.BaseStream.Position = 12; // Back to after structure
                    }

                    // Read input-hidden weights
                    for (int i = 0; i < inputSize; i++)
                    {
                        for (int j = 0; j < hiddenSize; j++)
                        {
                            weightsInputHidden[i, j] = reader.ReadDouble();
                        }
                    }

                    // Read hidden-output weights
                    for (int i = 0; i < hiddenSize; i++)
                    {
                        for (int j = 0; j < outputSize; j++)
                        {
                            weightsHiddenOutput[i, j] = reader.ReadDouble();
                        }
                    }

                    // Read hidden biases
                    for (int i = 0; i < hiddenSize; i++)
                    {
                        biasHidden[i] = reader.ReadDouble();
                    }

                    // Read output biases
                    for (int i = 0; i < outputSize; i++)
                    {
                        biasOutput[i] = reader.ReadDouble();
                    }

                    // Try to read momentum arrays if they exist
                    try
                    {
                        for (int i = 0; i < inputSize; i++)
                        {
                            for (int j = 0; j < hiddenSize; j++)
                            {
                                momentumInputHidden[i, j] = reader.ReadDouble();
                            }
                        }

                        for (int i = 0; i < hiddenSize; i++)
                        {
                            for (int j = 0; j < outputSize; j++)
                            {
                                momentumHiddenOutput[i, j] = reader.ReadDouble();
                            }
                        }
                    }
                    catch
                    {
                        // Initialize momentum to zero for older files
                        for (int i = 0; i < inputSize; i++)
                        {
                            for (int j = 0; j < hiddenSize; j++)
                            {
                                momentumInputHidden[i, j] = 0.0;
                            }
                        }

                        for (int i = 0; i < hiddenSize; i++)
                        {
                            for (int j = 0; j < outputSize; j++)
                            {
                                momentumHiddenOutput[i, j] = 0.0;
                            }
                        }
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading network weights: {ex.Message}");
                return false;
            }
        }
    }
}