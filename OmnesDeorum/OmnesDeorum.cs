using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Modding;
using JetBrains.Annotations;
using Mono.Cecil.Cil;
using MonoMod.RuntimeDetour.HookGen;
using On.HutongGames.PlayMaker.Actions;
using UnityEngine;
using Logger = Modding.Logger;
using Random = System.Random;
using USceneManager = UnityEngine.SceneManagement.SceneManager;

namespace OmnesDeorum
{
    [UsedImplicitly]
    public class OmnesDeorum : Mod, ITogglableMod
    {
        private static readonly FieldInfo SEQUENCE_FIELD = typeof(BossSequenceController).GetField("currentSequence", BindingFlags.NonPublic | BindingFlags.Static);

        private static readonly FieldInfo BOSS_SCENES_FI = typeof(BossSequence).GetField("bossScenes", BindingFlags.NonPublic | BindingFlags.Instance);

        private static readonly List<string> LORE_GARBAGE = new List<string>
        {
            "GG_Unn",
            "GG_Wyrm",
            "GG_Engine",
            "GG_Engine_Prime",
            "GG_Engine_Root",
        };

        private static bool _first = true;

        private const string RAD = "GG_Radiance";

        public override void Initialize() => IL.BossSequenceController.SetupNewSequence += RandomizeSequence;

        private static void RandomizeSequence(HookIL il)
        {
            HookILCursor c = il.At(0);

            while (c.TryFindNext(out HookILCursor[] cursors,
                instr => instr.MatchCall(typeof(BossSequenceController), "SetupBossScene")
            ))
            {
                Logger.Log(cursors[0].Index);

                cursors[0].EmitDelegate(() =>
                {
                    object seq = SEQUENCE_FIELD.GetValue(null);

                    BossScene[] bossScenes = (BossScene[]) BOSS_SCENES_FI.GetValue(seq);

                    BossScene lastScene = bossScenes.Last();
                    BossScene firstScene = null;
                    
                    if (_first)
                    {
                        firstScene = bossScenes[0];
                    }

                    List<BossScene> scenes = bossScenes.Where(x => x.sceneName != RAD && !LORE_GARBAGE.Contains(x.sceneName)).Skip(1).OrderBy(i => RNG.Next()).ToList();

                    scenes.Add(lastScene);
                    
                    if (_first)
                    {
                        scenes.Insert(0, firstScene);
                        _first = false;
                    }

                    BOSS_SCENES_FI.SetValue(seq, scenes.ToArray());
                });
            }

            foreach (Instruction instr in il.Instrs)
            {
                Logger.Log(instr);
            }
        }

        public void Unload() => IL.BossSequenceController.SetupNewSequence -= RandomizeSequence;

        private static readonly Random RNG = new Random();
    }
}