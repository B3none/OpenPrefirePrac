﻿using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Menu;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Modules.Memory;
using Vector = CounterStrikeSharp.API.Modules.Utils.Vector;
using System.Globalization;

namespace OpenPrefirePrac;

public class OpenPrefirePrac : BasePlugin
{
    public override string ModuleName => "Open Prefire Prac";
    public override string ModuleVersion => "0.0.13";

    // private Dictionary<int, List<int>> bots_of_players = new Dictionary<int, List<int>>();

    // private Dictionary<int, int> progress_of_players = new Dictionary<int, int>();
    
    // private Dictionary<int, int> healing_method_of_players = new Dictionary<int, int>(); // 0: No healing; 1: Init hp 500 with no healing; 2: +25hp for each kill; 3: +100hp for each kill; 4: +500hp for each kill

    private Dictionary<int, PlayerStatus> player_manager = new Dictionary<int, PlayerStatus>();
    private Dictionary<int, int> masters_of_bots = new Dictionary<int, int>();

    // private Dictionary<int, int> practice_of_players = new Dictionary<int, int>();

    private Dictionary<string, int> practice_name_to_id = new Dictionary<string, int>();

    private Dictionary<int, bool> practice_enabled = new Dictionary<int, bool>();

    // private Dictionary<int, Dictionary<string, int>> localized_practice_names = new Dictionary<int, Dictionary<string, int>>();

    // private Dictionary<int, Dictionary<string, int>> localized_difficulty_names = new Dictionary<int, Dictionary<string, int>>();

    private string map_name = "";

    private int player_count = 0;

    // private Dictionary<int, PrefirePractice> practices = new Dictionary<int, PrefirePractice>();         // This is actually a list
    private List<PrefirePractice> practices = new List<PrefirePractice>();

    private List<string> availble_maps = new List<string>();

    private Translator translator;

    public override void Load(bool hotReload)
    {
        base.Load(hotReload);

	    Console.WriteLine(" {ChatColors.Green}[OpenPrefirePrac] {ChatColors.White}Registering listeners.");
        RegisterListener<Listeners.OnClientPutInServer>(OnClientPutInServerHandler);
        RegisterListener<Listeners.OnMapStart>(OnMapStartHandler);
        RegisterListener<Listeners.OnClientDisconnectPost>(OnClientDisconnectHandler);

        translator = new Translator(Localizer, ModuleDirectory, CultureInfo.CurrentCulture.Name);

        if (hotReload)
        {
            // Clear status registers
            masters_of_bots.Clear();
            practice_name_to_id.Clear();
            practice_enabled.Clear();
            practices.Clear();
            availble_maps.Clear();
            map_name = "";
            player_count = 0;
            player_manager.Clear();
            // bots_of_players.Clear();
            // progress_of_players.Clear();
            // healing_method_of_players.Clear();
            // practice_of_players.Clear();
            // localized_practice_names.Clear();
            // localized_difficulty_names.Clear();

            // Setup map
            OnMapStartHandler(Server.MapName);
            
            // Setup players
            IEnumerable<CCSPlayerController> playerEntities = Utilities.FindAllEntitiesByDesignerName<CCSPlayerController>("cs_player_controller");
            foreach (CCSPlayerController tempPlayer in playerEntities)
            {
                if (!tempPlayer.IsValid || tempPlayer.IsBot || tempPlayer.IsHLTV)
                    continue;
                OnClientPutInServerHandler(tempPlayer.Slot);    
            }
        }
    }

    public void OnClientPutInServerHandler(int slot)
    {
        var player = new CCSPlayerController(NativeAPI.GetEntityFromIndex(slot + 1));

        if (!player.IsValid || player.IsBot || player.IsHLTV) return;

        // bots_of_players.Add(slot, new List<int>());
        // progress_of_players.Add(slot, 0);
        // practice_of_players.Add(slot, -1);
        // localized_practice_names.Add(slot, new Dictionary<string, int>());
        // localized_difficulty_names.Add(slot, new Dictionary<string, int>());
        // healing_method_of_players.Add(slot, 3);
        player_manager.Add(slot, new PlayerStatus());

        // Record player language
        translator.RecordPlayerCulture(player);
        // Console.WriteLine(" {ChatColors.Green}[OpenPrefirePrac] {ChatColors.White}Player " + player.PlayerName + "(" + translator.language_manager[player.SteamID] + ") just connected.");
    }

