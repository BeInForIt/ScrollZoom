using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using Barotrauma;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace ScrollZoom
{
    public static class FileLogger
    {
        private static readonly string PathFile =
            System.IO.Path.Combine(Environment.CurrentDirectory, "scrollzoom_log.txt");

        public static void Log(string message)
        {
            try
            {
                string line = $"[{DateTime.Now:HH:mm:ss.fff}] {message}";
                File.AppendAllText(PathFile, line + Environment.NewLine);
            }
            catch
            {
            }
        }
    }

    public sealed class ScrollZoomPlugin : IAssemblyPlugin, IDisposable
    {
        private const string HarmonyId = "hoppinhauler.scrollzoom";
        internal static Harmony HarmonyInstance;

        public void Initialize()
        {
            try
            {
                FileLogger.Log($"{HarmonyId}: Initialize start");
                LuaCsLogger.Log($"{HarmonyId}: Initialize start");
                ScrollZoomConfig.Load();
                HarmonyInstance = new Harmony(HarmonyId);
                HarmonyInstance.PatchAll(Assembly.GetExecutingAssembly());
                FileLogger.Log($"{HarmonyId}: Initialize completed");
            }
            catch (Exception ex)
            {
                FileLogger.Log($"{HarmonyId}: Initialize exception: {ex}");
            }
        }

        public void OnLoadCompleted()
        {
            FileLogger.Log($"{HarmonyId}: OnLoadCompleted");
        }

        public void PreInitPatching()
        {
            FileLogger.Log($"{HarmonyId}: PreInitPatching");
        }

        public void Dispose()
        {
            try
            {
                FileLogger.Log($"{HarmonyId}: Dispose start");
                HarmonyInstance?.UnpatchSelf();
                HarmonyInstance = null;
                ScrollZoomState.Reset();
                FileLogger.Log($"{HarmonyId}: Dispose completed");
            }
            catch (Exception ex)
            {
                FileLogger.Log($"{HarmonyId}: Dispose exception: {ex}");
            }
        }
    }

    internal static class ScrollZoomConfig
    {
        private const string ConfigFileName = "scrollzoom_config.txt";
        private const float DefaultMaxZoomOut = 0.20f;
        private const float DefaultMaxZoomIn = 2.25f;
        private const float DefaultZoomStepPerNotch = 0.06f;
        private const float DefaultFreecamChaseSpeed = 10.0f;
        private const float MinAllowedZoom = 0.05f;
        private const float MaxAllowedZoom = 8.0f;

        public static float MaxZoomOut = DefaultMaxZoomOut;
        public static float MaxZoomIn = DefaultMaxZoomIn;
        public static float ZoomStepPerNotch = DefaultZoomStepPerNotch;
        public static float FreecamChaseSpeed = DefaultFreecamChaseSpeed;

        public static void Load()
        {
            ResetDefaults();

            try
            {
                string configPath = ResolveConfigPath();
                EnsureConfigExists(configPath);
                ParseConfig(configPath);
                Clamp();
                FileLogger.Log(
                    $"CONFIG_LOADED path={configPath} maxZoomOut={MaxZoomOut} maxZoomIn={MaxZoomIn} zoomStep={ZoomStepPerNotch} freecamChaseSpeed={FreecamChaseSpeed}");
            }
            catch (Exception ex)
            {
                ResetDefaults();
                Clamp();
                FileLogger.Log($"CONFIG_LOAD_FAILED exception={ex}");
            }
        }

        private static void ResetDefaults()
        {
            MaxZoomOut = DefaultMaxZoomOut;
            MaxZoomIn = DefaultMaxZoomIn;
            ZoomStepPerNotch = DefaultZoomStepPerNotch;
            FreecamChaseSpeed = DefaultFreecamChaseSpeed;
        }

        private static void Clamp()
        {
            MaxZoomOut = MathHelper.Clamp(MaxZoomOut, MinAllowedZoom, MaxAllowedZoom - 0.05f);
            MaxZoomIn = MathHelper.Clamp(MaxZoomIn, MaxZoomOut + 0.05f, MaxAllowedZoom);
            ZoomStepPerNotch = MathHelper.Clamp(ZoomStepPerNotch, 0.01f, 1.0f);
            FreecamChaseSpeed = MathHelper.Clamp(FreecamChaseSpeed, 0.1f, 60.0f);
        }

        private static void ParseConfig(string configPath)
        {
            foreach (string rawLine in File.ReadAllLines(configPath))
            {
                string line = rawLine.Trim();
                if (string.IsNullOrEmpty(line) || line.StartsWith("#") || line.StartsWith("//"))
                {
                    continue;
                }

                int separatorIndex = line.IndexOf('=');
                if (separatorIndex <= 0 || separatorIndex >= line.Length - 1)
                {
                    continue;
                }

                string key = line.Substring(0, separatorIndex).Trim();
                string valueText = line.Substring(separatorIndex + 1).Trim();
                if (!float.TryParse(valueText, NumberStyles.Float, CultureInfo.InvariantCulture, out float value))
                {
                    FileLogger.Log($"CONFIG_INVALID_VALUE key={key} value={valueText}");
                    continue;
                }

                switch (key.ToLowerInvariant())
                {
                    case "maxzoomout":
                        MaxZoomOut = value;
                        break;
                    case "maxzoomin":
                        MaxZoomIn = value;
                        break;
                    case "zoomsteppernotch":
                        ZoomStepPerNotch = value;
                        break;
                    case "freecamchasespeed":
                        FreecamChaseSpeed = value;
                        break;
                }
            }
        }

        private static void EnsureConfigExists(string configPath)
        {
            if (File.Exists(configPath))
            {
                return;
            }

            string directory = Path.GetDirectoryName(configPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(configPath, BuildDefaultConfigText());
        }

        private static string ResolveConfigPath()
        {
            string currentDirectory = Environment.CurrentDirectory;
            string assemblyDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? currentDirectory;
            string[] candidateDirectories =
            {
                Path.Combine(currentDirectory, "LocalMods", "ScrollZoom"),
                Path.Combine(currentDirectory, "LocalMods", "scrollzoom"),
                assemblyDirectory,
                currentDirectory
            };

            foreach (string directory in candidateDirectories)
            {
                if (!string.IsNullOrWhiteSpace(directory) && Directory.Exists(directory))
                {
                    return Path.Combine(directory, ConfigFileName);
                }
            }

            return Path.Combine(currentDirectory, ConfigFileName);
        }

        private static string BuildDefaultConfigText()
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("# ScrollZoom config");
            builder.AppendLine("# MaxZoomOut: lower value = camera can zoom farther away.");
            builder.AppendLine("# MaxZoomIn: higher value = camera can zoom in closer.");
            builder.AppendLine("# ZoomStepPerNotch: wheel sensitivity, lower is smoother.");
            builder.AppendLine("# FreecamChaseSpeed: how fast freecam follows the cursor.");
            builder.AppendLine("MaxZoomOut=0.20");
            builder.AppendLine("MaxZoomIn=2.25");
            builder.AppendLine("ZoomStepPerNotch=0.06");
            builder.AppendLine("FreecamChaseSpeed=10.0");
            return builder.ToString();
        }
    }

    internal static class ScrollZoomState
    {
        public static float MinZoom => ScrollZoomConfig.MaxZoomOut;
        public static float MaxZoom => ScrollZoomConfig.MaxZoomIn;
        public static float ZoomStepPerNotch => ScrollZoomConfig.ZoomStepPerNotch;
        public static float FreecamChaseSpeed => ScrollZoomConfig.FreecamChaseSpeed;

        public static bool FreecamEnabled;
        public static bool FreecamPositionFrozen;
        public static bool ScrollZoomEnabled = true;
        public static bool LockToCharacterEnabled;

        public static bool ToggleFreecamWasDown;
        public static bool ToggleFreezeWasDown;
        public static bool ToggleZoomWasDown;
        public static bool ToggleLockWasDown;

        public static bool HasCustomZoom;
        public static float CustomZoom = 1.0f;

        public static bool HasFreecamTarget;
        public static Vector2 FreecamTarget = Vector2.Zero;

        public static int FreecamToggleCounter;
        public static int FreezeToggleCounter;
        public static int ZoomToggleCounter;
        public static int LockToggleCounter;
        public static int WheelCounter;
        public static int ApplyZoomCounter;
        public static int ApplyFreecamCounter;
        public static int ApplyLockCounter;
        public static int LogCounter;

        public static void Reset()
        {
            FreecamEnabled = false;
            FreecamPositionFrozen = false;
            ScrollZoomEnabled = true;
            LockToCharacterEnabled = false;

            ToggleFreecamWasDown = false;
            ToggleFreezeWasDown = false;
            ToggleZoomWasDown = false;
            ToggleLockWasDown = false;

            HasCustomZoom = false;
            CustomZoom = 1.0f;

            HasFreecamTarget = false;
            FreecamTarget = Vector2.Zero;

            FreecamToggleCounter = 0;
            FreezeToggleCounter = 0;
            ZoomToggleCounter = 0;
            LockToggleCounter = 0;
            WheelCounter = 0;
            ApplyZoomCounter = 0;
            ApplyFreecamCounter = 0;
            ApplyLockCounter = 0;
            LogCounter = 0;
        }
    }

    [HarmonyPatch(typeof(Camera), "MoveCamera", new[]
    {
        typeof(float),
        typeof(bool),
        typeof(bool),
        typeof(bool),
        typeof(bool?)
    })]
    internal static class CameraMoveCameraPatch
    {
        private static readonly FieldInfo PositionField =
            AccessTools.Field(typeof(Camera), "position");

        private static readonly FieldInfo PreviousOffsetField =
            AccessTools.Field(typeof(Camera), "previousOffset");

        private static void Prefix(Camera __instance, float deltaTime)
        {
            HandleToggles(__instance);
            HandleScrollZoomInput(__instance);

            if (__instance == null)
            {
                return;
            }

            if (!ScrollZoomState.FreecamEnabled)
            {
                ScrollZoomState.HasFreecamTarget = false;
                return;
            }

            Vector2 centerWorld = GetCameraCenterWorld(__instance);

            if (!ScrollZoomState.HasFreecamTarget)
            {
                ScrollZoomState.FreecamTarget = centerWorld;
                ScrollZoomState.HasFreecamTarget = true;
                FileLogger.Log($"FREECAM_INIT target={ScrollZoomState.FreecamTarget}");
            }

            if (ScrollZoomState.FreecamPositionFrozen)
            {
                __instance.TargetPos = ScrollZoomState.FreecamTarget;
                return;
            }

            Vector2 cursorWorld = __instance.ScreenToWorld(PlayerInput.MousePosition);
            Vector2 worldDelta = cursorWorld - centerWorld;
            float dt = Math.Max(deltaTime, 0.0001f);
            ScrollZoomState.FreecamTarget += worldDelta * ScrollZoomState.FreecamChaseSpeed * dt;
            __instance.TargetPos = ScrollZoomState.FreecamTarget;
        }

        private static void Postfix(Camera __instance, bool allowMove)
        {
            if (__instance == null)
            {
                return;
            }

            if (!allowMove)
            {
                return;
            }

            if (Screen.Selected != GameMain.GameScreen)
            {
                return;
            }

            if (ScrollZoomState.FreecamEnabled)
            {
                ForceCameraPosition(__instance, ScrollZoomState.FreecamTarget);
                ScrollZoomState.ApplyFreecamCounter++;

                if (ScrollZoomState.ApplyFreecamCounter <= 20 || ScrollZoomState.ApplyFreecamCounter % 120 == 0)
                {
                    FileLogger.Log(
                        $"APPLY_FREECAM #{ScrollZoomState.ApplyFreecamCounter} target={ScrollZoomState.FreecamTarget}");
                }
            }
            else if (ScrollZoomState.LockToCharacterEnabled && __instance.TargetPos != Vector2.Zero)
            {
                ForceCameraPosition(__instance, __instance.TargetPos);
                ScrollZoomState.ApplyLockCounter++;

                if (ScrollZoomState.ApplyLockCounter <= 20 || ScrollZoomState.ApplyLockCounter % 120 == 0)
                {
                    FileLogger.Log(
                        $"APPLY_LOCK #{ScrollZoomState.ApplyLockCounter} target={__instance.TargetPos}");
                }
            }

            ApplyScrollZoom(__instance);
        }

        private static void HandleToggles(Camera __instance)
        {
            bool freecamDown = PlayerInput.KeyDown(Keys.F);
            if (freecamDown && !ScrollZoomState.ToggleFreecamWasDown)
            {
                ScrollZoomState.FreecamEnabled = !ScrollZoomState.FreecamEnabled;
                ScrollZoomState.FreecamPositionFrozen = false;
                ScrollZoomState.FreecamToggleCounter++;

                if (ScrollZoomState.FreecamEnabled && __instance != null)
                {
                    ScrollZoomState.FreecamTarget = GetCameraCenterWorld(__instance);
                    ScrollZoomState.HasFreecamTarget = true;
                }
                else
                {
                    ScrollZoomState.HasFreecamTarget = false;
                }

                FileLogger.Log(
                    $"TOGGLE_FREECAM #{ScrollZoomState.FreecamToggleCounter} enabled={ScrollZoomState.FreecamEnabled} target={ScrollZoomState.FreecamTarget}");
            }
            ScrollZoomState.ToggleFreecamWasDown = freecamDown;

            bool freezeDown = PlayerInput.KeyDown(Keys.NumPad4);
            if (freezeDown && !ScrollZoomState.ToggleFreezeWasDown && ScrollZoomState.FreecamEnabled)
            {
                ScrollZoomState.FreecamPositionFrozen = !ScrollZoomState.FreecamPositionFrozen;
                ScrollZoomState.FreezeToggleCounter++;

                if (__instance != null)
                {
                    ScrollZoomState.FreecamTarget = GetCameraCenterWorld(__instance);
                    ScrollZoomState.HasFreecamTarget = true;
                }

                FileLogger.Log(
                    $"TOGGLE_FREEZE #{ScrollZoomState.FreezeToggleCounter} enabled={ScrollZoomState.FreecamPositionFrozen} target={ScrollZoomState.FreecamTarget}");
            }
            ScrollZoomState.ToggleFreezeWasDown = freezeDown;

            bool zoomDown = PlayerInput.KeyDown(Keys.NumPad5);
            if (zoomDown && !ScrollZoomState.ToggleZoomWasDown)
            {
                ScrollZoomState.ScrollZoomEnabled = !ScrollZoomState.ScrollZoomEnabled;
                ScrollZoomState.ZoomToggleCounter++;

                if (!ScrollZoomState.ScrollZoomEnabled)
                {
                    ScrollZoomState.HasCustomZoom = false;
                }
                else if (__instance != null)
                {
                    ScrollZoomState.CustomZoom = MathHelper.Clamp(__instance.Zoom, ScrollZoomState.MinZoom, ScrollZoomState.MaxZoom);
                    ScrollZoomState.HasCustomZoom = true;
                }

                FileLogger.Log(
                    $"TOGGLE_SCROLL_ZOOM #{ScrollZoomState.ZoomToggleCounter} enabled={ScrollZoomState.ScrollZoomEnabled} hasCustomZoom={ScrollZoomState.HasCustomZoom} customZoom={ScrollZoomState.CustomZoom}");
            }
            ScrollZoomState.ToggleZoomWasDown = zoomDown;

            bool lockDown = PlayerInput.KeyDown(Keys.NumPad6);
            if (lockDown && !ScrollZoomState.ToggleLockWasDown)
            {
                ScrollZoomState.LockToCharacterEnabled = !ScrollZoomState.LockToCharacterEnabled;
                ScrollZoomState.LockToggleCounter++;

                FileLogger.Log(
                    $"TOGGLE_LOCK #{ScrollZoomState.LockToggleCounter} enabled={ScrollZoomState.LockToCharacterEnabled}");
            }
            ScrollZoomState.ToggleLockWasDown = lockDown;
        }

        private static void HandleScrollZoomInput(Camera __instance)
        {
            if (!ScrollZoomState.ScrollZoomEnabled || __instance == null)
            {
                return;
            }

            float wheel = PlayerInput.ScrollWheelSpeed;
            if (wheel == 0f)
            {
                return;
            }

            if (!ScrollZoomState.HasCustomZoom)
            {
                ScrollZoomState.CustomZoom = MathHelper.Clamp(__instance.Zoom, ScrollZoomState.MinZoom, ScrollZoomState.MaxZoom);
                ScrollZoomState.HasCustomZoom = true;
            }

            float notches = wheel / 120f;
            float oldZoom = ScrollZoomState.CustomZoom;
            float newZoom = oldZoom + notches * ScrollZoomState.ZoomStepPerNotch;
            newZoom = MathHelper.Clamp(newZoom, ScrollZoomState.MinZoom, ScrollZoomState.MaxZoom);

            ScrollZoomState.CustomZoom = newZoom;
            ScrollZoomState.WheelCounter++;

            FileLogger.Log(
                $"WHEEL #{ScrollZoomState.WheelCounter} wheel={wheel} notches={notches} oldCustomZoom={oldZoom} newCustomZoom={newZoom}");
        }

        private static void ApplyScrollZoom(Camera __instance)
        {
            if (!ScrollZoomState.ScrollZoomEnabled || !ScrollZoomState.HasCustomZoom)
            {
                ScrollZoomState.LogCounter++;
                if (ScrollZoomState.LogCounter <= 10 || ScrollZoomState.LogCounter % 180 == 0)
                {
                    FileLogger.Log(
                        $"VANILLA_ZOOM zoom={__instance.Zoom} freecam={ScrollZoomState.FreecamEnabled} lock={ScrollZoomState.LockToCharacterEnabled} scrollZoom={ScrollZoomState.ScrollZoomEnabled}");
                }
                return;
            }

            float finalZoom = MathHelper.Clamp(ScrollZoomState.CustomZoom, ScrollZoomState.MinZoom, ScrollZoomState.MaxZoom);
            __instance.Zoom = finalZoom;
            ScrollZoomState.ApplyZoomCounter++;

            if (ScrollZoomState.ApplyZoomCounter <= 20 || ScrollZoomState.ApplyZoomCounter % 120 == 0)
            {
                FileLogger.Log(
                    $"APPLY_ZOOM #{ScrollZoomState.ApplyZoomCounter} finalZoom={finalZoom} freecam={ScrollZoomState.FreecamEnabled} lock={ScrollZoomState.LockToCharacterEnabled}");
            }
        }

        private static void ForceCameraPosition(Camera camera, Vector2 target)
        {
            if (PreviousOffsetField != null)
            {
                PreviousOffsetField.SetValue(camera, Vector2.Zero);
            }

            if (PositionField != null)
            {
                PositionField.SetValue(camera, target);
            }
        }

        private static Vector2 GetScreenCenter(Camera camera)
        {
            return new Vector2(camera.Resolution.X * 0.5f, camera.Resolution.Y * 0.5f);
        }

        private static Vector2 GetCameraCenterWorld(Camera camera)
        {
            return camera.ScreenToWorld(GetScreenCenter(camera));
        }
    }
}
