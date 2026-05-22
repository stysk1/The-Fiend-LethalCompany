using System;
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using LethalLib.Modules;
using UnityEngine;

namespace TheFiend
{
    [BepInPlugin("com.TheFiend", "The Fiend", MyPluginInfo.PLUGIN_VERSION)]
    [BepInDependency("evaisa.lethallib", BepInDependency.DependencyFlags.HardDependency)]
    public class TheFiend : BaseUnityPlugin
    {
        public static TheFiend instance;
        public static string RoleCompanyFolder = "Assets/TheFiend/";
        public static AssetBundle bundle;

        public static ConfigEntry<int> SpawnChance;
        public static ConfigEntry<Levels.LevelTypes> Moon;
        public static ConfigEntry<int> FlickerRngChance;
        public static ConfigEntry<bool> WillRageAfterApparatus;
        public static ConfigEntry<float> Volume;

        private void Awake()
        {
            ConfigFile config = new ConfigFile(Path.Combine(Paths.ConfigPath, "Fiend.cfg"), true);
            SpawnChance = config.Bind("Fiend", "Spawn Weight", 30, "The Chance to spawn the fiend inside of the building");
            Moon = config.Bind("Fiend", "Moon", (Levels.LevelTypes)(-1), "What is the only moon it can spawn on. Only one VALUE at a time.");
            FlickerRngChance = config.Bind("Fiend", "Flicker Chance", 1000, "This is a Random chance out of 1/1000 happening to a random player");
            WillRageAfterApparatus = config.Bind("Fiend", "Rage After Apparatus", true, "Trigger his rage mode if you remove the Apparatus.");
            Volume = config.Bind("Fiend", "Volume", 1f, "Sounds as scream and idle sound, not step sounds");

            InitializeNetworkBehaviours();

            instance = this;
            bundle = AssetBundle.LoadFromFile(Path.Combine(Path.GetDirectoryName(Info.Location), "thefiend"));

            EnemyType enemyType = bundle.LoadAsset<EnemyType>(RoleCompanyFolder + "TheFiend.asset");
            TerminalNode terminalNode = bundle.LoadAsset<TerminalNode>(RoleCompanyFolder + "TheFiendNode.asset");
            TerminalKeyword terminalKeyword = bundle.LoadAsset<TerminalKeyword>(RoleCompanyFolder + "TheFiendKey.asset");

            Enemies.RegisterEnemy(enemyType, SpawnChance.Value, Moon.Value, terminalNode, terminalKeyword);
            NetworkPrefabs.RegisterNetworkPrefab(enemyType.enemyPrefab);
            Utilities.FixMixerGroups(enemyType.enemyPrefab);

            Logger.LogInfo($"The Fiend {MyPluginInfo.PLUGIN_VERSION} loaded.");
        }

        // The NetcodePatcher emits RPC-registration methods marked [RuntimeInitializeOnLoadMethod].
        // BepInEx loads this assembly too late for Unity to invoke them automatically, so do it here.
        private static void InitializeNetworkBehaviours()
        {
            foreach (Type type in Assembly.GetExecutingAssembly().GetTypes())
            {
                MethodInfo[] methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic);
                foreach (MethodInfo method in methods)
                {
                    if (method.GetCustomAttributes(typeof(RuntimeInitializeOnLoadMethodAttribute), false).Length != 0)
                    {
                        method.Invoke(null, null);
                    }
                }
            }
        }
    }
}