    public void OnClientDisconnectHandler(int slot)
    {
        var player = new CCSPlayerController(NativeAPI.GetEntityFromIndex(slot + 1));

        if (!player.IsValid || player.IsBot || player.IsHLTV) return;

        // if (bots_of_players[slot].Count != 0)
        // {
        //     ExitPrefireMode(player);
        // }
        if (player_manager[slot].practice_no != -1)
            ExitPrefireMode(slot);

        // Release resources(practices, targets, bots...)
        player_manager.Remove(slot);
        // bots_of_players.Remove(slot);
        // progress_of_players.Remove(slot);
        // practice_of_players.Remove(slot);
        // localized_practice_names.Remove(slot);
        // localized_difficulty_names.Remove(slot);
        // healing_method_of_players.Remove(slot);
    }

    public void OnMapStartHandler(string map)
    {
        map_name = map;
        // Console.WriteLine(" {ChatColors.Green}[OpenPrefirePrac] {ChatColors.White}Map loaded: " + map_name);

        // load practices available in current map, from corresponding map directory.
        availble_maps.Clear();
        List<string> map_dirs = new List<string>(Directory.EnumerateDirectories(ModuleDirectory + "/maps"));
        bool found = false;
        for (int i = 0; i < map_dirs.Count; i++)
        {
            string map_path = map_dirs[i].Substring(map_dirs[i].LastIndexOf(Path.DirectorySeparatorChar) + 1);
            Console.WriteLine($" {ChatColors.Green}[OpenPrefirePrac] {ChatColors.White}Map folder for map {map_path} founded.");
            availble_maps.Add(map_path);

            if (map_path.Equals(map_name))
            {
                found = true;
                Console.WriteLine(" {ChatColors.Green}[OpenPrefirePrac] {ChatColors.White}Map folder for current map founded.");
            }
        }

        if (found)
        {
            LoadPractice();
        }
        else
        {
            Console.WriteLine(" {ChatColors.Green}[OpenPrefirePrac] {ChatColors.White}Failed to load practices on map " + map_name);
        }

        // Move menu creation to command !prefire to support localization.
        // Create menu.
        // main_menu.MenuOptions.Clear();
        // main_menu.AddMenuOption(Localizer["mainmenu.practice"], OpenPracticeMenu);
        // main_menu.AddMenuOption(Localizer["mainmenu.map"], OpenMapMenu);
        // main_menu.AddMenuOption(Localizer["mainmenu.exit"], ForceExitPrefireMode);

    }

    [GameEventHandler]
    public HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
    {
        // For bots, set them up.
        if (@event.Userid.IsValid && @event.Userid.IsBot && !@event.Userid.IsHLTV) 
        {
            // if there are more targets to place, move bot to next place
            if (masters_of_bots.ContainsKey(@event.Userid.Slot))
            {
                int master_slot = masters_of_bots[@event.Userid.Slot];
                // int target_no = progress_of_players[master_slot];
                // int practice_no = practice_of_players[master_slot];
                int target_no = player_manager[master_slot].progress;
                int practice_no = player_manager[master_slot].practice_no;
                // if (target_no < practices[practice_no].targets.Count)
                if (target_no < player_manager[master_slot].enabled_targets.Count)
                {
                    // progress_of_players[master_slot]++;
                    player_manager[master_slot].progress++;

                    MovePlayer(@event.Userid, practices[practice_no].targets[player_manager[master_slot].enabled_targets[target_no]].is_crouching, practices[practice_no].targets[player_manager[master_slot].enabled_targets[target_no]].position, practices[practice_no].targets[player_manager[master_slot].enabled_targets[target_no]].rotation);
                    // Console.WriteLine($" {ChatColors.Green}[OpenPrefirePrac] {ChatColors.White}{@event.Userid.PlayerName}, {practices[practice_no].targets[target_no].is_crouching}, {practices[practice_no].targets[target_no].position.ToString()}");
                    Server.ExecuteCommand($"css_freeze_helper {@event.Userid.Slot}");
                }
                else
                {
                    // This is to patch the issue of extra bots
                    masters_of_bots.Remove(@event.Userid.Slot);
                    // bots_of_players[master_slot].Remove(@event.Userid.Slot);
                    player_manager[master_slot].bots.Remove(@event.Userid.Slot);
                    Server.ExecuteCommand($"bot_kick {@event.Userid.PlayerName}");

                    // if (bots_of_players[master_slot].Count == 0)
                    if (player_manager[master_slot].bots.Count == 0)
                    {
                        // Practice finished.
                        var player = new CCSPlayerController(NativeAPI.GetEntityFromIndex(master_slot + 1));
                        player.PrintToChat($" {ChatColors.Green}[OpenPrefirePrac] {ChatColors.White}{translator.Translate(player, "practice.finish")}");
                        ExitPrefireMode(player.Slot);
                    }
                }
            }
        }

        // For players, restart practice
        if (@event.Userid.IsValid && !@event.Userid.IsBot && !@event.Userid.IsHLTV)
        {
            // Unmanaged player. This should not happen since hot_reload is now supported.
            if (!player_manager.ContainsKey(@event.Userid.Slot))
                return HookResult.Continue;

            // int current_practice_no = practice_of_players[@event.Userid.Slot];

            if (player_manager[@event.Userid.Slot].practice_no < 0)
                return HookResult.Continue;

            SetupPrefireMode(@event.Userid);
        }

        return HookResult.Continue;
    }

