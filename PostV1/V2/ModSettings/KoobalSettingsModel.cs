using System;
using System.Collections.Generic;
using System.Globalization;

namespace PartSearchSuggest.PostV1.V2.ModSettings
{
    /// <summary>
    /// Persisted Koobal settings model (PluginData-backed at wire-up).
    /// Source of truth for Settings tab + parts-list experience (Track S and Track R).
    /// </summary>
    [PostV1Phase(PostV1Phase.E_V2PartsListAndSettings)]
    internal sealed class KoobalSettingsModel
    {
        /// <summary>Track S: slide-grow the parts panel (same family as dropdown slide).</summary>
        public bool SlideExpandEnabled { get; set; }

        /// <summary>Additional horizontal width in UI units (0 = stock width; expand adds to rest geometry).</summary>
        public float ExpandWidthAmount { get; set; }

        /// <summary>Optional extra vertical size (0 = stock height).</summary>
        public float ExpandHeightAmount { get; set; }

        public PartsListExperience.PartsListIconSize IconSize { get; set; }

        public PartsListExperience.PartsListStyle ListStyle { get; set; }

        /// <summary>When true, soft-yield / layout-only if organizers detected.</summary>
        public bool OrganizerCompatibilityMode { get; set; }

        /// <summary>Experimental in-place or owned virtualization (still gated).</summary>
        public bool VirtualizationExperimental { get; set; }

        /// <summary>Preferred architecture track. Default SlideExpand; Rebuild only after go/no-go.</summary>
        public PartsListExperience.PartsListArchitectureTrack ArchitectureTrack { get; set; }

        /// <summary>Search section placeholder — suggestion density hint (1 = default).</summary>
        public int SearchSuggestionDensity { get; set; }

        /// <summary>History max entries (aligns with shipping history cap when wired).</summary>
        public int HistoryMaxEntries { get; set; }

        /// <summary>Advanced: allow Track R experimental UI when feature gate later allows.</summary>
        public bool AllowRebuildExperimental { get; set; }

        /// <summary>Advanced: force Track S even if model says Rebuild.</summary>
        public bool ForceSlideExpandTrack { get; set; }

        public static KoobalSettingsModel CreateDefaults()
        {
            return new KoobalSettingsModel
            {
                SlideExpandEnabled = false,
                ExpandWidthAmount = 0f,
                ExpandHeightAmount = 0f,
                IconSize = PartsListExperience.PartsListIconSize.Comfortable,
                ListStyle = PartsListExperience.PartsListStyle.Grid,
                OrganizerCompatibilityMode = true,
                VirtualizationExperimental = false,
                ArchitectureTrack = PartsListExperience.PartsListArchitectureTrack.SlideExpand,
                SearchSuggestionDensity = 1,
                HistoryMaxEntries = 12,
                AllowRebuildExperimental = false,
                ForceSlideExpandTrack = false
            };
        }

        public KoobalSettingsModel Clone()
        {
            return new KoobalSettingsModel
            {
                SlideExpandEnabled = SlideExpandEnabled,
                ExpandWidthAmount = ExpandWidthAmount,
                ExpandHeightAmount = ExpandHeightAmount,
                IconSize = IconSize,
                ListStyle = ListStyle,
                OrganizerCompatibilityMode = OrganizerCompatibilityMode,
                VirtualizationExperimental = VirtualizationExperimental,
                ArchitectureTrack = ArchitectureTrack,
                SearchSuggestionDensity = SearchSuggestionDensity,
                HistoryMaxEntries = HistoryMaxEntries,
                AllowRebuildExperimental = AllowRebuildExperimental,
                ForceSlideExpandTrack = ForceSlideExpandTrack
            };
        }
    }

    /// <summary>Load/save port for <see cref="KoobalSettingsModel"/>.</summary>
    [PostV1Phase(PostV1Phase.E_V2PartsListAndSettings)]
    internal interface IKoobalSettingsStore
    {
        KoobalSettingsModel Current { get; }

        /// <summary>Replace current with defaults and persist.</summary>
        void ResetToDefaults();

        /// <summary>Reload from persistence.</summary>
        void Reload();

        /// <summary>Apply a full model snapshot and persist.</summary>
        void Save(KoobalSettingsModel model);

        /// <summary>Patch individual fields (nulls ignored) and persist when anything changes.</summary>
        bool ApplyPatch(KoobalSettingsPatch patch);
    }

