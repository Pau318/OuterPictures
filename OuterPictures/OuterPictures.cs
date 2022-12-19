using Epic.OnlineServices;
using HarmonyLib;
using OWML.Common;
using OWML.ModHelper;
using OWML.Utils;
using System.Drawing;
using System.EnterpriseServices.CompensatingResourceManager;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SocialPlatforms.Impl;

namespace OuterPictures
{
    public class OuterPictures : ModBehaviour
    {
        public static OuterPictures Instance;
        public static RenderTexture rt;
        public static bool probeActive=false;
        public static string directory_name = "/Outer Pictures";
        public static string frame, paperType= "[none]";
        public static int size = 512;
        public static Texture2D paper, picture = new Texture2D(size, size);
        public static int startX=0, startY=0;

        private void Awake()
        {
            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());
            Instance = this;

        }

        [HarmonyPatch]
        public class MyPatchClass
        {
            [HarmonyPrefix]
            [HarmonyPatch(typeof(ProbeCamera), nameof(ProbeCamera.TakeSnapshot))]
            public static void ProbeCamera_TakeSnapshot_prefix()
            {
                probeActive = true;
            }

            [HarmonyPrefix]
            [HarmonyPatch(typeof(ProbeLauncherUI), nameof(ProbeLauncherUI.HideProbeHUD))]
            public static void ProbeLauncherUI_HideProbeHUD_prefix()
            {
                probeActive = false;
            }

            [HarmonyPrefix]
            [HarmonyPatch(typeof(ProbeLauncherUI),nameof(ProbeLauncherUI.OnTakeSnapshot))]
            public static void ProbeLauncherUI_OnTakeSnapshot_prefix(RenderTexture snapshot, ProbeCamera camera, float secondsAnchored) {
                rt = snapshot;
            }
        }

        private void Start()
        {
            /*var bundle = ModHelper.Assets.LoadBundle("assets/my_bundle");
            paper = bundle.LoadAsset<Texture2D>("assets/paper.png");*/
            paper = ModHelper.Assets.GetTexture("assets/paper.png");
        }

        public override void Configure(IModConfig config)
        {
            var new_directory_name= config.GetSettingsValue<string>("Directory Name");
            if (!new_directory_name.Equals(directory_name)) {
                if (Directory.Exists(new_directory_name)) {
                    directory_name = new_directory_name;
                    ModHelper.Console.WriteLine($"Target directory has been successfully updated to \"{directory_name}\"",MessageType.Success);
                }
                else
                {
                    ModHelper.Console.WriteLine($"Couldn't find directory \"{new_directory_name}\"", MessageType.Error);
                }
            } 
            frame = config.GetSettingsValue<string>("Frame");
        }

        public void Update()
        { 
            if(probeActive && Keyboard.current[Key.P].wasPressedThisFrame)
            {
                printPicture();
            }
        }

        public void printPicture()
        {
            Locator.GetPlayerAudioController().PlayPatchPuncture();
            int n = 1;

            Texture2D picture = getPicture();

            if (!Directory.Exists(directory_name)) Directory.CreateDirectory(directory_name);
            while (File.Exists($"{directory_name}/picture{n}.png")) n++;

            File.WriteAllBytes($"{directory_name}/picture{n}.png", ImageConversion.EncodeToPNG(picture));

            Invoke("printNotification", 1f);
        }

        public Texture2D getPicture()
        {
            updatePaper();
            var aux = RenderTexture.active;
            RenderTexture.active = rt;
            picture.ReadPixels(new Rect(0, 0, size, size), startX, startY);
            picture.Apply();
            RenderTexture.active = aux;

            return picture;
        }

        public void updatePaper()
        {
            if (!frame.Equals(paperType))
            {
                if (frame.Equals("[none]"))
                {
                    picture = new Texture2D(size, size);
                    startX = 0; startY = 0;
                }
                else
                {
                    startX = 33;
                    startY = frame.Equals("Square") ? 33 : 129;

                    picture = new Texture2D(paper.width, paper.width + startY - 33);
                    picture.SetPixels(paper.GetPixels(0, 0, picture.width, picture.height));
                }
                paperType = frame;
            }
        }

        public void printNotification()
        {
            NotificationManager.SharedInstance.PostNotification(new NotificationData(NotificationTarget.All, "THE PICTURE HAS BEEN PRINTED", 3f, true), false);
        }
    }
}