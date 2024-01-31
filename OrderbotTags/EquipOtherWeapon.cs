﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media;
using Buddy.Coroutines;
using Clio.XmlEngine;
using ff14bot;
using ff14bot.Enums;
using ff14bot.Managers;
using ff14bot.RemoteWindows;
using LlamaLibrary.Extensions;
using LlamaLibrary.JsonObjects.Lisbeth;
using TreeSharp;
using FontFamily = System.Drawing.FontFamily;

namespace LlamaUtilities.OrderbotTags
{
    [XmlElement("EquipOtherWeapon")]
    public class EquipOtherWeapon : LLProfileBehavior
    {
        private bool _isDone;

        public static readonly LlamaLibrary.Logging.LLogger Log = new("EquipOtherWeapon", Colors.MediumPurple);

        public override bool IsDone => _isDone;

        public override bool HighPriority => true;

        public EquipOtherWeapon() : base()
        {
        }

        protected override void OnStart()
        {
        }

        protected override void OnDone()
        {
        }

        protected override void OnResetCachedDone()
        {
            _isDone = false;
        }

        private async Task ChangeJob()
        {
            // List of the very first weapon available for each class
            var StarterWeapons = new List<KeyValuePair<ClassJobType, int>>()
            {
                new KeyValuePair<ClassJobType, int>(ClassJobType.Astrologian, 10524), // Star Globe
                new KeyValuePair<ClassJobType, int>(ClassJobType.Bard, 1889), // Weathered Shortbow
                new KeyValuePair<ClassJobType, int>(ClassJobType.BlackMage, 2055), // Weathered Scepter
                new KeyValuePair<ClassJobType, int>(ClassJobType.Dancer, 25644), // Deepgold War Quoits
                new KeyValuePair<ClassJobType, int>(ClassJobType.DarkKnight, 10400), // Steel Claymore
                new KeyValuePair<ClassJobType, int>(ClassJobType.Gunbreaker, 25643), // Deepgold Gunblade
                new KeyValuePair<ClassJobType, int>(ClassJobType.Paladin, 1601), // Weathered Shortsword
                new KeyValuePair<ClassJobType, int>(ClassJobType.Dragoon, 1819), // Weathered Spear
                new KeyValuePair<ClassJobType, int>(ClassJobType.Warrior, 1749), // Weathered War Axe
                new KeyValuePair<ClassJobType, int>(ClassJobType.Monk, 1680), // Weathered Hora
                new KeyValuePair<ClassJobType, int>(ClassJobType.Machinist, 10462), // Steel-Barreled Carbine
                new KeyValuePair<ClassJobType, int>(ClassJobType.Ninja, 7952), // Weathered Daggers
                new KeyValuePair<ClassJobType, int>(ClassJobType.Reaper, 35760), // Deepgold War Scythe
                new KeyValuePair<ClassJobType, int>(ClassJobType.RedMage, 18203), // Koppranickel Rapier
                new KeyValuePair<ClassJobType, int>(ClassJobType.Sage, 35778), // Stonegold Milpreves
                new KeyValuePair<ClassJobType, int>(ClassJobType.Samurai, 18046), // High Steel Tachi
                new KeyValuePair<ClassJobType, int>(ClassJobType.Scholar, 34091), // Gaja Codex
                new KeyValuePair<ClassJobType, int>(ClassJobType.Summoner, 2142), // Weathered Grimoire
                new KeyValuePair<ClassJobType, int>(ClassJobType.WhiteMage, 1995), // Weathered Cane
            };

            var SquareMapleShield = 2219;
            var gearSets = GearsetManager.GearSets.Where(i => i.InUse);
            var foundJob = Enum.TryParse(Core.Me.CurrentJob.ToString().Trim(), true, out ClassJobType newjob);
            var weapon = StarterWeapons.FirstOrDefault(x => x.Key == newjob).Value;

            var Job = Core.Me.CurrentJob.ToString().Trim() + "s_Primary_Tool";

            var categoryFound = Enum.TryParse(Job, true, out ItemUiCategory category);

            if (categoryFound)
            {
                Log.Information($"Found Item Category: {categoryFound} Category:{category}");
                var item = InventoryManager.FilledInventoryAndArmory.Where(i => i.Item.EquipmentCatagory == category).OrderByDescending(i => i.Item.ItemLevel).FirstOrDefault();
                var EquipSlot = InventoryManager.GetBagByInventoryBagId(InventoryBagId.EquippedItems)[EquipmentSlot.MainHand];

                Log.Information($"Found Item {item}");
                if (item != null)
                {
                    item.Move(EquipSlot);
                }
            }
            else
            {
                var message = $"Couldn't find other weapon for {Core.Me.CurrentJob}. Attempting to purchase {DataManager.GetItem((uint)weapon).CurrentLocaleName} with Lisbeth";
                Core.OverlayManager.AddToast(() => $"" + message,
                                             TimeSpan.FromMilliseconds(10000),
                                             System.Windows.Media.Color.FromRgb((byte)13, (byte)106, (byte)175),
                                             System.Windows.Media.Color.FromRgb(13, 106, 175),
                                             new System.Windows.Media.FontFamily("Gautami"));
                Log.Information(message);
                if (!await LlamaLibrary.Helpers.Lisbeth.IsProductKeyValid())
                {
                    Log.Error("Lisbeth key is not valid, unable to automatically purchase weapon.");
                    return;
                }

                if (InventoryManager.GetBagByInventoryBagId(InventoryBagId.Armory_MainHand).FreeSlots < 1)
                {
                    Log.Error("You have no free slots in your MainHand armory so we can't equip a new weapon.");
                    return;
                }

                if (InventoryManager.GetBagByInventoryBagId(InventoryBagId.Armory_OffHand).FreeSlots < 1)
                {
                    Log.Error("You have no free slots in your OffHand armory so we can't equip a new weapon.");
                    return;
                }

                var order = new LlamaLibrary.JsonObjects.Lisbeth.Order()
                {
                    Amount = 1,
                    AmountMode = LlamaLibrary.JsonObjects.Lisbeth.AmountMode.Restock,
                    Item = (uint)weapon,
                    Type = LlamaLibrary.JsonObjects.Lisbeth.SourceType.Purchase
                };

                if (!await LlamaLibrary.Helpers.Lisbeth.ExecuteOrders(LlamaLibrary.Extensions.OtherExtensions.GetOrderJson(new List<LlamaLibrary.JsonObjects.Lisbeth.Order>() { order })))
                {
                    Log.Error($"Could not purchase {DataManager.GetItem((uint)weapon).CurrentLocaleName}");
                }

                var item = InventoryManager.FilledInventoryAndArmory.Where(i => i.RawItemId == weapon).OrderByDescending(i => i.Item.ItemLevel).FirstOrDefault();
                var EquipSlot = InventoryManager.GetBagByInventoryBagId(InventoryBagId.EquippedItems)[EquipmentSlot.MainHand];

                Log.Information($"Found Item {item}");
                if (item != null)
                {
                    item.Move(EquipSlot);
                }

                if (Core.Me.CurrentJob == ClassJobType.Paladin)
                {
                    weapon = SquareMapleShield;

                    Log.Information($"Couldn't find item category for {Core.Me.CurrentJob}. Attempting to purchase {DataManager.GetItem((uint)weapon).CurrentLocaleName} with Lisbeth");

                    order = new LlamaLibrary.JsonObjects.Lisbeth.Order()
                    {
                        Amount = 1,
                        AmountMode = LlamaLibrary.JsonObjects.Lisbeth.AmountMode.Restock,
                        Item = (uint)weapon,
                        Type = LlamaLibrary.JsonObjects.Lisbeth.SourceType.Purchase
                    };

                    if (!await LlamaLibrary.Helpers.Lisbeth.ExecuteOrders(LlamaLibrary.Extensions.OtherExtensions.GetOrderJson(new List<LlamaLibrary.JsonObjects.Lisbeth.Order>() { order })))
                    {
                        Log.Error($"Could not purchase {DataManager.GetItem((uint)weapon).CurrentLocaleName}");
                    }

                    item = InventoryManager.FilledInventoryAndArmory.Where(i => i.RawItemId == weapon).OrderByDescending(i => i.Item.ItemLevel).FirstOrDefault();
                    EquipSlot = InventoryManager.GetBagByInventoryBagId(InventoryBagId.EquippedItems)[EquipmentSlot.OffHand];

                    Log.Information($"Found Item {item}");
                    if (item != null)
                    {
                        item.Move(EquipSlot);
                    }
                }
            }

            _isDone = Core.Me.CurrentJob == newjob;
        }

        protected override Composite CreateBehavior()
        {
            return new ActionRunCoroutine(r => ChangeJob());
        }
    }
}