    /// <summary>Optional field patch for settings UI binds.</summary>
    [PostV1Phase(PostV1Phase.E_V2PartsListAndSettings)]
    internal sealed class KoobalSettingsPatch
    {
        public bool? SlideExpandEnabled { get; set; }
        public float? ExpandWidthAmount { get; set; }
        public float? ExpandHeightAmount { get; set; }
        public PartsListExperience.PartsListIconSize? IconSize { get; set; }
        public PartsListExperience.PartsListStyle? ListStyle { get; set; }
        public bool? OrganizerCompatibilityMode { get; set; }
        public bool? VirtualizationExperimental { get; set; }
        public PartsListExperience.PartsListArchitectureTrack? ArchitectureTrack { get; set; }
        public int? SearchSuggestionDensity { get; set; }
        public int? HistoryMaxEntries { get; set; }
        public bool? AllowRebuildExperimental { get; set; }
        public bool? ForceSlideExpandTrack { get; set; }
    }

    /// <summary>
    /// Concrete store against <see cref="ISettingsPersistence"/>. Pure — no Unity.
    /// </summary>
    [PostV1Phase(PostV1Phase.E_V2PartsListAndSettings)]
    internal sealed class KoobalSettingsStore : IKoobalSettingsStore
    {
        private readonly ISettingsPersistence _persistence;
        private KoobalSettingsModel _current;

        public KoobalSettingsStore(ISettingsPersistence persistence, bool loadOnConstruct = true)
        {
            _persistence = persistence ?? throw new ArgumentNullException(nameof(persistence));
            _current = KoobalSettingsModel.CreateDefaults();
            if (loadOnConstruct)
            {
                Reload();
            }
        }

        public KoobalSettingsModel Current => _current.Clone();

        public void ResetToDefaults()
        {
            _current = KoobalSettingsModel.CreateDefaults();
            Persist();
        }

        public void Reload()
        {
            KoobalSettingsModel loaded = _persistence.Load();
            _current = Sanitize(loaded ?? KoobalSettingsModel.CreateDefaults());
        }

        public void Save(KoobalSettingsModel model)
        {
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            _current = Sanitize(model.Clone());
            Persist();
        }

        public bool ApplyPatch(KoobalSettingsPatch patch)
        {
            if (patch == null)
            {
                return false;
            }

            bool changed = false;
            KoobalSettingsModel m = _current;

            if (patch.SlideExpandEnabled.HasValue && m.SlideExpandEnabled != patch.SlideExpandEnabled.Value)
            {
                m.SlideExpandEnabled = patch.SlideExpandEnabled.Value;
                changed = true;
            }

            if (patch.ExpandWidthAmount.HasValue
                && !NearlyEqual(m.ExpandWidthAmount, patch.ExpandWidthAmount.Value))
            {
                m.ExpandWidthAmount = patch.ExpandWidthAmount.Value;
                changed = true;
            }

            if (patch.ExpandHeightAmount.HasValue
                && !NearlyEqual(m.ExpandHeightAmount, patch.ExpandHeightAmount.Value))
            {
                m.ExpandHeightAmount = patch.ExpandHeightAmount.Value;
                changed = true;
            }

            if (patch.IconSize.HasValue && m.IconSize != patch.IconSize.Value)
            {
                m.IconSize = patch.IconSize.Value;
                changed = true;
            }

            if (patch.ListStyle.HasValue && m.ListStyle != patch.ListStyle.Value)
            {
                m.ListStyle = patch.ListStyle.Value;
                changed = true;
            }

            if (patch.OrganizerCompatibilityMode.HasValue
                && m.OrganizerCompatibilityMode != patch.OrganizerCompatibilityMode.Value)
            {
                m.OrganizerCompatibilityMode = patch.OrganizerCompatibilityMode.Value;
                changed = true;
            }

            if (patch.VirtualizationExperimental.HasValue
                && m.VirtualizationExperimental != patch.VirtualizationExperimental.Value)
            {
                m.VirtualizationExperimental = patch.VirtualizationExperimental.Value;
                changed = true;
            }

            if (patch.ArchitectureTrack.HasValue && m.ArchitectureTrack != patch.ArchitectureTrack.Value)
            {
                m.ArchitectureTrack = patch.ArchitectureTrack.Value;
                changed = true;
            }

            if (patch.SearchSuggestionDensity.HasValue
                && m.SearchSuggestionDensity != patch.SearchSuggestionDensity.Value)
            {
                m.SearchSuggestionDensity = patch.SearchSuggestionDensity.Value;
                changed = true;
            }

            if (patch.HistoryMaxEntries.HasValue && m.HistoryMaxEntries != patch.HistoryMaxEntries.Value)
            {
                m.HistoryMaxEntries = patch.HistoryMaxEntries.Value;
                changed = true;
            }

            if (patch.AllowRebuildExperimental.HasValue
                && m.AllowRebuildExperimental != patch.AllowRebuildExperimental.Value)
            {
                m.AllowRebuildExperimental = patch.AllowRebuildExperimental.Value;
                changed = true;
            }

            if (patch.ForceSlideExpandTrack.HasValue
                && m.ForceSlideExpandTrack != patch.ForceSlideExpandTrack.Value)
            {
                m.ForceSlideExpandTrack = patch.ForceSlideExpandTrack.Value;
                changed = true;
            }

            if (!changed)
            {
                return false;
            }

            _current = Sanitize(m);
            Persist();
            return true;
        }

