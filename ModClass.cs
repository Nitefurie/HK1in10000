using Modding;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HK1in10000
{
    public static class WavUtility
    {
        public static AudioClip ToAudioClip(byte[] wavBytes, string clipName)
        {
            int channels = BitConverter.ToInt16(wavBytes, 22);
            int sampleRate = BitConverter.ToInt32(wavBytes, 24);
            int dataStart = 44;
            int sampleCount = (wavBytes.Length - dataStart) / 2;

            float[] samples = new float[sampleCount];
            for (int i = 0; i < sampleCount; i++)
            {
                short raw = BitConverter.ToInt16(wavBytes, dataStart + i * 2);
                samples[i] = raw / 32768f;
            }

            AudioClip clip = AudioClip.Create(clipName, sampleCount / channels, channels, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
    public class HK1in10000 : Mod, IGlobalSettings<GlobalSettingsClass>, IMenuMod
    {
        private static GlobalSettingsClass GS { get; set;} = new GlobalSettingsClass();
        public void OnLoadGlobal(GlobalSettingsClass s)
        {
            GS = s;
            if (GS.chance < 1)
            {
                GS.chance = 10000;
            }
            if (GS.volume < 0)
            {
                GS.volume = 0;
            }
        }
        public GlobalSettingsClass OnSaveGlobal()
        {
            return GS;
        }
        public HK1in10000() : base("1 in 10000 Chance for Withered Foxy Jumpscare") { }
        public override string GetVersion() => "1.0";
        public override void Initialize()
        {
            Assembly asm = Assembly.GetExecutingAssembly();
            using (Stream stream = asm.GetManifestResourceStream("HK1in10000.Resources.jumpscare.wav"))
            using (MemoryStream ms = new MemoryStream())
            {
                stream.CopyTo(ms);
                UpdateRunner.JumpscareSound = WavUtility.ToAudioClip(ms.ToArray(), "jumpscare");
            }
            GameObject go = new GameObject("HK1in10000UpdateRunner");
            UnityEngine.Object.DontDestroyOnLoad(go);
            go.AddComponent<UpdateRunner>();
            LoadAnimationFrames(asm);
        }
        public bool ToggleButtonInsideMenu => false;
        public List<IMenuMod.MenuEntry> GetMenuData(IMenuMod.MenuEntry? toggleButtonEntry)
        {
            List<int> steps = new List<int> {10, 20, 50, 100, 200, 500, 1000, 2000, 5000, 10000, 20000, 50000, 100000, 200000, 500000, 1000000 };
            if (!steps.Contains(GS.chance))
            {
                steps.Add(GS.chance);
                steps.Sort();
            }
            return new List<IMenuMod.MenuEntry>
            {
                new IMenuMod.MenuEntry
                {
                    Name = "1 in X Chance",
                    Description = "To set an exact value edit the JSON file. A button to copy the file path is available. Please close the game before saving your custom value, otherwise it will be overwritten",
                    Values = steps.Select(v => v.ToString("N0")).ToArray(),
                    Saver = opt => GS.chance = steps[opt],
                    Loader = () => steps.IndexOf(GS.chance)
                },
                new IMenuMod.MenuEntry
                {
                    Name = "",
                    Description = null,
                    Values = new string[] { "" },
                    Saver = _ => { },
                    Loader = () => 0
                },
                new IMenuMod.MenuEntry
                {
                    Name = "Volume",
                    Description = null,
                    Values = Enumerable.Range(0, 11).Select(v => v.ToString()).ToArray(),
                    Saver = opt => GS.volume = opt,
                    Loader = () => GS.volume
                },
                new IMenuMod.MenuEntry
                {
                    Name = "",
                    Description = null,
                    Values = new string[] { "" },
                    Saver = _ => { },
                    Loader = () => 0
                },
                new IMenuMod.MenuEntry
                {
                    Name = "",
                    Description = null,
                    Values = new string[] { "" },
                    Saver = _ => { },
                    Loader = () => 0
                },
                new IMenuMod.MenuEntry
                {
                    Name = "",
                    Description = null,
                    Values = new string[] { "" },
                    Saver = _ => { },
                    Loader = () => 0
                },
                new IMenuMod.MenuEntry
                {
                    Name = "Copy JSON file path",
                    Description = "This file will only exist if at some point the game has been closed with this mod installed. Changes will be overwritten if the game is open when this file is modified",
                    Values = new string[] {"Copy Path"},
                    Saver = _ => GUIUtility.systemCopyBuffer = Path.Combine(Application.persistentDataPath,"HK1in10000.GlobalSettings.json"),
                    Loader = () => 0
                }
            };
        }
        private void LoadAnimationFrames(Assembly asm)
        {
            for (int frameIndex = 0; frameIndex <= 13; frameIndex++)
            {
                string resourceName = $"HK1in10000.Resources.Frames.tile{frameIndex:D3}.png";
                using (Stream stream = asm.GetManifestResourceStream(resourceName))
                {;
                        using (MemoryStream ms = new MemoryStream())
                    {
                        stream.CopyTo(ms);
                        Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                        tex.LoadImage(ms.ToArray());
                        Sprite sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                        UpdateRunner.AnimationFrames.Add(sprite);
                    }
                }
            }
        }
        public class UpdateRunner : MonoBehaviour
        {
            private void Awake()
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.spatialBlend = 0f;
                audioSource.playOnAwake = false;
                GameObject canvasGO = new GameObject("HK1in10000AnimCanvas");
                DontDestroyOnLoad(canvasGO);
                Canvas canvas = canvasGO.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 9999;
                GameObject imageGO = new GameObject("HK1in10000AnimImage");
                imageGO.transform.SetParent(canvasGO.transform, false);
                animImage = imageGO.AddComponent<Image>();
                animImage.preserveAspect = false;
                animImage.enabled = false;
                RectTransform rt = animImage.rectTransform;
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
            }
            private float timer = 0f;
            public static AudioClip JumpscareSound;
            private AudioSource audioSource;
            public static List<Sprite> AnimationFrames = new List<Sprite>();
            private Image animImage;
            private float frameTimer = 0f;
            int currentFrame = 0;
            private void Update()
            {
                timer += Time.unscaledDeltaTime;
                if (timer >= 1f)
                {
                    timer = 0f;
                    int roll = UnityEngine.Random.Range(1, GS.chance);
                    if (roll == 1)
                    {
                        GameObject selected = EventSystem.current?.currentSelectedGameObject;
                        if (selected == null || selected.name != "1 in X Chance")
                        {
                            frameTimer = 0f;
                            currentFrame = 0;
                            animImage.sprite = AnimationFrames[0];
                            animImage.enabled = true;
                            audioSource.PlayOneShot(JumpscareSound, GS.volume / 10f);
                        }
                    }
                }
                if (animImage)
                {
                    frameTimer += Time.unscaledDeltaTime;
                    if (frameTimer >= 0.05f)
                    {
                        frameTimer = 0f;
                        currentFrame++;
                        if (currentFrame >= AnimationFrames.Count)
                        {
                            animImage.enabled = false;
                        }
                        else
                        {
                            animImage.sprite = AnimationFrames[currentFrame];
                        }
                    }
                }
            }
        }
    }
    public class GlobalSettingsClass
    {
        public int volume = 5;
        public int chance = 10000;
    }
}