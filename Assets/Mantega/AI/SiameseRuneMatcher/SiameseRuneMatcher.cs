namespace Mantega.AI
{
    using UnityEngine;
    using Unity.InferenceEngine;

    using Mantega.Core.Diagnostics;
    using Mantega.Core.Lifecycle;

    public class SiameseRuneMatcher : LifecycleBehaviour
    {
        [Header("Model Settings")]
        [SerializeField] private ModelAsset _modelAsset;

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

        private Model _runtimeModel;
        private Worker _worker;
        private Tensor<float> _tensor1;
        private Tensor<float> _tensor2;

        protected override void OnInitialize()
        {
            Validations.ValidateNotNull(_modelAsset, this);

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


        public float Compare(Texture2D texture1, Texture2D texture2)
        {
            if(_status != ILifecycle.LifecyclePhase.Initialized)
            {
                Log.Error($"Attempted to compare textures before SiameseRuneMatcher was initialized. Use {nameof(Initialized)} to ensure the component is ready before calling Compare.", this);
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

        protected override void OnUninitialize()
        {
            base.OnUninitialize();
            CleanUpResources();
        }

        protected override void OnRestart()
        {
            base.OnRestart();
            CleanUpResources();
            _modelAsset = null;
        }

        protected override void OnFixFault()
        {
            base.OnFixFault();
            if (_modelAsset == null)
            {
                throw new System.InvalidOperationException("Cannot fix fault without a valid model asset reference.");
            }
            CleanUpResources();
        }

        private void CleanUpResources()
        {
            _worker?.Dispose();
            _worker = null;
            _tensor1?.Dispose();
            _tensor1 = null;
            _tensor2?.Dispose();
            _tensor2 = null;
        }
    }
}