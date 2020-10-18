namespace Assets.Scripts {
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System;
    using ModApi.Common;
    using ModApi.Settings.Core;

    /// <summary>
    /// The settings for the mod.
    /// </summary>
    /// <seealso cref="ModApi.Settings.Core.SettingsCategory{Assets.Scripts.ModSettings}" />
    public class ModSettings : SettingsCategory<ModSettings> {
        /// <summary>
        /// The mod settings instance.
        /// </summary>
        private static ModSettings _instance;

        /// <summary>
        /// Initializes a new instance of the <see cref="ModSettings"/> class.
        /// </summary>
        public ModSettings () : base ("Designer Tools") { }

        /// <summary>
        /// Gets the mod settings instance.
        /// </summary>
        /// <value>
        /// The mod settings instance.
        /// </value>
        public static ModSettings Instance => _instance ?? (_instance = Game.Instance.Settings.ModSettings.GetCategory<ModSettings> ());

        public BoolSetting DevMode { get; private set; }

        /// <summary>
        /// Initializes the settings in the category.
        /// </summary>
        protected override void InitializeSettings () {
            DevMode = this.CreateBool ("BetaDev Mode")
                .SetDescription ("Turns all the hidden Designer Tools features that are currently in developpment, DO NOT LEAVE THIS SETTING ON")
                .AddWarningOnEnabled ("Do not leave this setting on when playing, turn on at your own risk")
                .SetDefault (false);
        }
    }
}