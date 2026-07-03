using System.Collections.Generic;
using UnityEngine;
using AFramework;

namespace App.Services
{
    public class ServiceLocator : ManualSingletonMono<ServiceLocator>
    {
        [Header("Services")]
        [SerializeField] private BGMService _bgmService;
        [SerializeField] private SoundEffectService _soundEffectService;
        [SerializeField] private VibrationService _vibrationService;

        public IBGMService BGM => _bgmService;
        public ISoundEffectService SFX => _soundEffectService;
        public IVibrationService Vibration => _vibrationService;

        private Dictionary<System.Type, IService> _registry = new Dictionary<System.Type, IService>();

        protected override void Awake()
        {
            base.Awake();
            RegisterAll();
            InitializeAll();
        }

        private void RegisterAll()
        {
            Register<IBGMService>(_bgmService);
            Register<ISoundEffectService>(_soundEffectService);
            Register<IVibrationService>(_vibrationService);
        }

        private void Register<T>(IService service) where T : IService
        {
            if (service != null)
                _registry[typeof(T)] = service;
        }

        private void InitializeAll()
        {
            foreach (var kvp in _registry)
            {
                if (!kvp.Value.IsInitialized)
                {
                    kvp.Value.Initialize();
#if UNITY_EDITOR
                    Debug.Log($"[ServiceLocator] Initialized: {kvp.Key.Name}");
#endif
                }
            }
        }

        public T Get<T>() where T : class, IService
        {
            _registry.TryGetValue(typeof(T), out var service);
            return service as T;
        }
    }
}
