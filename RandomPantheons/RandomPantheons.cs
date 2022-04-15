using System.Collections.Generic;
using System.Linq;
using Modding;
using JetBrains.Annotations;
using Random = System.Random;
using USceneManager = UnityEngine.SceneManagement.SceneManager;
using Vasi;

namespace RandomPantheons
{
    [UsedImplicitly]
    public class RandomPantheons : Mod, ITogglableMod
    {
        private static readonly List<string> Blacklist = new List<string>
        {
            "GG_Unn",
            "GG_Wyrm",
            "GG_Engine",
            "GG_Engine_Prime",
            "GG_Engine_Root",
        };

        // Some scenes cause issues when first.
        private static readonly List<string> InvalidFirst = new List<string>()
        {
            // Causes an infinite loop
            "GG_Spa",
            // Doesn't wake up
            "GG_Gruz_Mother",
            "GG_Gruz_Mother_V"
        };

        // Some scenes cause issues when last
        private static readonly List<string> InvalidLast = new List<string>()
        {
            // Causes an infinite loop
            "GG_Spa",
        };

        private readonly Random _rand = new Random();

        public override string GetVersion() => VersionUtil.GetVersion<RandomPantheons>();

        public override void Initialize()
        {
            /*
             * We need to hook on the door, rather than the controller
             * Because the door has the challengeFSM responsible for the transition to the first scene
             * Which is otherwise ignored if we edit the sequence later.
             */
            On.BossSequenceDoor.Start += RandomizeSequence;
            On.PlayMakerFSM.Start += ModifyRadiance;
        }

        private void ModifyRadiance(On.PlayMakerFSM.orig_Start orig, PlayMakerFSM self)
        {
            orig(self);
            if (self.name == "Absolute Radiance"&&self.FsmName== "Control")
            {
                self.GetAction<SetStaticVariable>("Ending Scene", 1).setValue.boolValue = false;//Modify this action to leave p5 as other doors
            }
        }

        private void RandomizeSequence
        (
            On.BossSequenceDoor.orig_Start orig,
            BossSequenceDoor self
        )
        {
            BossSequence seq = self.bossSequence;

            ref BossScene[] bossScenes = ref Mirror.GetFieldRef<BossSequence, BossScene[]>(seq, "bossScenes");

            List<BossScene> scenes = bossScenes
                                     .Where(x => !Blacklist.Contains(x.sceneName))
                                     .OrderBy(i => _rand.Next())
                                     .ToList();
            
            const string bench = "GG_Spa";

            while (InvalidFirst.Contains(scenes[0].sceneName))
            {
                BossScene first = scenes[0];

                scenes.RemoveAt(0);

                scenes.Insert(_rand.Next(1, scenes.Count), first);
            }
            
            while (InvalidLast.Contains(scenes[scenes.Count - 1].sceneName))
            {
                BossScene last = scenes[scenes.Count-1];
                
                scenes.RemoveAt(scenes.Count - 1);

                scenes.Insert(_rand.Next(1, scenes.Count - 1), last);
            }

            // Multiple benches in a row causes an infinite loop.
            for (int i = 0; i < scenes.Count - 1; i++)
            {
                if
                (
                    scenes[i].sceneName != bench
                    || scenes[i].sceneName != scenes[i + 1].sceneName
                )
                {
                    continue;
                }

                scenes.RemoveAt(i);

                // Move the cursor one back, because otherwise we'll skip over an element.
                i--;
            }

            bossScenes = scenes.ToArray();

            orig(self);
        }

        public void Unload()
        {
            On.BossSequenceDoor.Start -= RandomizeSequence;
        }
    }
}