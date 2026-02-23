// SPDX-License-Identifier: MIT
/* A module for FFX-2 that changes the ATB Recovery time after a command has been used
 * THIS MODULE IMPLEMENTS THE CTB IMGUI
 * Haste halves recovery time while Slow doubles is (similar to FFX)
 * It also reimplements the cooldown reduction from auto-abilities like Black Magic Lv.2 or Turbo Bushido
 * as the Charge Time mechanic was removed.
 */

using Hexa.NET.ImGui;
using System;
using System.Linq;
using System.Numerics;
using TerraFX.Interop.Windows;

namespace Fahrenheit.Modules.ATBRecoveryHandler;

//function delegates
//634140
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public delegate int atb_recov_calc(uint chr_id, uint command_id);
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
//6401c0
public delegate uint atb_rec_writer(uint chr_id, int param_2, int param_3);

//611450
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public delegate int get_chr_addr(uint chr_id);
//625160 
/* this function returns a base address for a command as far as the scope of ATB recovery time is concerned.
 * In reality, it checks a whole range of things, commands (item, command, monmagic), auto-abilities, Garment Grids*/
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public unsafe delegate int get_cmd_base_addr(uint command_id, int *param_2);
//624cd0
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public delegate int clamp_between(int param_1, int param_2, int param_3);

[FhLoad(FhGameId.FFX2)]
public unsafe class ATBRecoveryModule : FhModule {
    protected readonly FhLogger _logger;
    private readonly FhMethodHandle<atb_recov_calc>_atb_recovery_handle;
    private readonly FhMethodHandle<atb_rec_writer> _atb_rec_writer_handle;
    private readonly FhMethodHandle<get_chr_addr> _chr_addr_handle;
    private readonly FhMethodHandle<get_cmd_base_addr> _get_cmd_base_addr_handle;
    private readonly FhMethodHandle<clamp_between> _clamp_between_handle;


    public ATBRecoveryModule() {
        int addr_offset = 0x400000;
        _logger = new FhLogger($"TurnBased_ATBRecovery.log");

        _atb_recovery_handle = new FhMethodHandle<atb_recov_calc>(this, "FFX-2.exe", 0x634140 - addr_offset, h_atb_recovery_calculator);
        _atb_rec_writer_handle = new FhMethodHandle<atb_rec_writer>(this, "FFX-2.exe", 0x6401c0 - addr_offset, h_atb_rec_writer);
        _chr_addr_handle = new FhMethodHandle<get_chr_addr>(this, "FFX-2.exe", 0x611450 - addr_offset, h_get_chr_addr);
        _get_cmd_base_addr_handle = new FhMethodHandle<get_cmd_base_addr>(this, "FFX-2.exe", 0x625160 - addr_offset, h_get_cmd_addr);
        _clamp_between_handle = new FhMethodHandle<clamp_between>(this, "FFX-2.exe", 0x624cd0 - addr_offset, h_clamp_between);

        
    }

