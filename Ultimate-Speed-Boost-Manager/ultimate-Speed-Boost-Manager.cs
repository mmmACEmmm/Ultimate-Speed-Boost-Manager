using System;
using System.Collections.Generic;
using System.IO;
using BepInEx;
using BepInEx.Configuration;
using GorillaLocomotion;
using UnityEngine;
using Valve.VR;

namespace GorillaTagSpeedBoost
{
    [BepInPlugin("com.yourname.gorillatag.speedboost", "Gorilla Tag Speed Boost", "1.0.0")]
    public class SpeedBoostMod : BaseUnityPlugin
    {
        private static class Defaults
        {
            public const float DefaultMultiplier = 1.2f;
            public const float DefaultMaxSpeed = 8f;
        }

        private Dictionary<string, BoostSettings> allSettings;
        private ConfigFile config;

        private bool showGUI = true;
        private Rect windowRect = new Rect(20, 20, 300, 400);
        private readonly Dictionary<string, bool> expandedSections = new Dictionary<string, bool>();

        private void Awake()
        {
            config = new ConfigFile(Path.Combine(Paths.ConfigPath, "SpeedBoostMod.cfg"), true);
            InitializeSettings();
        }

        private void InitializeSettings()
        {
            allSettings = new Dictionary<string, BoostSettings>
            {
                {"Left Primary", LoadOrCreateSettings("LeftPrimary")},
                {"Left Secondary", LoadOrCreateSettings("LeftSecondary")},
                {"Left Grab", LoadOrCreateSettings("LeftGrab")},
                {"Left Joystick", LoadOrCreateSettings("LeftJoystick")},
                {"Right Primary", LoadOrCreateSettings("RightPrimary")},
                {"Right Secondary", LoadOrCreateSettings("RightSecondary")},
                {"Right Grab", LoadOrCreateSettings("RightGrab")},
                {"Right Joystick", LoadOrCreateSettings("RightJoystick")},
                {"Defaults", LoadOrCreateSettings("Defaults")}
            };
        }

        private BoostSettings LoadOrCreateSettings(string settingName)
        {
            float multiplier = config.Bind("Multipliers", settingName, Defaults.DefaultMultiplier, "Multiplier for " + settingName).Value;
            float maxSpeed = config.Bind("MaxSpeeds", settingName, Defaults.DefaultMaxSpeed, "Max speed for " + settingName).Value;
            return new BoostSettings(multiplier, maxSpeed);
        }

        private void SaveSettings()
        {
            foreach (var kvp in allSettings)
            {
                config.Bind("Multipliers", kvp.Key, kvp.Value.Multiplier, "Multiplier for " + kvp.Key).Value = kvp.Value.Multiplier;
                config.Bind("MaxSpeeds", kvp.Key, kvp.Value.MaxSpeed, "Max speed for " + kvp.Key).Value = kvp.Value.MaxSpeed;
            }
            config.Save();
        }

        private void LoadSettings()
        {
            foreach (var kvp in allSettings)
            {
                kvp.Value.Multiplier = config.Bind("Multipliers", kvp.Key, Defaults.DefaultMultiplier, "Multiplier for " + kvp.Key).Value;
                kvp.Value.MaxSpeed = config.Bind("MaxSpeeds", kvp.Key, Defaults.DefaultMaxSpeed, "Max speed for " + kvp.Key).Value;
            }
        }

        private void Update()
        {
            ToggleGUIVisibility();
            ApplyBoostSettings();
        }

        private void OnGUI()
        {
            if (showGUI)
            {
                GUI.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.9f);
                windowRect = GUILayout.Window(0, windowRect, DrawWindow, "Speed Boost Settings");
            }
        }

        private void DrawWindow(int windowID)
        {
            GUI.backgroundColor = Color.white;
            GUI.contentColor = Color.yellow;

            foreach (var kvp in allSettings)
            {
                DrawControlSetButton(kvp.Key, kvp.Value);
            }

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Reset", GUILayout.Width(90)))
            {
                ResetToDefaults();
            }
            if (GUILayout.Button("Save", GUILayout.Width(90)))
            {
                SaveSettings();
            }
            if (GUILayout.Button("Load", GUILayout.Width(90)))
            {
                LoadSettings();
            }
            GUILayout.EndHorizontal();

            if (GUILayout.Button("Hide"))
            {
                showGUI = false;
            }

            GUI.DragWindow();
        }

        private void DrawControlSetButton(string label, BoostSettings settings)
        {
            expandedSections.TryAdd(label, false);

            GUI.backgroundColor = expandedSections[label] ? Color.cyan : Color.white;
            if (GUILayout.Button(label))
            {
                expandedSections[label] = !expandedSections[label];
            }

            if (expandedSections[label])
            {
                DrawControlSetSliders(label, settings);
            }
        }

        private void DrawControlSetSliders(string label, BoostSettings settings)
        {
            GUI.backgroundColor = Color.white;
            GUI.contentColor = Color.yellow;

            GUILayout.Label($"Max Speed: {settings.MaxSpeed:F2}");
            settings.MaxSpeed = GUILayout.HorizontalSlider(settings.MaxSpeed, 0f, 20f);

            GUILayout.Label($"Multiplier: {settings.Multiplier:F2}");
            settings.Multiplier = GUILayout.HorizontalSlider(settings.Multiplier, 0f, 3f);

            GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));
        }

        private void ApplyBoostSettings()
        {
            if (Player.Instance == null) return;

            BoostSettings activeSettings = GetActiveBoostSettings();
            Player.Instance.maxJumpSpeed = activeSettings.MaxSpeed;
            Player.Instance.jumpMultiplier = activeSettings.Multiplier;
        }

        private BoostSettings GetActiveBoostSettings()
        {
            if (SteamVR_Actions.gorillaTag_LeftJoystickClick.state) return allSettings["Left Joystick"];
            if (SteamVR_Actions.gorillaTag_RightJoystickClick.state) return allSettings["Right Joystick"];
            if (ControllerInputPoller.instance.leftControllerPrimaryButton) return allSettings["Left Primary"];
            if (ControllerInputPoller.instance.leftControllerSecondaryButton) return allSettings["Left Secondary"];
            if (ControllerInputPoller.instance.leftGrab) return allSettings["Left Grab"];
            if (ControllerInputPoller.instance.rightControllerPrimaryButton) return allSettings["Right Primary"];
            if (ControllerInputPoller.instance.rightControllerSecondaryButton) return allSettings["Right Secondary"];
            if (ControllerInputPoller.instance.rightGrab) return allSettings["Right Grab"];

            return allSettings["Defaults"];
        }

        private void ResetToDefaults()
        {
            InitializeSettings();
            SaveSettings();
        }

        private void ToggleGUIVisibility()
        {
            if (ControllerInputPoller.instance.leftGrab && ControllerInputPoller.instance.rightGrab)
            {
                showGUI = !showGUI;
                Debug.Log($"GUI visibility toggled. ShowGUI: {showGUI}");
            }
        }

        private class BoostSettings
        {
            public float Multiplier { get; set; }
            public float MaxSpeed { get; set; }

            public BoostSettings(float multiplier, float maxSpeed)
            {
                Multiplier = multiplier;
                MaxSpeed = maxSpeed;
            }
        }
    }
}