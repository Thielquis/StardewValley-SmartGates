using SmartGates.Framework;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using xTile.Dimensions;

namespace SmartGates {

    public class ModEntry : Mod {
        private ModConfig config = null!;

        private GateManager _gateManager = null!;
        private GateDiscovery _gateDiscovery = null!;

        private Vector2? _lastFrontTile;


        public override void Entry(IModHelper Helper) {
            config = Helper.ReadConfig<ModConfig>();
            _gateManager = new GateManager(Monitor, config);
            _gateDiscovery = new GateDiscovery(_gateManager, Monitor);
            Helper.Events.GameLoop.GameLaunched += OnGameLaunched;
            Helper.Events.GameLoop.ReturnedToTitle += OnReturnedToTile;
            Helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
        }

        private void OnGameLaunched(object? sender, GameLaunchedEventArgs e) {
            // Config Menu
            IGenericModConfigMenuApi? configMenu = Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            if (configMenu != null) {
                configMenu.Register(
                    mod: ModManifest,
                    reset: () => config = new ModConfig(),
                    save: () => Helper.WriteConfig(config)
                );

                configMenu.AddNumberOption(
                    mod: ModManifest,
                    name: () => Helper.Translation.Get("GateDelay"),
                    getValue: () => config.GateDelay,
                    setValue: value => config.GateDelay = Math.Max(0, value),
                    min: 0,
                    max: 5000,
                    interval: 50
                );
                configMenu.AddBoolOption(
                    mod: ModManifest,
                    name: () => Helper.Translation.Get("CloseOpenGates"),
                    getValue: () => config.CloseOpenGates,
                    setValue: value => config.CloseOpenGates = value
                );
            }
        }


        private void OnSaveLoaded(object? sender, EventArgs e) {
            base.Helper.Events.Player.Warped -= OnWarped;
            base.Helper.Events.World.ObjectListChanged -= OnObjectListChanged;
            base.Helper.Events.GameLoop.UpdateTicked -= OnUpdateTicked;
            base.Helper.Events.GameLoop.OneSecondUpdateTicked -= OnOneSecondUpdateTicked;

            base.Helper.Events.Player.Warped += OnWarped;
            base.Helper.Events.World.ObjectListChanged += OnObjectListChanged;
            base.Helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
            base.Helper.Events.GameLoop.OneSecondUpdateTicked += OnOneSecondUpdateTicked;
        }

        private void OnReturnedToTile(object? sender, ReturnedToTitleEventArgs e) {
            if (Game1.IsMultiplayer) {
                return;
            }
            base.Helper.Events.Player.Warped -= OnWarped;
            base.Helper.Events.World.ObjectListChanged -= OnObjectListChanged;
            base.Helper.Events.GameLoop.UpdateTicked -= OnUpdateTicked;
            base.Helper.Events.GameLoop.OneSecondUpdateTicked -= OnOneSecondUpdateTicked;
        }

        private void OnWarped(object? sender, WarpedEventArgs e) {
            this._gateDiscovery.CheckOnWarp(e);
        }

        private void OnObjectListChanged(object? sender, ObjectListChangedEventArgs e) {
            this._gateDiscovery.CheckOnObjectsChanged(e);
        }

        private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e) {
            if (!Context.IsWorldReady)
                return;

            Farmer player = Game1.player;
            Vector2 currentFrontTile = player.Tile + Utils.FacingOffsets[player.FacingDirection]; ;

            if (_lastFrontTile == null || currentFrontTile != _lastFrontTile.Value) {
                _lastFrontTile = currentFrontTile;

                PlayerMovedTile(player, currentFrontTile);
            }
        }

        private void OnOneSecondUpdateTicked(object? sender, OneSecondUpdateTickedEventArgs e) {
            if (!Context.IsWorldReady)
                return;

            GameLocation location = Game1.currentLocation;
            if (location != null && location.objects != null) {
                _gateDiscovery.ScanFences(location);
            }
        }

        private void PlayerMovedTile(Farmer player, Vector2 tile) {
            if (_gateManager.ManagedGates != null) {
                _gateManager.ManageGates(tile);
            }
        }
    }
}