    //called constantly - not a one and done function - use atb_rec_writer for those types of effects
    public int h_atb_recovery_calculator(uint chr_id, uint command_id) {
        int chr_base_address;
        int cmd_base_address;

        /* Gets the character's base address */
        chr_base_address = h_get_chr_addr(chr_id);
        // FUN_00625160 - Get the commands base address, p2 was 0 in h_atb_recovery_handler (FUN_00634140) decompile
        cmd_base_address = h_get_cmd_addr(command_id, (int*)(0));

        //normal calculation
        //read the commands atb_cost and multiply
        int cmd_recovery_time = (int)(*(ushort*)(cmd_base_address + 0x22) * 10000);

        //divide that by (user's Agility + 1) -- VANILLA
        uint agility_divisor = (uint)(*(byte*)(chr_base_address + 0x39a)) + 1;

        /*CUSTOM DIVISOR
        byte agility = (*(byte*)(chr_base_address + 0x39a));
        double divisor;
        //if agility is over 100, use a stronger taper that means higher agility stats don't reduce recovery time as much.
        if (agility > 100) {
            divisor = Math.Pow(agility + 125, 0.85) + 1;
        }
        else {
            //if agility is 99 or less, use vanilla divisor
            divisor = agility + 1;
        }

        uint agility_divisor = (uint)Math.Round(divisor);

        */

        //delay from attacks to be added
        uint accrued_delay = (uint)*(int*)(chr_base_address + 0x9e0);
        //calculate ATB timer length and clamp between 0 and 99999
        int calced_recovery = h_clamp_between((int)((cmd_recovery_time / agility_divisor) + accrued_delay), 0, 99999);


        //Haste / Slow Modifier
        //if character is hasted - half recovery time
        if (*(byte*)(chr_base_address + 0x43c) != '\0') {
            calced_recovery = calced_recovery / 2;
        }
        //if character is slowed - double recovery time
        if (*(byte*)(chr_base_address + 0x43d) != '\0') {
            calced_recovery = calced_recovery * 2;
        }


        //auto ability recovery time reduction
        ushort command_used = (*(ushort*)(chr_base_address + 0xf3c));
        int percent_reduction = calc_aa_cmd_recov_reduction(chr_base_address, command_used, (int)cmd_base_address);
        //_logger.Info("Command charge time percent reduction is: " + percent_reduction);

        //apply auto ability reduction
        calced_recovery = ((100 - percent_reduction) * calced_recovery) / 100;
            
        // Accrued delay is reset 
        *(int*)(chr_base_address + 0x9e0) = 0;


        //return calculated recovery time to be written
        return calced_recovery;
    }


    //Charge time reduction function replacement
    //returns the proper percentage reduction for auto-abilities that originally reduced charge time
    //so it can be used to reduce ATB recovery instead.
    //For command_used/param_2 it is the command id (for example: 0x308f for Unhinge)
    public int calc_aa_cmd_recov_reduction(int chr_base_addr, uint command_used, int cmd_base_addr) {
        //int calced_charge_time;
        int menu_cmd_addr;
        int recov_time_reduction;
        ushort *puVar1;
        int percent_reduction;
        int incV1;
        
        //if user has no auto-abilities that reduce recovery time (originally charge time) (e.g Turbo Arcana) 
        if (*(char*)(chr_base_addr + 0x5af) == '\0') {
            return 0;
        }

        //initialise incrementer variable 1 and recov_time_reduction
        recov_time_reduction = 0;
        incV1 = 0;

        /*This is a pointer to an array inside the Chr BattleData
        *This contains up to 4 command ids relating to sub-menus (Arcana, Instinct, Black Magic for example)
        *for each auto-ability a character has that reduces a command styles charge time
        */
        puVar1 = (ushort*)(chr_base_addr + 0x5b0);

        do {
            //get the percentage reduction 
            percent_reduction = (int)*(byte*)(chr_base_addr + 0x5b8 + incV1);
            
            //get the base address of the menu command (e.g White Magic, Swordplay etc.)
            menu_cmd_addr = h_get_cmd_addr((uint)*puVar1, (int*)(0));
            
                
            //condition 1: do something with the menu commands 'sub_command' parameter
            //condition 2: do something with the player chosen command 'sub_command parameter'
            //condition 3: check if the player chosen commands 'flow_system' parameter matches the menu_cmd 'system' parameter
            if ((((*(byte*)(menu_cmd_addr + 0xd) & 0xf8) != 0) &&
                    ((*(byte*)(cmd_base_addr + 0xd) & 7) == 0)) &&
                   (*(byte*)(cmd_base_addr + 0xf) == *(byte*)(menu_cmd_addr + 0xe))) {
                    recov_time_reduction = recov_time_reduction + percent_reduction;
            }
            
            incV1 = incV1 + 1;
            puVar1 = puVar1 + 1;
        } while (incV1 < (int)(uint)*(byte*)(chr_base_addr + 0x5af));

        //if recovery time is 0, return 0 otherwise return the result
        if (recov_time_reduction == 0) {
            return 0;
        }
        recov_time_reduction = h_clamp_between(recov_time_reduction, -100, 100);
        return recov_time_reduction;
    }

