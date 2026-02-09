namespace Mantega.Core
{
    using UnityEngine;

    using Mantega.Core.Syncables;

    public interface IInitializable
    {
        public enum State
        {
            Unitialized,
            Reseting, Initializing, Unitializing,
            Initialized
        }
        
        public State GetState();
        public void Initialize();
        public void Reset();
        public void Unitialize();
    }

    public class Initializable : MonoBehaviour//, IInitializable
    {
        private Syncable<IInitializable.State> _state;
        public IInitializable.State State => _state;

        #region IInitializable methods
        IInitializable.State GetState()
        {
            throw new System.NotImplementedException();
        }
        #endregion
    }

}