        private void Persist()
        {
            _persistence.Save(_current.Clone());
        }

        internal static KoobalSettingsModel Sanitize(KoobalSettingsModel model)
        {
            KoobalSettingsModel d = KoobalSettingsModel.CreateDefaults();
            if (model == null)
            {
                return d;
            }

            model.ExpandWidthAmount = ClampNonNegative(model.ExpandWidthAmount);
            model.ExpandHeightAmount = ClampNonNegative(model.ExpandHeightAmount);
            if (!Enum.IsDefined(typeof(PartsListExperience.PartsListIconSize), model.IconSize))
            {
                model.IconSize = d.IconSize;
            }

            if (!Enum.IsDefined(typeof(PartsListExperience.PartsListStyle), model.ListStyle))
            {
                model.ListStyle = d.ListStyle;
            }

            if (!Enum.IsDefined(typeof(PartsListExperience.PartsListArchitectureTrack), model.ArchitectureTrack))
            {
                model.ArchitectureTrack = d.ArchitectureTrack;
            }

            if (model.SearchSuggestionDensity < 1)
            {
                model.SearchSuggestionDensity = 1;
            }

            if (model.HistoryMaxEntries < 1)
            {
                model.HistoryMaxEntries = d.HistoryMaxEntries;
            }

            if (model.HistoryMaxEntries > 64)
            {
                model.HistoryMaxEntries = 64;
            }

            return model;
        }

        private static float ClampNonNegative(float value)
        {
            return value < 0f ? 0f : value;
        }

        private static bool NearlyEqual(float a, float b)
        {
            return Math.Abs(a - b) < 0.0001f;
        }
    }

    /// <summary>Persistence boundary for settings cfg / PluginData.</summary>
    [PostV1Phase(PostV1Phase.E_V2PartsListAndSettings)]
    internal interface ISettingsPersistence
    {
        KoobalSettingsModel Load();

        void Save(KoobalSettingsModel model);
    }

    /// <summary>In-memory persistence for architecture self-check.</summary>
    [PostV1Phase(PostV1Phase.E_V2PartsListAndSettings)]
    internal sealed class MemorySettingsPersistence : ISettingsPersistence
    {
        private KoobalSettingsModel _stored;

        public int SaveCount { get; private set; }

        public KoobalSettingsModel Load()
        {
            return _stored != null ? _stored.Clone() : KoobalSettingsModel.CreateDefaults();
        }

        public void Save(KoobalSettingsModel model)
        {
            _stored = model != null ? model.Clone() : KoobalSettingsModel.CreateDefaults();
            SaveCount++;
        }

        public void Seed(KoobalSettingsModel model)
        {
            _stored = model != null ? model.Clone() : null;
        }
    }

    /// <summary>Throws — documents PluginData wire-up boundary.</summary>
    [PostV1Phase(PostV1Phase.E_V2PartsListAndSettings)]
    internal sealed class UnwiredSettingsPersistence : ISettingsPersistence
    {
        public KoobalSettingsModel Load()
        {
            throw new NotImplementedException(
                "UnwiredSettingsPersistence.Load — wire PluginData/KoobalSettings.cfg via SettingsCfgCodec.");
        }

        public void Save(KoobalSettingsModel model)
        {
            throw new NotImplementedException(
                "UnwiredSettingsPersistence.Save — wire PluginData/KoobalSettings.cfg via SettingsCfgCodec.");
        }
    }

    /// <summary>
    /// Pure key=value codec for KoobalSettings.cfg lines. No file IO.
    /// </summary>
    [PostV1Phase(PostV1Phase.E_V2PartsListAndSettings)]
    internal static class SettingsCfgCodec
    {
        public static KoobalSettingsModel Parse(IEnumerable<string> lines)
        {
            KoobalSettingsModel model = KoobalSettingsModel.CreateDefaults();
            if (lines == null)
            {
                return model;
            }

            foreach (string raw in lines)
            {
                if (string.IsNullOrWhiteSpace(raw))
                {
                    continue;
                }

                string line = raw.Trim();
                if (line.StartsWith("//", StringComparison.Ordinal) || line.StartsWith("#", StringComparison.Ordinal))
                {
                    continue;
                }

                int eq = line.IndexOf('=');
                if (eq <= 0)
                {
                    continue;
                }

                string key = line.Substring(0, eq).Trim();
                string value = line.Substring(eq + 1).Trim();
                ApplyKey(model, key, value);
            }

            return KoobalSettingsStore.Sanitize(model);
        }