    //6401c0 - called 'once' after character takes turn - 
    public uint h_atb_rec_writer(uint chr_id, int param_2, int param_3) {
        uint original_result = _atb_rec_writer_handle.orig_fptr.Invoke(chr_id, param_2, param_3);
        uint command_used = (uint)*(ushort*)(param_3 + 0xa4);

        //update character's Poison accumulator
        _logger.Info("Poison accumulator updated");
        nint chr_base_address = h_get_chr_addr(chr_id);
        *(int*)(chr_base_address + 0x684) = 16001;

        //Bugfix - always set their their +0xEC2 flag to 1 after acting - could maybe replace WaitFlagWriter exception coomands?
        if (chr_id < 3) {
            *(byte*)(chr_base_address + 0xec2) = 1;
        }

        //Time-trip handling - disable so you can't spam it over and Stop the enemy forever
        if (command_used == 0x31EA) {
            disable_time_Trip();
            _logger.Info("Disable Time Trip function" + command_used.ToString("X"));
        }
        
        return original_result;
    }

    //function to disable Psychics Time Trip command
    public void disable_time_Trip() {
        
            //get the commands data 
            int tt_exp_data = h_get_cmd_addr(0x31EA, (int*)(0));
            int tt_mp_cost = h_get_cmd_addr(0x31EA, (int*)(0));

            //set its com_dark_flag to true - this makes it drain HP instead of MP
            *(int*)(tt_exp_data + 0x14) |= (1 << 28);
            /*set its MP cost so high it requires all of the users HP -- command is greyed out because
             * it can't be used if HP is not high enough*/
            *(byte*)(tt_mp_cost + 0x26) = 128; 
        
    }


    /* TURN ORDER WINDOW STUFF */
    public struct CTBCurrentEntry {
        public string Name;
        public int Slot;
        public int ATB_Left;
    }

    public struct CTBTurn {
        public int Slot;
        public string Name;
        public int Time;      // time until that turn happens
        public bool IsGhost;  // false=next real turn, true=future preview
    }

    const int CHR_STRIDE = 0x17E0;
    const int ATB_OFFSET = 0x9D8;

    unsafe int ReadATB(int chrBase, int slot) {
        return *(int*)(chrBase + ATB_OFFSET + (slot * CHR_STRIDE));
    }

    unsafe CTBCurrentEntry BuildCTB(int baseAddr, int slot, string name) {
        int atb = ReadATB(baseAddr, slot);
        return new CTBCurrentEntry {
            Name = name,
            Slot = slot,
            ATB_Left = atb
        };
    }

    ushort GetHoveredCommand() {
        ushort sub  = FhUtil.get_at<ushort>(0x00DB7388);
        ushort main = FhUtil.get_at<ushort>(0x00DB7380);

        return (sub != 0x00FF && sub != 0xFF00)
            ? sub
            : main;
    }

    uint GetActiveMenuChr() => FhUtil.get_at<uint>(0x00DB747C);

    float NormalizeCTB(int value, int max) {
        float v = value / (float)max;
        //return MathF.Sqrt(v);   // perceptual curve
        return v;
    }

    //function to read character's name string
    unsafe string ReadChrName(int chrBase) {
        byte* p = (byte*)(chrBase + 0x358);
        Span<byte> buf = stackalloc byte[16];

        int len = 0;
        for (int i = 0; i < 16; i++) {
            byte b = p[i];
            if (b == 0) break;
            buf[len++] = b;
        }

        Span<byte> decoded_string = stackalloc byte[64];
        FhEncoding.decode(buf, decoded_string, FhLangId.English, FhGameId.FFX2);
        

        return System.Text.Encoding.ASCII.GetString(decoded_string.Slice(0, len));
    }

    private int _ctbHorizon = 1;