    [GameEventHandler]
    public HookResult OnPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
    {
        if (@event.Userid.IsValid && @event.Userid.IsBot && !@event.Userid.IsHLTV) 
        {
            if (masters_of_bots.ContainsKey(@event.Userid.Slot))
            {
                int master_slot = masters_of_bots[@event.Userid.Slot];
                // int target_no = progress_of_players[master_slot];
                // int practice_no = practice_of_players[master_slot];
                int target_no = player_manager[master_slot].progress;
                int practice_no = player_manager[master_slot].practice_no;

                if (target_no >= practices[practice_no].num_bots)         // Bots will be killed once automatically when they are added so as to move them to target spots.
                {
                    CCSPlayerController master = new CCSPlayerController(NativeAPI.GetEntityFromIndex(master_slot + 1));
                    
                    // Award the player.
                    if (master.PawnIsAlive && master.Pawn.Value != null  && player_manager[master_slot].healing_method > 1)
                    {
                        master.GiveNamedItem("item_assaultsuit");
                        
                        int current_hp = master.Pawn.Value.Health;
                        // if (healing_method_of_players[master_slot] == 2)
                        //     current_hp = current_hp + 25;
                        // else
                        //     current_hp = current_hp + 100;
                        switch (player_manager[master_slot].healing_method)
                        {
                            case 2:
                                current_hp = current_hp + 25;
                                break;
                            case 4:
                                current_hp = current_hp + 500;
                                break;
                            default:
                                current_hp = current_hp + 100;
                                break;
                        }
                        SetPlayerHealth(master, current_hp);
                    }

                    // Print progress
                    master.PrintToCenter(translator.Translate(master, "practice.progress", player_manager[master.Slot].enabled_targets.Count, player_manager[master.Slot].enabled_targets.Count - target_no + player_manager[master_slot].bots.Count - 1));
                }

                // Kick unnecessary bots
                // if (target_no >= practices[practice_no].targets.Count)
                if (target_no >= player_manager[master_slot].enabled_targets.Count)
                {
                    masters_of_bots.Remove(@event.Userid.Slot);
                    // bots_of_players[master_slot].Remove(@event.Userid.Slot);
                    player_manager[master_slot].bots.Remove(@event.Userid.Slot);
                    Server.ExecuteCommand($"bot_kick {@event.Userid.PlayerName}");

                    // if (bots_of_players[master_slot].Count == 0)
                    if (player_manager[master_slot].bots.Count == 0)
                    {
                        // Practice finished.
                        var player = new CCSPlayerController(NativeAPI.GetEntityFromIndex(master_slot + 1));
                        player.PrintToChat($" {ChatColors.Green}[OpenPrefirePrac] {ChatColors.White}{translator.Translate(player, "practice.finish")}");
                        ExitPrefireMode(player.Slot);
                    }
                }
            }
        }

        // Check if player has enough bots for selected practice
        if (@event.Userid.IsValid && !@event.Userid.IsBot && !@event.Userid.IsHLTV)
        {
            // int practice_no = practice_of_players[@event.Userid.Slot];
            int practice_no = player_manager[@event.Userid.Slot].practice_no;
            int num_bots = player_manager[@event.Userid.Slot].bots.Count;
            
            // if (practice_no > 0 && bots_of_players[@event.Userid.Slot].Count < practices[practice_no].num_bots)
            //     AddBot(@event.Userid, practices[practice_no].num_bots - bots_of_players[@event.Userid.Slot].Count);
            if (practice_no > 0 && num_bots < practices[practice_no].num_bots)
                AddBot(@event.Userid, practices[practice_no].num_bots - num_bots);
        }
        
        return HookResult.Continue;
    }

