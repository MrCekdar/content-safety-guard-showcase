using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace ContentSafetyGuard.View__Interface_.SettingViewFolder.Cards.Detection
{

    public enum DetectionMode
    {
        Balanced,
        Strict,
        Maximum,
        Custom
    };

    public enum ScanFrequencyMode
    {
        High,
        Normal,
        Low
    }

    public class DetectionSettingsState
    {

        public DetectionMode DetectionMode { get; set; } = DetectionMode.Balanced; 

        public bool AiDetectionEnabled { get; set; } = true;
        public bool GridScanningEnabled { get; set; } = false;
        public int SensitivityPercent { get; set; } = 50;
        public int NsfwSensitivityPercent { get; set; } = 50;
        public ScanFrequencyMode ScanFrequency { get; set; } = ScanFrequencyMode.High;

        [JsonIgnore]
        public float NsfwThreshold
        {
            get
            {
                float sensitivity = NsfwSensitivityPercent / 100f;

                float maxThreshold = 0.95f; // sehr permissiv
                float minThreshold = 0.20f; // sehr streng, aber nicht alles blocken

                return maxThreshold - sensitivity * (maxThreshold - minThreshold);
            }
        }

    }
}
