using BepInEx;
using BepInEx.Logging;
using UnityEngine;

namespace SuperEnergy
{
    [BepInPlugin("Dinwlooc.SuperEnergy", "SuperEnergy", "1.0.0")]
    [BepInDependency("Dinwlooc.Common", BepInDependency.DependencyFlags.HardDependency)]
    public class SuperEnergy : BaseUnityPlugin
    {
        internal static SuperEnergy Instance { get; private set; } = null!;
        internal new static ManualLogSource Logger { get; private set; } = null!;

        private EnergyService _service = null!;

        private void Awake()
        {
            Instance = this;
            Logger = base.Logger;

            SuperEnergyConfig.Instance.Initialize(Config);

            gameObject.transform.parent = null;
            gameObject.hideFlags = HideFlags.HideAndDontSave;

            _service = gameObject.AddComponent<EnergyService>();
            Logger.LogInfo($"{Info.Metadata.GUID} v{Info.Metadata.Version} has loaded!");
        }
    }
}