    [ConsoleCommand("css_prefire", "Print available prefire routes and receive user's choice")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void OnPrefireCommand(CCSPlayerController player, CommandInfo commandInfo)
    {       
        // var language = player.GetLanguage();
        // Console.WriteLine($" {ChatColors.Green}[OpenPrefirePrac] {ChatColors.White}Player {player.PlayerName}'s language is {language.Name}.");
        ChatMenu main_menu = new ChatMenu(translator.Translate(player, "mainmenu.title"));
        // main_menu.MenuOptions.Clear();
        main_menu.AddMenuOption(translator.Translate(player, "mainmenu.practice"), OpenPracticeMenu);
        main_menu.AddMenuOption(translator.Translate(player, "mainmenu.map"), OpenMapMenu);
        string current_difficulty = translator.Translate(player, $"difficulty.{player_manager[player.Slot].healing_method}");
        main_menu.AddMenuOption(translator.Translate(player, "mainmenu.difficulty", current_difficulty), OpenDifficultyMenu);
        string current_training_mode = translator.Translate(player, $"modemenu.{player_manager[player.Slot].training_mode}");
        main_menu.AddMenuOption(translator.Translate(player, "mainmenu.mode", current_training_mode), OpenModeMenu);
        main_menu.AddMenuOption(translator.Translate(player, "mainmenu.exit"), ForceExitPrefireMode);
        
        player.PrintToChat("============ [OpenPrefirePrac] ============");
        MenuManager.OpenChatMenu(player, main_menu);
        player.PrintToChat("===========================================");
    }

    public void OnRouteSelect(CCSPlayerController player, ChatMenuOption option)
    {
        if (player_count == 0)
        {
            Server.ExecuteCommand("tv_enable 0");
            Server.ExecuteCommand("sv_cheats 1");
            Server.ExecuteCommand("mp_maxmoney 60000");
            Server.ExecuteCommand("mp_startmoney 60000");
            Server.ExecuteCommand("mp_buytime 9999");
            Server.ExecuteCommand("mp_buy_anywhere 1");
            Server.ExecuteCommand("bot_allow_grenades 0");
            Server.ExecuteCommand("bot_allow_snipers 0");
            Server.ExecuteCommand("bot_allow_shotguns 0");
            Server.ExecuteCommand("bot_difficulty 5");
            Server.ExecuteCommand("custom_bot_difficulty 5");
            Server.ExecuteCommand("sv_infinite_ammo 1");
            Server.ExecuteCommand("mp_limitteams 0");
            Server.ExecuteCommand("mp_autoteambalance 0");
            Server.ExecuteCommand("mp_warmup_pausetimer 1");
            Server.ExecuteCommand("bot_quota_mode normal");
            Server.ExecuteCommand("weapon_auto_cleanup_time 1");
            Server.ExecuteCommand("mp_free_armor 2");
            Server.ExecuteCommand("mp_respawn_immunitytime -1");
            // Server.ExecuteCommand("mp_roundtime 60");
            // Server.ExecuteCommand("mp_roundtime_defuse 60");
            // Server.ExecuteCommand("mp_freezetime 0");
            // Server.ExecuteCommand("mp_team_intro_time 0");
            // Server.ExecuteCommand("mp_ignore_round_win_conditions 1");
            // Server.ExecuteCommand("mp_respawn_on_death_ct 1");
            // Server.ExecuteCommand("mp_respawn_on_death_t 1");
            Server.ExecuteCommand("mp_warmup_start");
        }

        // string choosen_practice = option.Text;
        // player.PrintToChat(choosen_practice);
        // int practice_no = practice_name_to_id[choosen_practice];
        int practice_no = player_manager[player.Slot].localized_practice_names[option.Text];

        // Check if selected practice route is compatible with other on-playing routes.
        if (!practice_enabled[practice_no])
        {
            player.PrintToChat($" {ChatColors.Green}[OpenPrefirePrac] {ChatColors.White}{translator.Translate(player, "practice.incompatible")}");
            return;
        }

        // int previous_practice_no = practice_of_players[player.Slot];
        int previous_practice_no = player_manager[player.Slot].practice_no;
        if (previous_practice_no > -1)
        {
            // Enable disabled practice routes
            for (int i = 0; i < practices[previous_practice_no].incompatible_practices.Count; i++)
            {
                if (practice_name_to_id.ContainsKey(practices[previous_practice_no].incompatible_practices[i]))
                {
                    int disabled_practice_no = practice_name_to_id[practices[previous_practice_no].incompatible_practices[i]];
                    practice_enabled[disabled_practice_no] = true;
                }
            }
        
            RemoveBots(player.Slot);
        }
        else
        {
            player_count++;
        }

        // practice_of_players[player.Slot] = practice_no;
        player_manager[player.Slot].practice_no = practice_no;

        // Disable incompatible practices.
        for (int i = 0; i < practices[practice_no].incompatible_practices.Count; i++)
        {
            if (practice_name_to_id.ContainsKey(practices[practice_no].incompatible_practices[i]))
            {
                int disabled_practice_no = practice_name_to_id[practices[practice_no].incompatible_practices[i]];
                practice_enabled[disabled_practice_no] = false;
            }
        }

        AddBot(player, practices[practice_no].num_bots);
        SetupPrefireMode(player);
        
        string localized_practice_name = translator.Translate(player, "map." + map_name + "." + practices[practice_no].practice_name);
        player.PrintToChat($" {ChatColors.Green}[OpenPrefirePrac] {ChatColors.White} {translator.Translate(player, "practice.choose", localized_practice_name)}");
        player.PrintToCenter(translator.Translate(player, "practice.begin"));
    }

    public void ForceExitPrefireMode(CCSPlayerController player, ChatMenuOption option)
    {
        ExitPrefireMode(player.Slot);
        
        player.PrintToChat($" {ChatColors.Green}[OpenPrefirePrac] {ChatColors.White}{translator.Translate(player, "practice.exit")}");
    }

    public void OpenMapMenu(CCSPlayerController player, ChatMenuOption option)
    {
        ChatMenu map_menu = new ChatMenu(translator.Translate(player, "mapmenu.title"));
        for (int i = 0; i < availble_maps.Count; i++)
            map_menu.AddMenuOption(availble_maps[i], ChangeMap);

        player.PrintToChat("============ [OpenPrefirePrac] ============");
        MenuManager.OpenChatMenu(player, map_menu);
        player.PrintToChat("===========================================");
    }

    public void ChangeMap(CCSPlayerController player, ChatMenuOption option)
    {
        // Only allow change map when noone is practicing.
        if (player_count == 0)
        {
            // map_name = option.Text;
            // LoadPractice();
            Server.ExecuteCommand($"changelevel {option.Text}");
        }
        else
        {
            player.PrintToChat($" {ChatColors.Green}[OpenPrefirePrac] {ChatColors.White}{translator.Translate(player, "mapmenu.busy")}");
        }
    }

    public void OpenPracticeMenu(CCSPlayerController player, ChatMenuOption option)
    {
        // Dynamically draw menu
        ChatMenu practice_menu = new ChatMenu(translator.Translate(player, "practicemenu.title"));
        // localized_practice_names[player.Slot].Clear();
        player_manager[player.Slot].localized_practice_names.Clear();

        for (int i = 0; i < practices.Count; i++)
        {
            if (practice_enabled[i])
            {
                string tmp_localized_practice_name = translator.Translate(player, "map." + map_name + "." + practices[i].practice_name);
                // localized_practice_names[player.Slot].Add(tmp_localized_practice_name, i);
                player_manager[player.Slot].localized_practice_names.Add(tmp_localized_practice_name, i);
                practice_menu.AddMenuOption(tmp_localized_practice_name, OnRouteSelect);     // practice name here is splited by space instead of underline. TODO: Use localized text.
            }
        }

        player.PrintToChat("============ [OpenPrefirePrac] ============");
        MenuManager.OpenChatMenu(player, practice_menu);
        player.PrintToChat("===========================================");
    }

    public void OpenDifficultyMenu(CCSPlayerController player, ChatMenuOption option)
    {
        // Dynamically draw menu
        ChatMenu difficulty_menu = new ChatMenu(translator.Translate(player, "difficulty.title"));
        // localized_difficulty_names[player.Slot].Clear();
        player_manager[player.Slot].localized_difficulty_names.Clear();

        for (int i = 0; i < 5; i++)
        {
            string tmp_localized_difficulty_name = translator.Translate(player, $"difficulty.{i}");
            // localized_difficulty_names[player.Slot].Add(tmp_localized_difficulty_name, i);
            player_manager[player.Slot].localized_difficulty_names.Add(tmp_localized_difficulty_name, i);
            difficulty_menu.AddMenuOption(tmp_localized_difficulty_name, OnDifficultyChosen);     // practice name here is splited by space instead of underline. TODO: Use localized text.
        }

        player.PrintToChat("============ [OpenPrefirePrac] ============");
        MenuManager.OpenChatMenu(player, difficulty_menu);
        player.PrintToChat("===========================================");
    }

    public void OnDifficultyChosen(CCSPlayerController player, ChatMenuOption option)
    {
        // int difficulty_no = localized_difficulty_names[player.Slot][option.Text];
        int difficulty_no = player_manager[player.Slot].localized_difficulty_names[option.Text];
        // healing_method_of_players[player.Slot] = difficulty_no;
        player_manager[player.Slot].healing_method = difficulty_no;
        string current_difficulty = translator.Translate(player, $"difficulty.{difficulty_no}");
        player.PrintToChat($" {ChatColors.Green}[OpenPrefirePrac] {ChatColors.White} {translator.Translate(player, "difficulty.set", current_difficulty)}");
    }

    public void OpenModeMenu(CCSPlayerController player, ChatMenuOption option)
    {
        ChatMenu training_mode_menu = new ChatMenu(translator.Translate(player, "modemenu.title"));
        player_manager[player.Slot].localized_training_mode_names.Clear();

        for (int i = 0; i < 2; i++)
        {
            string tmp_localized_training_mode_name = translator.Translate(player, $"modemenu.{i}");
            player_manager[player.Slot].localized_training_mode_names.Add(tmp_localized_training_mode_name, i);
            training_mode_menu.AddMenuOption(tmp_localized_training_mode_name, OnModeChosen);
        }

        player.PrintToChat("============ [OpenPrefirePrac] ============");
        MenuManager.OpenChatMenu(player, training_mode_menu);
        player.PrintToChat("===========================================");
    }

    public void OnModeChosen(CCSPlayerController player, ChatMenuOption option)
    {
        int training_mode_no = player_manager[player.Slot].localized_training_mode_names[option.Text];
        player_manager[player.Slot].training_mode = training_mode_no;
        string current_training_mode = translator.Translate(player, $"modemenu.{training_mode_no}");
        player.PrintToChat($" {ChatColors.Green}[OpenPrefirePrac] {ChatColors.White} {translator.Translate(player, "modemenu.set", current_training_mode)}");
    }

    private void LoadPractice()
    {
        Console.WriteLine($" {ChatColors.Green}[OpenPrefirePrac] {ChatColors.White}Loading practices for map {map_name}.");
        List<string> practice_files = new List<string>(Directory.EnumerateFiles(ModuleDirectory + "/maps/" + map_name));
        practices.Clear();
        practice_name_to_id.Clear();
        practice_enabled.Clear();
        for (int i = 0; i < practice_files.Count; i++)
        {
            string practice_name = practice_files[i].Substring(practice_files[i].LastIndexOf(Path.DirectorySeparatorChar) + 1).Split(".")[0];
            practices.Add(new PrefirePractice(map_name, practice_name));
            practice_name_to_id.Add(practice_name, i);
            practice_enabled.Add(i, true);
            Console.WriteLine($" {ChatColors.Green}[OpenPrefirePrac] {ChatColors.White}{map_name} {practice_name} Loaded.");
        }
    }
    
    private void ExitPrefireMode(int player_slot)
    {
        // Enable disabled practice routes
        // int previous_practice_no = practice_of_players[player.Slot];
        int previous_practice_no = player_manager[player_slot].practice_no;
        if (previous_practice_no > -1)
        {
            RemoveBots(player_slot);

            for (int i = 0; i < practices[previous_practice_no].incompatible_practices.Count; i++)
            {
                if (practice_name_to_id.ContainsKey(practices[previous_practice_no].incompatible_practices[i]))
                {
                    int disabled_practice_no = practice_name_to_id[practices[previous_practice_no].incompatible_practices[i]];
                    practice_enabled[disabled_practice_no] = true;
                }
            }
        }
        
        // practice_of_players[player.Slot] = -1;
        player_manager[player_slot].practice_no = -1;
        
        player_count--;

        if (player_count == 0)
        {
            Server.ExecuteCommand("sv_cheats 0");
            Server.ExecuteCommand("mp_warmup_pausetimer 0");
            Server.ExecuteCommand("bot_quota_mode competitive");
            Server.ExecuteCommand("tv_enable 1");
            Server.ExecuteCommand("weapon_auto_cleanup_time 0");
            Server.ExecuteCommand("mp_buytime 20");
            Server.ExecuteCommand("mp_maxmoney 16000");
            Server.ExecuteCommand("mp_startmoney 16000");
            Server.ExecuteCommand("mp_buy_anywhere 0");
            Server.ExecuteCommand("mp_free_armor 0");
            // Server.ExecuteCommand("mp_roundtime 1.92");
            // Server.ExecuteCommand("mp_roundtime_defuse 1.92");
            // Server.ExecuteCommand("mp_team_intro_time 6.5");
            // Server.ExecuteCommand("mp_freezetime 15");
            // Server.ExecuteCommand("mp_ignore_round_win_conditions 0");
            // Server.ExecuteCommand("mp_respawn_on_death_ct 0");
            // Server.ExecuteCommand("mp_respawn_on_death_t 0");
            Server.ExecuteCommand("mp_warmup_start");
        }
    }

    private void ResetBots(int player_slot)
    {
        // progress_of_players[player.Slot] = 0;
        player_manager[player_slot].progress = 0;

        // for (int i = 0; i < bots_of_players[player.Slot].Count; i++)
        for (int i = 0; i < player_manager[player_slot].bots.Count; i++)
        {
            // int bot_slot = bots_of_players[player.Slot][i];
            int bot_slot = player_manager[player_slot].bots[i];
            var bot = new CCSPlayerController(NativeAPI.GetEntityFromIndex(bot_slot + 1));
            if (bot.IsValid || bot.PawnIsAlive)
            {
                Server.ExecuteCommand($"bot_kill {bot.PlayerName}");
            }
            else
            {
                Console.WriteLine($" {ChatColors.Green}[OpenPrefirePrac] {ChatColors.White}Error: Player has an invalid bot.(slot: {bot_slot})");
            }
        }
    }

    private void SetupPrefireMode(CCSPlayerController player)
    {
        // int practice_no = practice_of_players[player.Slot];
        int practice_no = player_manager[player.Slot].practice_no;
        GenerateRandomPractice(player.Slot);
        
        AddTimer(0.5f, () => ResetBots(player.Slot));
        
        // Setup player's HP
        // if (healing_method_of_players[player.Slot] == 1 || healing_method_of_players[player.Slot] == 4)
        if (player_manager[player.Slot].healing_method == 1 || player_manager[player.Slot].healing_method == 4)
            AddTimer(0.5f, () => SetPlayerHealth(player, 500));

        AddTimer(1f, () => EquipPlayer(player));

        AddTimer(1.5f, () => MovePlayer(player, false, practices[practice_no].player.position, practices[practice_no].player.rotation));
    }

    private void RemoveBots(int player_slot)
    {
        // for (int i = 0; i < bots_of_players[player.Slot].Count; i++)
        for (int i = 0; i < player_manager[player_slot].bots.Count; i++)
        {
            int bot_slot = player_manager[player_slot].bots[i];
            var bot = new CCSPlayerController(NativeAPI.GetEntityFromIndex(bot_slot + 1));
            if (bot.IsValid)
            {
                Server.ExecuteCommand($"bot_kick {bot.PlayerName}");
            }
            else
            {
                Console.WriteLine($" {ChatColors.Green}[OpenPrefirePrac] {ChatColors.White}Trying to kick an invalid bot.");
            }
            masters_of_bots.Remove(bot_slot);
        }
        player_manager[player_slot].bots.Clear();
        player_manager[player_slot].progress = 0;
    }

    private void AddBot(CCSPlayerController player, int number_of_bots)
    {
        // Not working
        // CCSPlayerController? bot = Utilities.CreateEntityByName<CCSPlayerController>($"bot {bots_of_players[player.Slot].Count}");
        
        // if (bot != null)
        // {
        //     Console.WriteLine($" {ChatColors.Green}[OpenPrefirePrac] {ChatColors.White}Bot created: {bot.Slot}.");
        //     bot.Teleport(pos, ang, vel);
        //     bot.DispatchSpawn();
        //     bots_of_players[player.Slot].Add(bot.Slot);
        // }

        Console.WriteLine($" {ChatColors.Green}[OpenPrefirePrac] {ChatColors.White}Creating {number_of_bots} bots.");
        for (int i = 0; i < number_of_bots; i++)
        {
            if (player.TeamNum == (byte)CsTeam.CounterTerrorist)
            {
                Server.ExecuteCommand("bot_join_team T");
                Server.ExecuteCommand("bot_add_t");
            }
            else if (player.TeamNum == (byte)CsTeam.Terrorist)
            {
                Server.ExecuteCommand("bot_join_team CT");
                Server.ExecuteCommand("bot_add_ct");
            }
        }

        AddTimer(0.4f, () =>
        {
            int number_bot_to_find = number_of_bots;
            var playerEntities = Utilities.FindAllEntitiesByDesignerName<CCSPlayerController>("cs_player_controller");

            foreach (var tempPlayer in playerEntities)
            {
                if (!tempPlayer.IsValid || !tempPlayer.IsBot || tempPlayer.IsHLTV) continue;
                if (tempPlayer.UserId.HasValue)
                {
                    // Chech if it belongs to someone, if so, do nothing
                    if (masters_of_bots.ContainsKey(tempPlayer.Slot))
                        continue;

                    // If it's a newly added bot
                    if (number_bot_to_find == 0)
                    {
                        // a redundent bot, kick it
                        Server.ExecuteCommand($"bot_kick {tempPlayer.PlayerName}");
                        Console.WriteLine($" {ChatColors.Green}[OpenPrefirePrac] {ChatColors.White}Exec command: bot_kick {tempPlayer.PlayerName}");
                        continue;
                    }

                    // bots_of_players[player.Slot].Add(tempPlayer.Slot);
                    player_manager[player.Slot].bots.Add(tempPlayer.Slot);
                    masters_of_bots.Add(tempPlayer.Slot, player.Slot);

                    number_bot_to_find--;
                    
                    Console.WriteLine($" {ChatColors.Green}[OpenPrefirePrac] {ChatColors.White}Bot {tempPlayer.PlayerName}, slot: {tempPlayer.Slot} has been spawned.");
                }
            }
        });
    }

    private void MovePlayer(CCSPlayerController player, bool crouch, Vector pos, QAngle ang)
    {
        // Only bot can crouch
        if (crouch)
        {
            CCSPlayer_MovementServices movement_service = new CCSPlayer_MovementServices(player.PlayerPawn.Value!.MovementServices!.Handle);
            AddTimer(0.1f, () => movement_service.DuckAmount = 1);
            AddTimer(0.2f, () => player.PlayerPawn.Value.Bot!.IsCrouching = true);
        }
        
        player.PlayerPawn.Value!.Teleport(pos, ang, new Vector(0, 0, 0));
    }

    // FreezeBot doesn't work in Event environment, so make it a command.
    [ConsoleCommand("css_freeze_helper", "Freeze a player")]
    [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
    public void OnFreezeHelperCommand(CCSPlayerController? player, CommandInfo commandInfo)
    {
        int bot_slot = int.Parse(commandInfo.ArgString);
        CCSPlayerController bot = new CCSPlayerController(NativeAPI.GetEntityFromIndex(bot_slot + 1));

        if (bot != null && bot.IsValid && bot.IsBot && !bot.IsHLTV && bot.PawnIsAlive && bot.Pawn.Value != null) // && bot.Pawn.Value.LifeState == (byte)LifeState_t.LIFE_ALIVE)
        {
            bot.Pawn.Value.MoveType = MoveType_t.MOVETYPE_OBSOLETE;
            Schema.SetSchemaValue(bot.Pawn.Value.Handle, "CBaseEntity", "m_nActualMoveType", 1);
            Utilities.SetStateChanged(bot.Pawn.Value, "CBaseEntity", "m_MoveType");
        }
    }

    private void EquipPlayer(CCSPlayerController player)
    {
        if (!player.PawnIsAlive || player.Pawn.Value == null)
            return;
        
        player.RemoveWeapons();

        // Give weapons and items
        player.GiveNamedItem("weapon_ak47");
        player.GiveNamedItem("weapon_deagle");
        player.GiveNamedItem("weapon_knife");
        player.GiveNamedItem("weapon_flashbang");
        player.GiveNamedItem("weapon_smokegrenade");
        player.GiveNamedItem("item_assaultsuit");

        // Switch to main weapon
        player.ExecuteClientCommand("slot1");
    }

    private void SetPlayerHealth(CCSPlayerController player, int hp)
    {
        if (!player.PawnIsAlive || player.Pawn.Value == null || hp < 0)
            return;
        
        if (hp > 100)
            player.Pawn.Value.MaxHealth = hp;
        player.Pawn.Value.Health = hp;
        Utilities.SetStateChanged(player.Pawn.Value, "CBaseEntity", "m_iHealth");
    }

    private void GenerateRandomPractice(int player_slot)
    {
        player_manager[player_slot].enabled_targets.Clear();
        int practice_no = player_manager[player_slot].practice_no;
        
        for (int i = 0; i < practices[practice_no].targets.Count; i++)
            player_manager[player_slot].enabled_targets.Add(i);

        if (player_manager[player_slot].training_mode == 0)
        {
            // 0: Use 70% of the targets.
            int num_targets = (int)(0.7 * practices[practice_no].targets.Count);
            Random rnd = new Random(DateTime.Now.Millisecond);

            int num_to_remove = practices[practice_no].targets.Count - num_targets;
            for (int i = 0; i < num_to_remove; i++)
                player_manager[player_slot].enabled_targets.RemoveAt(rnd.Next(player_manager[player_slot].enabled_targets.Count));
        }
        // 1: Use all of the targets.
    }
}