        public static string[] Format(KoobalSettingsModel model)
        {
            model = KoobalSettingsStore.Sanitize(model ?? KoobalSettingsModel.CreateDefaults());
            var lines = new List<string>
            {
                "// Koobal Search Engine — settings (architecture codec)",
                "SlideExpandEnabled=" + Bool(model.SlideExpandEnabled),
                "ExpandWidthAmount=" + Float(model.ExpandWidthAmount),
                "ExpandHeightAmount=" + Float(model.ExpandHeightAmount),
                "IconSize=" + model.IconSize,
                "ListStyle=" + model.ListStyle,
                "OrganizerCompatibilityMode=" + Bool(model.OrganizerCompatibilityMode),
                "VirtualizationExperimental=" + Bool(model.VirtualizationExperimental),
                "ArchitectureTrack=" + model.ArchitectureTrack,
                "SearchSuggestionDensity=" + model.SearchSuggestionDensity.ToString(CultureInfo.InvariantCulture),
                "HistoryMaxEntries=" + model.HistoryMaxEntries.ToString(CultureInfo.InvariantCulture),
                "AllowRebuildExperimental=" + Bool(model.AllowRebuildExperimental),
                "ForceSlideExpandTrack=" + Bool(model.ForceSlideExpandTrack)
            };
            return lines.ToArray();
        }

        private static void ApplyKey(KoobalSettingsModel model, string key, string value)
        {
            switch (key)
            {
                case "SlideExpandEnabled":
                    model.SlideExpandEnabled = ParseBool(value, model.SlideExpandEnabled);
                    break;
                case "ExpandWidthAmount":
                    model.ExpandWidthAmount = ParseFloat(value, model.ExpandWidthAmount);
                    break;
                case "ExpandHeightAmount":
                    model.ExpandHeightAmount = ParseFloat(value, model.ExpandHeightAmount);
                    break;
                case "IconSize":
                    PartsListExperience.PartsListIconSize icon;
                    if (Enum.TryParse(value, true, out icon))
                    {
                        model.IconSize = icon;
                    }

                    break;
                case "ListStyle":
                    PartsListExperience.PartsListStyle style;
                    if (Enum.TryParse(value, true, out style))
                    {
                        model.ListStyle = style;
                    }

                    break;
                case "OrganizerCompatibilityMode":
                    model.OrganizerCompatibilityMode = ParseBool(value, model.OrganizerCompatibilityMode);
                    break;
                case "VirtualizationExperimental":
                    model.VirtualizationExperimental = ParseBool(value, model.VirtualizationExperimental);
                    break;
                case "ArchitectureTrack":
                    PartsListExperience.PartsListArchitectureTrack track;
                    if (Enum.TryParse(value, true, out track))
                    {
                        model.ArchitectureTrack = track;
                    }

                    break;
                case "SearchSuggestionDensity":
                    model.SearchSuggestionDensity = ParseInt(value, model.SearchSuggestionDensity);
                    break;
                case "HistoryMaxEntries":
                    model.HistoryMaxEntries = ParseInt(value, model.HistoryMaxEntries);
                    break;
                case "AllowRebuildExperimental":
                    model.AllowRebuildExperimental = ParseBool(value, model.AllowRebuildExperimental);
                    break;
                case "ForceSlideExpandTrack":
                    model.ForceSlideExpandTrack = ParseBool(value, model.ForceSlideExpandTrack);
                    break;
            }
        }

        private static string Bool(bool value)
        {
            return value ? "true" : "false";
        }

        private static string Float(float value)
        {
            return value.ToString("0.###", CultureInfo.InvariantCulture);
        }

        private static bool ParseBool(string value, bool fallback)
        {
            if (string.Equals(value, "true", StringComparison.OrdinalIgnoreCase)
                || value == "1")
            {
                return true;
            }

            if (string.Equals(value, "false", StringComparison.OrdinalIgnoreCase)
                || value == "0")
            {
                return false;
            }

            return fallback;
        }

        private static float ParseFloat(string value, float fallback)
        {
            float parsed;
            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out parsed))
            {
                return parsed;
            }

            return fallback;
        }

        private static int ParseInt(string value, int fallback)
        {
            int parsed;
            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed))
            {
                return parsed;
            }

            return fallback;
        }
    }
}
