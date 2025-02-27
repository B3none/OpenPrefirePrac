using CounterStrikeSharp.API.Core;

namespace OpenPrefirePrac;

public class PlayerStatus
{
    /**
     * -1 if player is not practicing
     */
    public int PracticeIndex = -1;
    
    public int Progress = 0;
    
    /**
     * 0: No healing
     * 1: Init hp 500 with no healing
     * 2: +25hp for each kill
     * 3: +100hp for each kill
     * 4: +500hp for each kill
     */
    public int HealingMethod = 3;
    
    public readonly List<CCSPlayerController> Bots = new();
    public readonly Dictionary<string, int> LocalizedPracticeNames = new();
    public readonly Dictionary<string, int> LocalizedDifficultyNames = new();
    
    /**
     * 0: Random mode, 70% targets
     * 1: Full mode, all targets
     */
    public int TrainingMode = 0;
    
    public readonly Dictionary<string, int> LocalizedTrainingModeNames = new();
    public readonly List<int> EnabledTargets = new();
    public readonly List<int> Beams = new();
}
