﻿using System;
using System.IO;
using System.Reflection;
using System.Security;
using System.Security.Permissions;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using CommonAPI;
using CommonAPI.Systems;
using CommonAPI.Systems.ModLocalization;
using HarmonyLib;
using UnityEngine;

[module: UnverifiableCode]
#pragma warning disable 618
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore 618
namespace GigaStations
{
    [BepInDependency(LDB_TOOL_GUID)]
    [BepInDependency(WARPERS_MOD_GUID, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(CommonAPIPlugin.GUID)]
    [BepInPlugin(MODGUID, MODNAME, VERSION)]
    
    [CommonAPISubmoduleDependency(nameof(ProtoRegistry), nameof(UtilSystem), nameof(LocalizationModule))]
    public class GigaStationsPlugin : BaseUnityPlugin
    {

        public const string MODGUID = "org.kremnev8.plugin.GigaStationsUpdated";
        public const string MODNAME = "GigaStationsUpdated";
        public const string VERSION = "2.3.4";
        
        public const string LDB_TOOL_GUID = "me.xiaoye97.plugin.Dyson.LDBTool";
        public const string WARPERS_MOD_GUID = "ShadowAngel.DSP.DistributeSpaceWarper";



        public static ManualLogSource logger;
        public static int gridXCount { get; set; } = 1;
        public static int gridYCount { get; set; } = 5;
        
        public static Color stationColor { get; set; } = new Color(0.3726f, 0.8f, 1f, 1f);
        
        //ILS
        public static int ilsMaxStorage { get; set; } = 30000; //Vanilla 10000
        public static int ilsMaxWarps { get; set; } = 150; //Vanilla 50
        public static int ilsMaxVessels { get; set; } = 30; //Vanilla 10 (limit from 10-30)
        public static int ilsMaxDrones { get; set; } = 150; //Vanilla 50
        public static long ilsMaxAcuGJ { get; set; } = 50; //Vanilla 12 GJ = * 1 000 000 000
        public static int ilsMaxSlots { get; set; } = 12; //Vanilla 5 (limited to from 5-12)

        //PLS
        public static int plsMaxStorage { get; set; } = 15000; //Vanilla 5000
        public static int plsMaxDrones { get; set; } = 150; //Vanilla 50 (limit from 50-150)
        public static long plsMaxAcuMJ { get; set; } = 500; //Vanilla 180 MJ = * 1 000 000
        public static int plsMaxSlots { get; set; } = 12; //Vanilla 3 (limited to from 3-12)

        //Collector
        public static int colMaxStorage { get; set; } = 15000; //Vanilla 5000 
        public static int colSpeedMultiplier { get; set; } = 3; //Vanilla 1 (==8)


        //VesselCapacity
        public static int vesselCapacityMultiplier { get; set; } = 3; //Vanilla 1

        //DroneCapacity
        public static int droneCapacityMultiplier { get; set; } = 3; //Vanilla 1

        public static ItemProto pls;
        public static ItemProto ils;
        public static ItemProto collector;

        public static ModelProto plsModel;
        public static ModelProto ilsModel;
        public static ModelProto collectorModel;

        public static ResourceData resource;

        void Awake()
        {
            logger = Logger;

            string pluginfolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            resource = new ResourceData(MODNAME, "gigastations", pluginfolder);
            resource.LoadAssetBundle("gigastations");
            
            ProtoRegistry.AddResource(resource);


            //General
            gridXCount = Config.Bind("-|0|- General", "-| 1 Grid X Max. Count", 1, new ConfigDescription("Amount of slots visible horizontally.\nIf this value is bigger than 1, layout will form a grid", new AcceptableValueRange<int>(1, 3))).Value;
            gridYCount = Config.Bind("-|0|- General", "-| 2 Grid Y Max. Count", 5, new ConfigDescription("Amount of slots visible vertically", new AcceptableValueRange<int>(3, 12))).Value;
            stationColor = Config.Bind("-|0|- General", "-| 3 Station Color", new Color(0.3726f, 0.8f, 1f, 1f), "Color tint of giga stations").Value;
            
            //ILS
            ilsMaxSlots = Config.Bind("-|1|- ILS", "-| 1 Max. Item Slots", 12, new ConfigDescription("The maximum Item Slots the Station can have.\nVanilla: 5", new AcceptableValueRange<int>(5, 12))).Value;
            ilsMaxStorage = Config.Bind("-|1|- ILS", "-| 2 Max. Storage", 30000, "The maximum Storage capacity per Item-Slot.\nVanilla: 10000").Value;
            ilsMaxVessels = Config.Bind("-|1|- ILS", "-| 3 Max. Vessels", 30, new ConfigDescription("The maximum Logistic Vessels amount.\nVanilla: 10", new AcceptableValueRange<int>(10, 30))).Value;
            ilsMaxDrones = Config.Bind("-|1|- ILS", "-| 4 Max. Drones", 150, new ConfigDescription("The maximum Logistic Drones amount.\nVanilla: 50", new AcceptableValueRange<int>(50, 150))).Value;
            ilsMaxAcuGJ = Config.Bind("-|1|- ILS", "-| 5 Max. Accu Capacity (GJ)", 50, "The Stations maximum Accumulator Capacity in GJ.\nVanilla: 12 GJ").Value;
            ilsMaxWarps = Config.Bind("-|1|- ILS", "-| 6 Max. Warps", 150, "The maximum Warp Cells amount.\nVanilla: 50").Value;

            //PLS
            plsMaxSlots = Config.Bind("-|2|- PLS", "-| 1 Max. Item Slots", 12, new ConfigDescription("The maximum Item Slots the Station can have.\nVanilla: 3", new AcceptableValueRange<int>(3, 12))).Value;
            plsMaxStorage = Config.Bind("-|2|- PLS", "-| 2 Max. Storage", 15000, "The maximum Storage capacity per Item-Slot.\nVanilla: 5000").Value;
            plsMaxDrones = Config.Bind("-|2|- PLS", "-| 3 Max. Drones", 150, new ConfigDescription("The maximum Logistic Drones amount.\nVanilla: 50", new AcceptableValueRange<int>(50, 150))).Value;
            plsMaxAcuMJ = Config.Bind("-|2|- PLS", "-| 4 Max. Accu Capacity (GJ)", 500, "The Stations maximum Accumulator Capacity in MJ.\nVanilla: 180 MJ").Value;

            //Collector
            colSpeedMultiplier = Config.Bind("-|3|- Collector", "-| 1 Collect Speed Multiplier", 3, "Multiplier for the Orbital Collectors Collection-Speed.\nVanilla: 1").Value;
            colMaxStorage = Config.Bind("-|3|- Collector", "-| 2 Max. Storage", 15000, "The maximum Storage capacity per Item-Slot.\nVanilla: 5000").Value;


            //VesselCapacity
            vesselCapacityMultiplier = Config.Bind("-|4|- Vessel", "-| 1 Max. Capacity", 3, "Vessel Capacity Multiplier\n1 == 1000 Vessel Capacity at max Level").Value;

            //DroneCapacity
            droneCapacityMultiplier = Config.Bind("-|5|- Drone", "-| 1 Max. Capacity", 3, "Drone Capacity Multiplier\n1 == 100 Drone Capacity at max Level").Value;

            LocalizationModule.RegisterTranslation("PLS_Name" , "Planetary Giga Station");
            LocalizationModule.RegisterTranslation("PLS_Desc" , "Has more Slots, Capacity, etc. than a usual PLS.");
            LocalizationModule.RegisterTranslation("ILS_Name" , "Interstellar Giga Station");
            LocalizationModule.RegisterTranslation("ILS_Desc" , "Has more Slots, Capacity, etc. than a usual ILS.");
            LocalizationModule.RegisterTranslation("Collector_Name" , "Orbital Giga Collector");
            LocalizationModule.RegisterTranslation("Collector_Desc" , $"Has more Capacity and collects {colSpeedMultiplier}x faster than a usual Collector.");
            
            LocalizationModule.RegisterTranslation("ModificationWarn" , "  - [GigaStationsUpdated] Replaced {0} buildings");
            
            LocalizationModule.RegisterTranslation("CantDowngradeWarn" , "Downgrading logistic station is not possible!");
            
            
            pls = ProtoRegistry.RegisterItem(2110, "PLS_Name", "PLS_Desc", "assets/gigastations/texture2d/icon_pls", 2701);
            ils = ProtoRegistry.RegisterItem(2111, "ILS_Name", "ILS_Desc", "assets/gigastations/texture2d/icon_ils", 2702);
            collector = ProtoRegistry.RegisterItem(2112, "Collector_Name", "Collector_Desc", "assets/gigastations/texture2d/icon_collector", 2703);
            collector.BuildInGas = true;

            ProtoRegistry.RegisterRecipe(410, ERecipeType.Assemble, 2400, new[] {2103, 1103, 1106, 1303, 1206}, new[] {1, 40, 40, 40, 20}, new[] {pls.ID},
                new[] {1}, "PGS_Desc", 1604);
            ProtoRegistry.RegisterRecipe(411, ERecipeType.Assemble, 3600, new[] { 2110, 1107, 1206 }, new[] { 1, 40, 20 }, new[] {ils.ID},
                new[] {1}, "ILS_Desc", 1605);
            ProtoRegistry.RegisterRecipe(412, ERecipeType.Assemble, 3600, new[] { 2111, 1205, 1406, 2207 }, new[] { 1, 50, 20, 20 }, new[] {collector.ID},
                new[] {1}, "Collector_Desc", 1606);


            plsModel = ProtoRegistry.RegisterModel(460, pls, "Entities/Prefabs/logistic-station", null, new[] {24, 38, 12, 10, 1}, 606, 2, new []{2103, 0});
            ilsModel = ProtoRegistry.RegisterModel(461, ils, "Entities/Prefabs/interstellar-logistic-station", null, new[] {24, 38, 12, 10, 1}, 607, 2, new []{2104, 0});
            collectorModel = ProtoRegistry.RegisterModel(462, collector, "Entities/Prefabs/orbital-collector", null, new[] {18, 11, 32, 1}, 608, 2, new []{2105, 0});

            ProtoRegistry.onLoadingFinished += AddGigaPLS;
            ProtoRegistry.onLoadingFinished += AddGigaILS;
            ProtoRegistry.onLoadingFinished += AddGigaCollector;
            
            var harmony = new Harmony(MODGUID);

            harmony.PatchAll(typeof(StationEditPatch));
            harmony.PatchAll(typeof(SaveFixPatch));
            harmony.PatchAll(typeof(StationUpgradePatch));
            harmony.PatchAll(typeof(UIStationWindowPatch));
            harmony.PatchAll(typeof(BlueprintBuilding_Patch));
            harmony.PatchAll(typeof(UIEntityBriefInfo_Patch));

            foreach (var pluginInfo in BepInEx.Bootstrap.Chainloader.PluginInfos)
            {
                if (pluginInfo.Value.Metadata.GUID != WARPERS_MOD_GUID) continue;

                ((ConfigEntry<bool>) pluginInfo.Value.Instance.Config["General", "ShowWarperSlot"]).Value = true;
                logger.LogInfo("Overriding Distribute Space Warpers config: ShowWarperSlot = true");
                break;
            }
            
            UtilSystem.AddLoadMessageHandler(SaveFixPatch.GetFixMessage);
            
            logger.LogInfo("GigaStations is initialized!");
            
        }
        
        void AddGigaPLS()
        {
            ColliderData[] buildColliders = plsModel.prefabDesc.buildColliders;
            logger.LogInfo($"PLS build collider is null: {buildColliders == null}, length: {buildColliders?.Length ?? 0}");
            
            plsModel.prefabDesc.workEnergyPerTick = 3333334;

            plsModel.prefabDesc.stationMaxItemCount = plsMaxStorage;
            plsModel.prefabDesc.stationMaxItemKinds = plsMaxSlots;
            plsModel.prefabDesc.stationMaxDroneCount = plsMaxDrones;
            plsModel.prefabDesc.stationMaxEnergyAcc = Convert.ToInt64(plsMaxAcuMJ * 1000000);

            //Make Giga stations blue
            Material newMat = Instantiate(plsModel.prefabDesc.lodMaterials[0][0]);
            newMat.color = stationColor;
            plsModel.prefabDesc.lodMaterials[0][0] = newMat;
            // Set MaxWarpers in station init!!!!!

        }

        void AddGigaILS()
        {
            ColliderData[] buildColliders = ilsModel.prefabDesc.buildColliders;
            logger.LogInfo($"ILS build collider is null: {buildColliders == null}, length: {buildColliders?.Length ?? 0}");
            
            ilsModel.prefabDesc.workEnergyPerTick = 3333334;

            ilsModel.prefabDesc.stationMaxItemCount = ilsMaxStorage;
            ilsModel.prefabDesc.stationMaxItemKinds = ilsMaxSlots;
            ilsModel.prefabDesc.stationMaxDroneCount = ilsMaxDrones;
            ilsModel.prefabDesc.stationMaxShipCount = ilsMaxVessels;
            ilsModel.prefabDesc.stationMaxEnergyAcc = Convert.ToInt64(ilsMaxAcuGJ * 1000000000);
            
            //Make Giga stations blue
            Material newMat = Instantiate(ilsModel.prefabDesc.lodMaterials[0][0]);
            newMat.color = stationColor;
            ilsModel.prefabDesc.lodMaterials[0][0] = newMat;
            // Set MaxWarpers in station init!!!!!

        }

        void AddGigaCollector()
        {
            var oriItem = LDB.items.Select(2105);
            
            collectorModel.prefabDesc.stationMaxItemCount = colMaxStorage;
            collectorModel.prefabDesc.stationCollectSpeed = oriItem.prefabDesc.stationCollectSpeed * colSpeedMultiplier;

            collectorModel.prefabDesc.workEnergyPerTick /= oriItem.prefabDesc.workEnergyPerTick / colSpeedMultiplier >= 0 ? colSpeedMultiplier : oriItem.prefabDesc.workEnergyPerTick; //??yes or no??
            
            //Make Giga stations blue
            Material newMat = Instantiate(collectorModel.prefabDesc.lodMaterials[0][0]);
            newMat.color = stationColor;
            collectorModel.prefabDesc.lodMaterials[0][0] = newMat;
            
            // Set MaxWarpers in station init!!!!!
        }

    }
}