    public override void render_imgui() {
        base.render_imgui();
        int num_allies_ready = FhUtil.get_at<int>(0xDB7480);

        if (num_allies_ready != 0) {

            ImGui.Begin(
                "Turn Order",
                ImGuiWindowFlags.NoFocusOnAppearing |
                ImGuiWindowFlags.NoCollapse
            );

            var entries = new List<CTBCurrentEntry>();
            int chrBase = h_get_chr_addr(0);

            for (uint i = 0; i < 31; i++) {
                int chr_base_addr = h_get_chr_addr(i);
                byte actual_unit = *(byte*)(chr_base_addr + 0x1784);
                int remaining_hp = *(int*)(chr_base_addr + 0x3b4);

                if (actual_unit == 1 && remaining_hp > 0) {
                    string name = ReadChrName(chr_base_addr);
                    entries.Add(BuildCTB(chrBase, (int)i, name));
                }
            }

            var sorted = entries.OrderBy(x => x.ATB_Left).ToList();

            // ---- Build preview timeline ----
            const int PREVIEW_TURNS_PER_BATTLER = 3;
            const int QUEUE_SIZE = 16;

            var timeline = new List<CTBTurn>(sorted.Count * PREVIEW_TURNS_PER_BATTLER);

            foreach (var e in sorted) {
                int t = e.ATB_Left;

                uint active = GetActiveMenuChr();
                ushort hovered = GetHoveredCommand();

                int rec = (e.Slot == active)
                    ? h_atb_recovery_calculator((uint)e.Slot, hovered)
                    : h_atb_recovery_calculator((uint)e.Slot, 0x2C30);

                //if (rec <= 0) rec = 3000;

                for (int n = 0; n < PREVIEW_TURNS_PER_BATTLER; n++) {
                    timeline.Add(new CTBTurn {
                        Slot = e.Slot,
                        Name = e.Name,
                        Time = t,
                        IsGhost = (n != 0)
                    });

                    t += rec;
                }
            }

            var queue = timeline.OrderBy(t => t.Time).Take(QUEUE_SIZE).ToList();

            
            int frameMax = queue.Max(t => t.Time);
            if (frameMax > _ctbHorizon)
                _ctbHorizon = frameMax;
            else
                _ctbHorizon = Math.Max(frameMax, _ctbHorizon - 250); // decay slowly
            int maxTime = _ctbHorizon;
            

            // ---- Draw queue ONLY ----
            foreach (var t in queue) {

                float pct = NormalizeCTB(t.Time, maxTime);
                if (!float.IsFinite(pct)) pct = 0f;
                pct = Math.Clamp(pct, 0f, 1f);

                bool isEnemy = t.Slot >= 15;

                if (isEnemy) {
                    ImGui.PushStyleColor(ImGuiCol.PlotHistogram, new Vector4(0.85f, 0.25f, 0.25f, 1f));
                    ImGui.PushStyleColor(ImGuiCol.FrameBg, new Vector4(0.25f, 0.05f, 0.05f, 1f));
                }

                ImGui.PushID(t.Slot);
                ImGui.ProgressBar(pct, new Vector2(32, 28), $"##ctb_{t.Time}");
                ImGui.PopID();

                ImGui.SameLine();
                if(isEnemy){
                    ImGui.Text(t.Name + " " +  ((t.Slot % 15) + 1) );
                }
                else {
                    ImGui.Text(t.Name);
                }
                

                if (isEnemy) { ImGui.PopStyleColor(2); }

            }

            ImGui.End();
        }
    }

    //sub functions
    public int h_get_chr_addr(uint chr_id) {
        return _chr_addr_handle.orig_fptr.Invoke(chr_id);
    }

    //this function returns the base address for commands
    //param_1 is the command id (e.g 0x3002)
    public unsafe int h_get_cmd_addr(uint command_id, int *param_2) {
        //_logger.Info("GET_CMD_ADDR PARAM_1 is:" + param_1.ToString("X"));
         return _get_cmd_base_addr_handle.orig_fptr.Invoke(command_id, param_2);
    }
    public int h_clamp_between(int param_1, int param_2, int param_3) {
        return _clamp_between_handle.orig_fptr.Invoke(param_1, param_2, param_3);
    }

    
    //Fh init
    public override bool init(FhModContext mod_context, FileStream global_state_file) {
        _atb_recovery_handle.hook();
        _atb_rec_writer_handle.hook();
        _chr_addr_handle.hook();
        _get_cmd_base_addr_handle.hook();
        _clamp_between_handle.hook();
        return true;
    }

    public override void load_local_state(FileStream? local_state_file, FhLocalStateInfo local_state_info) { }
    public override void save_local_state(FileStream local_state_file) { }
}
