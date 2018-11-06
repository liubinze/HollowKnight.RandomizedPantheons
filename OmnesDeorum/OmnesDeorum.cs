using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Modding;
using JetBrains.Annotations;
using MonoMod.RuntimeDetour.HookGen;
using On.HutongGames.PlayMaker;
using Random = System.Random;
using USceneManager = UnityEngine.SceneManagement.SceneManager;

// ReSharper disable Unity.NoNullPropogation

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
            "GG_Engine_Root"
        };

        private static BossScene First
        {
            get => string.IsNullOrEmpty(_name) || !_firstLast.ContainsKey(_name) ? null : _firstLast[_name].First;
            set
            {
                if (_firstLast.ContainsKey(_name))
                    _firstLast[_name].First = value;
                else
                    _firstLast[_name] = new FirstLast(value, null);
            }
        }

        private static BossScene Last
        {
            get => string.IsNullOrEmpty(_name) || !_firstLast.ContainsKey(_name) ? null : _firstLast[_name].Last;
            set
            {
                if (_firstLast.ContainsKey(_name))
                    _firstLast[_name].Last = value;
                else
                    _firstLast[_name] = new FirstLast(null, value);
            }
        }

        // ReSharper disable once InconsistentNaming
        private static readonly Dictionary<string, FirstLast> _firstLast = new Dictionary<string, FirstLast>();

        private static string _name;

        private const string RAD = "GG_Radiance";

        public override void Initialize() => IL.BossSequenceController.SetupNewSequence += RandomizeSequence;

        private static void RandomizeSequence(HookIL il)
        {
            HookILCursor c = il.At(0);

            while (c.TryFindNext(out HookILCursor[] cursors,
                instr => instr.MatchCall(typeof(BossSequenceController), "SetupBossScene")
            ))
            {
                cursors[0].EmitDelegate(() =>
                {
                    var seq = (BossSequence) SEQUENCE_FIELD.GetValue(null);

                    BossScene[] bossScenes = (BossScene[]) BOSS_SCENES_FI.GetValue(seq);

                    BossScene lastScene = bossScenes.Last().sceneName == RAD ? bossScenes.Last() : null;

                    // Swapping pantheons
                    _name = seq.name;

                    List<BossScene> scenes;

                    do
                    {
                        scenes = bossScenes
                            .Where(x => !LORE_GARBAGE.Contains(x.sceneName) && x.sceneName != (First?.sceneName ?? ""))
                            .Take(bossScenes.Length - (lastScene ? 1 : 0))
                            .OrderBy(i => RNG.Next())
                            .ToList();
                    } 
                    while (scenes[0].sceneName == "GG_Spa" || scenes.Last().sceneName == "GG_Spa");

                    if (lastScene != null)
                    {
                        scenes.Add(lastScene);
                    }

                    // First run of a pantheon
                    if (First == null)
                    {
                        scenes = scenes.Where((x, i) => bossScenes[0].sceneName != x.sceneName).ToList();

                        Last = bossScenes[0];
                    }
                    else
                    {
                        scenes.Insert(RNG.Next(1, scenes.Count + 1), Last);

                        Last = First;
                    }

                    First = scenes[0];

                    scenes.Insert(RNG.Next(1, scenes.Count + 1), First);

                    BOSS_SCENES_FI.SetValue(seq, scenes.ToArray());
                });
            }
        }

        public void Unload() => IL.BossSequenceController.SetupNewSequence -= RandomizeSequence;

        private static readonly Random RNG = new Random();
    }

    internal class FirstLast
    {
        public BossScene First;
        public BossScene Last;

        public FirstLast(BossScene first, BossScene last)
        {
            First = first;
            Last = last;
        }
    }
}