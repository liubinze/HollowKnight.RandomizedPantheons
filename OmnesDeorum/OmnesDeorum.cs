using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Modding;
using JetBrains.Annotations;
using MonoMod.RuntimeDetour.HookGen;
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

        private static BossScene _last = null;

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
                    object seq = SEQUENCE_FIELD.GetValue(null);

                    BossScene[] bossScenes = (BossScene[]) BOSS_SCENES_FI.GetValue(seq);

                    BossScene lastScene = bossScenes.Last().sceneName == RAD ? bossScenes.Last() : null;

                    List<BossScene> scenes = null;

                    do
                    {
                        scenes = bossScenes
                            .Where(x => x.sceneName != RAD && !LORE_GARBAGE.Contains(x.sceneName) && x.sceneName != (_last?.sceneName ?? ""))
                            .Take(scenes?.Count ?? bossScenes.Length - 1)
                            .Skip(1)
                            .OrderBy(i => RNG.Next())
                            .ToList();
                    } while (scenes[0].sceneName == "GG_Spa" || scenes.Last().sceneName == "GG_Spa");

                    if(lastScene != null)
                        scenes.Add(lastScene);

                    _last = scenes[0];

                    BOSS_SCENES_FI.SetValue(seq, scenes.ToArray());
                });
            }
        }

        public void Unload() => IL.BossSequenceController.SetupNewSequence -= RandomizeSequence;

        private static readonly Random RNG = new Random();
    }
}