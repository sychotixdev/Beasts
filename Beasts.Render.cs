using System.Globalization;
using System.Linq;
using Beasts.Data;
using Beasts.ExileCore;
using ExileCore.PoEMemory.Components;
using ExileCore.Shared.Enums;
using ImGuiNET;
using SharpDX;
using Vector2 = System.Numerics.Vector2;
using Vector3 = System.Numerics.Vector3;

namespace Beasts;

public partial class Beasts
{
    public override void Render()
    {
        DrawInGameBeasts();
        DrawBestiaryPanel();
        DrawBeastsWindow();
    }

    private void DrawInGameBeasts()
    {
        foreach (var entity in _trackedBeasts.Values)
        {
            var positioned = entity.GetComponent<Positioned>();
            if (positioned == null) continue;

            var beast = BeastsDatabase.AllBeasts.FirstOrDefault(b => b.Path == entity.Metadata);
            if (beast == null || Settings.Beasts.All(b => b.Path != beast.Path)) continue;

            var displayText = beast.DisplayName;
            var isTrapped = entity.TryGetComponent<Buffs>(out var buffComp) && buffComp.HasBuff("capture_monster_trapped");
            if (isTrapped)
                displayText = $"{beast.DisplayName}\nTrapped!";

            var pos = GameController.IngameState.Data.ToWorldWithTerrainHeight(positioned.GridPosition);
            Graphics.DrawTextWithBackground(
                displayText, GameController.IngameState.Camera.WorldToScreen(new Vector3(pos.X, pos.Y, pos.Z)), Color.White,
                FontAlign.Center | FontAlign.VerticalCenter, Color.Black with { A = 165 });

            if (Settings.DrawNamesOnMap.Value)
            {
                var fontSize = Graphics.MeasureText(displayText);
                var screenPos = GameController.IngameState.Data.GetGridMapScreenPosition(positioned.GridPosNum);
                var newScreenPos = new Vector2(screenPos.X, screenPos.Y - fontSize.Y);

                Graphics.DrawTextWithBackground(
                    displayText, newScreenPos, GetSpecialBeastColor(beast.DisplayName), FontAlign.Center | FontAlign.VerticalCenter,
                    Color.Black with { A = 165 });
            }
        }
    }

    private Color GetSpecialBeastColor(string beastName)
    {
        if (beastName.Contains("Vivid"))
            return new Color(255, 250, 0);

        if (beastName.Contains("Wild"))
            return new Color(255, 0, 235);

        if (beastName.Contains("Primal"))
            return new Color(0, 245, 255);

        if (beastName.Contains("Black"))
            return new Color(255, 255, 255);

        return Color.Red;
    }

    private void DrawBestiaryPanel()
    {
        var bestiary = GameController.IngameState.IngameUi.GetBestiaryPanel();
        if (bestiary == null || bestiary.IsVisible == false) return;

        var capturedBeastsPanel = bestiary.CapturedBeastsPanel;
        if (capturedBeastsPanel == null || capturedBeastsPanel.IsVisible == false) return;

        var beasts = bestiary.CapturedBeastsPanel.CapturedBeasts;
        foreach (var beast in beasts)
        {
            var beastMetadata = Settings.Beasts.Find(b => b.DisplayName == beast.DisplayName);
            if (beastMetadata == null) continue;

            if (!Settings.BeastPrices.ContainsKey(beastMetadata.DisplayName)) continue;

            var center = new Vector2(beast.GetClientRect().Center.X, beast.GetClientRect().Center.Y);

            Graphics.DrawBox(beast.GetClientRect(), new Color(0, 0, 0, 0.5f));
            Graphics.DrawFrame(beast.GetClientRect(), Color.White, 2);
            Graphics.DrawText(beastMetadata.DisplayName, center, Color.White, FontAlign.Center);

            var text = Settings.BeastPrices[beastMetadata.DisplayName].ToString(CultureInfo.InvariantCulture) + "c";
            var textPos = center + new Vector2(0, 20);
            Graphics.DrawText(text, textPos, Color.White, FontAlign.Center);
        }
    }

    private void DrawBeastsWindow()
    {
        if (!Settings.DrawBeastsWindow)
            return;

        ImGui.SetNextWindowSize(new Vector2(0, 0));
        ImGui.SetNextWindowBgAlpha(0.6f);
        ImGui.Begin("Beasts Window", ImGuiWindowFlags.NoDecoration);

        if (ImGui.BeginTable("Beasts Table", 2, ImGuiTableFlags.RowBg | ImGuiTableFlags.BordersOuter | ImGuiTableFlags.BordersV))
        {
            ImGui.TableSetupColumn("Price", ImGuiTableColumnFlags.WidthFixed, 48);
            ImGui.TableSetupColumn("Beast");

            foreach (var entity in _trackedBeasts.Values)
            {
                var beastMetadata = Settings.Beasts.Find(b => b.Path == entity.Metadata);
                if (beastMetadata == null) continue;

                ImGui.TableNextRow();

                ImGui.TableNextColumn();

                ImGui.Text(
                    Settings.BeastPrices.TryGetValue(beastMetadata.DisplayName, out var price) ? $"{price.ToString(CultureInfo.InvariantCulture)}c" : "0c");

                ImGui.TableNextColumn();

                ImGui.Text(beastMetadata.DisplayName);
                foreach (var craft in beastMetadata.Crafts)
                {
                    ImGui.Text(craft);
                }
            }

            ImGui.EndTable();
        }

        ImGui.End();
    }
}