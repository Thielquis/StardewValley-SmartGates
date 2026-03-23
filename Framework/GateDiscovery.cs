using SmartGates.Framework;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using xTile.Dimensions;

namespace SmartGates.Framework {
    public class GateDiscovery {
        private List<Vector2> _fenceCache = new();
        private GateManager _gateManager;
        private readonly IMonitor _monitor;

        public GateDiscovery(GateManager manager, IMonitor monitor) {
            _gateManager = manager;
            _monitor = monitor;
        }

        public void ScanFences(GameLocation location) {
            for (int i = _fenceCache.Count - 1; i >= 0; i--) {
                Vector2 pos = _fenceCache[i];
                if (location.objects.TryGetValue(pos, out var obj) && obj is Fence fence) {
                    if (fence.isGate.Value)
                        AddNewGate((pos, location));
                }
                else {
                    _fenceCache.RemoveAt(i);
                }
            }
        }

        public void CheckFencesAndGates(ObjectListChangedEventArgs items = null, WarpedEventArgs warp = null) {
            try {
                // Handle warp first, since we can skip ObjectListChangedEvent in this case
                if (warp != null) {
                    _fenceCache = new List<Vector2>();
                    foreach (KeyValuePair<Vector2, StardewValley.Object> pair in warp.NewLocation.objects.Pairs) {
                        if (pair.Value is Fence fence) {
                            if (!_fenceCache.Contains(pair.Key))
                                _fenceCache.Add(pair.Key);

                            if (fence.isGate.Value) {
                                AddNewGate((pair.Key, warp.NewLocation));
                            }
                        }
                    }
                    return;
                }

                if (items != null) {
                    foreach (KeyValuePair<Vector2, StardewValley.Object> pair in items.Removed) {
                        if (pair.Value is Fence fence) {
                            _fenceCache.Remove(pair.Key);
                            if (fence.isGate.Value)
                                _gateManager.RemoveManagedGate(pair.Key, items.Location);
                        }
                    }

                    foreach (KeyValuePair<Vector2, StardewValley.Object> pair in items.Added) {
                        if (pair.Value is Fence fence) {
                            _fenceCache.Add(pair.Key);

                            if (fence.isGate.Value) {
                                AddNewGate((pair.Key, items.Location));
                            }
                        }
                    }
                }
            }
            catch (Exception e) {
                _monitor.Log($"Error in CheckGates: {e}", LogLevel.Error);
            }
        }

        private void AddNewGate((Vector2, GameLocation) Key) {
            this._gateManager.ManagedGates.TryAdd(Key, new ManagedGate {
                Delayed = false,
                PlayerId = 0
            });
        }
    }
}
