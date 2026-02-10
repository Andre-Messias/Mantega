namespace Mantega.AI
{
    using UnityEngine;
    using Unity.InferenceEngine;

    using Mantega.Core.Diagnostics;

    public class SiameseRuneMatcher : MonoBehaviour
    {
        [SerializeField] private ModelAsset _modelAsset;
        private Model _runtimeModel;

        private Worker _worker;
        private Tensor<float> _tensor1;
        private Tensor<float> _tensor2;

        private void Awake()
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

        void OnDestroy()
        {
            // Clean up resources
            _worker?.Dispose();
            _tensor1?.Dispose();
            _tensor2?.Dispose();
        }
    }
}