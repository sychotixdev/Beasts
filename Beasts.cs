using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Beasts.Api;
using Beasts.Data;
using ExileCore;
using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared.Enums;

namespace Beasts;

public partial class Beasts : BaseSettingsPlugin<BeastsSettings>
{
    private readonly HashSet<long> _removedBeastIds = new();
    private readonly Dictionary<long, Entity> _trackedBeasts = new();

    public override void OnLoad()
    {
        Settings.FetchBeastPrices.OnPressed += async () => await FetchPrices();
        Task.Run(FetchPrices);
    }

    private async Task FetchPrices()
    {
        DebugWindow.LogMsg("Fetching Beast Prices from PoeNinja...");
        var prices = await PoeNinja.GetBeastsPrices();
        foreach (var beast in BeastsDatabase.AllBeasts)
        {
            Settings.BeastPrices[beast.DisplayName] = prices.GetValueOrDefault(beast.DisplayName, -1);
        }

        Settings.LastUpdate = DateTime.Now;
    }

    public override Job Tick()
    {
        RemoveCapturedBeasts();
        return null;
    }

    public override void AreaChange(AreaInstance area)
    {
        _trackedBeasts.Clear();
        _removedBeastIds.Clear();
    }

    public override void EntityAdded(Entity entity)
    {
        if (_removedBeastIds.Contains(entity.Id)) return;
        if (entity.Rarity != MonsterRarity.Rare) return;
        foreach (var _ in BeastsDatabase.AllBeasts.Where(beast => entity.Metadata == beast.Path))
        {
            _trackedBeasts.Add(entity.Id, entity);
        }
    }

    public override void EntityRemoved(Entity entity)
    {
        if (_trackedBeasts.ContainsKey(entity.Id))
            _trackedBeasts.Remove(entity.Id);
    }

    private void RemoveCapturedBeasts()
    {
        var capturedBeastIds = new List<long>();

        foreach (var entity in _trackedBeasts.Values)
        {
            if (entity.TryGetComponent<Buffs>(out var buffComp) && buffComp.HasBuff("capture_monster_captured"))
                capturedBeastIds.Add(entity.Id);
        }

        foreach (var id in capturedBeastIds)
        {
            _removedBeastIds.Add(id);
            _trackedBeasts.Remove(id);
        }
    }
}