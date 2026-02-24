namespace Mantega.AI
{
    using UnityEngine;
    using Unity.InferenceEngine;

    using Mantega.Core.Diagnostics;
    using Mantega.Core.Lifecycle;

    /// <summary>
    /// Provides functionality for comparing rune textures using a pre-trained Siamese neural network model to calculate
    /// similarity scores.
    /// </summary>
    /// <remarks>The component must be initialized before performing comparison operations.
    /// It manages model resources and inference workers internally, and supports dynamic model asset changes by
    /// restarting and reinitializing as needed. The similarity score calculation leverages GPU acceleration when
    /// available, falling back to CPU execution if necessary.</remarks>
    public class SiameseRuneMatcher : LifecycleBehaviour
    {
        /// <summary>
        /// The model asset containing the pre-trained Siamese network for rune comparison. 
        /// </summary>
        [Header("Model Settings")]
        [SerializeField] private ModelAsset _modelAsset;

        /// <summary>
        /// Gets or sets the model asset associated with this instance.
        /// </summary>
        /// <remarks>Changing this property while the instance is initialized will cause the instance to
        /// restart and reinitialize with the new model asset. This may affect the current state or resources in
        /// use.</remarks>
        public ModelAsset ModelAsset
        {
            get => _modelAsset;
            set
            {
                if (_modelAsset != value)
                {
                    bool wasInitialized = _status == ILifecycle.LifecyclePhase.Initialized;
                    if (wasInitialized) Restart();
                    _modelAsset = value;
                    if (wasInitialized) Initialize();
                }
            }
        }

        #region Resources
        /// <summary>
        /// Represents the runtime model used by the component.
        /// </summary>
        private Model _runtimeModel;

        /// <summary>
        /// Represents the worker responsible for executing the model inference operations.
        /// </summary>
        private Worker _worker;

        /// <summary>
        /// Represents the first input tensor used for model inference.
        /// </summary>
        private Tensor<float> _tensor1;

        /// <summary>
        /// Represents the second input tensor used for model inference.
        /// </summary>
        private Tensor<float> _tensor2;
        #endregion

        #region Lifecycle
        /// <summary>
        /// Initializes the component and prepares required resources for operation.
        /// </summary>
        protected override void OnInitialize()
        {
            base.OnInitialize();
            Validations.ValidateNotNull(_modelAsset, this);
            GenerateResources();
        }

        /// <summary>
        /// Initializes the model runtime and allocates input tensors required for inference operations.
        /// </summary>
        /// <remarks>This method attempts to create a worker using GPU acceleration. If GPU resources are
        /// unavailable, it falls back to CPU execution. 
        /// <para><b>Note:</b> The input tensors are hardcoded to a fixed shape of 105x105 pixels, 
        /// matching the specific expected input of the current model.</para></remarks>
        private void GenerateResources()
        {
            // Load the model
            _runtimeModel = ModelLoader.Load(_modelAsset);
            try
            {
                _worker = new Worker(_runtimeModel, BackendType.GPUCompute);
            }
            catch (System.Exception) // Fallback to CPU if GPU is not available
            {
                _worker = new Worker(_runtimeModel, BackendType.CPU);
            }

            // Prepare input tensors
            TensorShape inputShape = new(1, 1, 105, 105);
            _tensor1 = new Tensor<float>(inputShape);
            _tensor2 = new Tensor<float>(inputShape);
        }

        /// <summary>
        /// Performs cleanup operations when the component is being uninitialized.
        /// </summary>
        protected override void OnUninitialize()
        {
            base.OnUninitialize();
            CleanUpResources();
        }

        /// <summary>
        /// Handles operations required when the component is restarted, ensuring that resources are properly cleaned up
        /// and internal state is reset.
        /// </summary>
        protected override void OnRestart()
        {
            base.OnRestart();
            CleanUpResources();
            _modelAsset = null;
        }

        /// <summary>
        /// Attempts to resolve a fault condition by performing cleanup operations.
        /// </summary>
        /// <remarks>This method should be called when a fault needs to be fixed and requires a valid
        /// model asset reference.</remarks>
        /// <exception cref="System.InvalidOperationException">Thrown if the model asset reference is not set, indicating that fault resolution cannot proceed without a
        /// valid asset.</exception>
        protected override void OnFixFault()
        {
            base.OnFixFault();
            Validations.ValidateNotNull(_modelAsset, this);
            CleanUpResources();
        }

        /// <summary>
        /// Releases resources.
        /// </summary>
        /// <remarks>Call this method to ensure that all disposable resources associated with the current
        /// instance are properly released.</remarks>
        private void CleanUpResources()
        {
            _worker?.Dispose();
            _worker = null;
            _tensor1?.Dispose();
            _tensor1 = null;
            _tensor2?.Dispose();
            _tensor2 = null;
        }
        #endregion

        /// <summary>
        /// Calculates a similarity score between two textures.
        /// </summary>
        /// <remarks>Ensure that the component is initialized before calling this
        /// method (see <see cref="Initialized"/>). If called before initialization, the method returns 0. The similarity score is computed using a
        /// machine learning model and may depend on the quality and preprocessing of the input textures.</remarks>
        /// <param name="texture1">The first texture to compare. Cannot be <see langword="null"/>.</param>
        /// <param name="texture2">The second texture to compare. Cannot be <see langword="null"/>.</param>
        /// <exception cref="System.ArgumentNullException">Thrown when either <paramref name="texture1"/> or <paramref name="texture2"/> is <see langword="null"/>.</exception>
        /// <returns>A floating-point value from 0 to 1 representing the similarity score between the two textures,
        /// If the method is called before the component is initialized, it returns 0.</returns>
        public float Compare(Texture2D texture1, Texture2D texture2)
        {
            if(_status != ILifecycle.LifecyclePhase.Initialized)
            {
                Log.Error($"Attempted to compare textures before {nameof(SiameseRuneMatcher)} was initialized. Use {nameof(Initialized)} to ensure the component is ready before calling Compare.", this);
                return 0f;
            }

            Validations.ValidateNotNull(texture1);
            Validations.ValidateNotNull(texture2);

            // Format inputs
            var transformOps = new TextureTransform()
                .SetTensorLayout(TensorLayout.NCHW);

            // Convert textures to tensors
            TextureConverter.ToTensor(texture1, _tensor1, transformOps);
            TextureConverter.ToTensor(texture2, _tensor2, transformOps);

            // Execute the model
            _worker.SetInput("input_a", _tensor1);
            _worker.SetInput("input_b", _tensor2);

            _worker.Schedule();

            // Get output tensor
            var outputTensor = _worker.PeekOutput("similarity_score") as Tensor<float>;

            // Bring data back to CPU
            using var cpuData = outputTensor.ReadbackAndClone();
            {
                return cpuData[0]; 
            }
        }
    }
}