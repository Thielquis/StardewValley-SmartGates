using SmartGates.Framework;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using xTile.Dimensions;

namespace SmartGates.Framework {
    public class GateManager {
        public SerializableDictionary<(Vector2, GameLocation), ManagedGate> ManagedGates = new();

        private List<Vector2> _lastGates = new();
        private readonly IMonitor _monitor;
        private readonly ModConfig _config;

        public GateManager(IMonitor monitor, ModConfig config) {
            _monitor = monitor;
            _config = config;
        }

        public void RemoveManagedGate(Vector2 tile, GameLocation location) {
            ManagedGates.Remove((tile, location));
            _lastGates.Remove(tile);
        }

        public void ManageGates(Vector2 tile) {
            Farmer currentFarmer = Game1.player;
            GameLocation currentMap = currentFarmer.currentLocation;
            if (currentMap == null) {
                return;
            }
            Vector2 playerPos = currentFarmer.Tile;
            (Vector2, GameLocation) key = (tile, currentMap);
            long UniqueID = currentFarmer.UniqueMultiplayerID;
            try {
                // Opening current Gate
                if (currentMap.objects.ContainsKey(tile) && !_lastGates.Contains(tile)) {
                    Fence currentGate = currentMap.objects[tile] as Fence;
                    if (currentGate != null && currentGate.isGate.Value) {
                        int gatePosition = currentGate.gatePosition.Value;
                        if (gatePosition == 0)
                            currentGate.toggleGate(currentFarmer, true, false);
                        if (gatePosition != 0 && _config.CloseOpenGates || gatePosition == 0) {
                            _lastGates.Add(tile);
                            this.ManagedGates[key].Delayed = false;
                            this.ManagedGates[key].PlayerId = UniqueID;
                        }
                    }
                }

                // Closing Gates
                for (int i = _lastGates.Count - 1; i >= 0; i--) {
                    Vector2 pos = _lastGates[i];
                    if (pos != playerPos && pos != tile) {
                        Fence currentGate = currentMap.objects[pos] as Fence;
                        key = (pos, currentMap);
                        if (currentGate != null && currentGate.isGate.Value) {
                            bool isNearGate = IsNearGate(currentFarmer, pos, currentGate);
                            bool checkGate = this.ManagedGates[key].CanCloseGate(UniqueID);
                            if (checkGate && !isNearGate) {
                                // schedule the gate closing on the main thread with delay
                                ScheduleClosingGate(currentGate, currentFarmer, currentMap, key);
                            }
                        }
                    }
                }
            }
            catch (Exception e) {
                _monitor.Log($"SmartGate error: {e}", LogLevel.Error);
            }
        }

        private static bool IsNearGate(Farmer currentFarmer, Vector2 gateKey, Fence gate) {
            try {
                Vector2 playerPos = currentFarmer.Tile;

                if (gateKey == playerPos)
                    return true;

                int dx = (int)(gateKey.X - playerPos.X);
                int dy = (int)(gateKey.Y - playerPos.Y);

                bool isAdjacent = dx >= -1 && dx <= 1 && dy >= -1 && dy <= 1;
                bool isPassable = gate.isPassable();

                return isPassable && isAdjacent;
            }
            catch {
                return false;
            }
        }

        private void ScheduleClosingGate(Fence gate, Farmer currentFarmer, GameLocation map, (Vector2, GameLocation) key) {
            ManagedGates[key].Delayed = _config.GateDelay > 0;

            Game1.delayedActions.Add(new DelayedAction(_config.GateDelay, () => {
                ClosingGate(gate, currentFarmer, map, key);
            }));
        }

        private void ClosingGate(Fence gate, Farmer currentFarmer, GameLocation map, (Vector2, GameLocation) key) {
            try {
                if (!ManagedGates.TryGetValue(key, out ManagedGate state))
                    return;

                gate.toggleGate(currentFarmer, false, false);
                _lastGates.Remove(gate.TileLocation);

                state.PlayerId = 0;
                state.Delayed = false;
            }
            catch (Exception e) {
                _monitor.Log($"SmartGate error: {e}", LogLevel.Error);
            }
        }
    }
}
