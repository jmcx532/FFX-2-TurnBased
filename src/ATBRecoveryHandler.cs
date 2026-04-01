// SPDX-License-Identifier: MIT
/* A module for FFX-2 that changes the ATB Recovery time after a command has been used
 * THIS MODULE IMPLEMENTS THE CTB IMGUI
 * Haste halves recovery time while Slow doubles is (similar to FFX)
 * It also reimplements the cooldown reduction from auto-abilities like Black Magic Lv.2 or Turbo Bushido
 * as the Charge Time mechanic was removed.
 */

namespace Fahrenheit.Modules.FFX2TurnBased;

[FhLoad(FhGameId.FFX2)]
public unsafe class ATBRecoveryModule : FhModule {
    protected readonly FhLogger _logger;

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
    public unsafe delegate int get_cmd_base_addr(uint command_id, int* param_2);
    //624cd0
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int clamp_between(int param_1, int param_2, int param_3);

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

    //SUB-FUNCTIONS
    public int h_get_chr_addr(uint chr_id) {
        return _chr_addr_handle.orig_fptr.Invoke(chr_id);
    }

    //this function returns the base address for commands
    //param_1 is the command id (e.g 0x3002)
    public unsafe int h_get_cmd_addr(uint command_id, int* param_2) {
        //_logger.Info("GET_CMD_ADDR PARAM_1 is:" + param_1.ToString("X"));
        return _get_cmd_base_addr_handle.orig_fptr.Invoke(command_id, param_2);
    }
    public int h_clamp_between(int param_1, int param_2, int param_3) {
        return _clamp_between_handle.orig_fptr.Invoke(param_1, param_2, param_3);
    }

    //MAIN FUNCTIONS---------------------------------------------------------------------------------------------------
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


    // A-Ability Charge time reduction function replacement ------------------------------------------- 
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


    //6401c0 - called 'once' after character takes turn -------------------------------------------------------- 
    public uint h_atb_rec_writer(uint chr_id, int param_2, int param_3) {
        uint original_result = _atb_rec_writer_handle.orig_fptr.Invoke(chr_id, param_2, param_3);
        uint command_used = (uint)*(ushort*)(param_3 + 0xa4);

        //update character's Poison accumulator
        //_logger.Info("Poison accumulator updated");
        //nint chr_base_address = h_get_chr_addr(chr_id);
        //*(int*)(chr_base_address + 0x684) = 16001;

        /* always set their their +0xEC2 flag to 1 after acting - could maybe replace WaitFlagWriter exception commands? --> Nope
        if (chr_id < 3) {
            *(byte*)(chr_base_address + 0xec2) = 1;
        }*/

        //Time-trip handling - disable so you can't spam it over and Stop the enemy forever
        if (command_used == 0x31EA) {
            disable_time_Trip();
            _logger.Info("Disable Time Trip function" + command_used.ToString("X"));
        }
        
        return original_result;
    }

    //function to disable Psychics Time Trip command --------------------------------------
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

    // TURN ORDER WINDOW STUFF ----------------------------------------------------------------------------

    const int CHR_STRIDE = 0x17E0;
    const int ATB_REMAIN_OFFSET = 0x9D8;
    const int ATB_LENGTH_OFFSET = 0x9DC;
 

    /* function that converts a character's raw ATB remaining value, into ticks - based on the games ATB Speed config value
     * that is fixed to 95 for Slow/Normal/Fast
     * 
     * Used for character's immediate next turn -- include the character who currently has the turn
     * This and the next function use 1050 because 1050 * 95 = 99750 , and the ATB calculation clamps largest possible value to 99999
     * though it never goes this high in practice
     */
    float ReadATBTicksLeft(uint chr_id) {
        int chr_base_addr = h_get_chr_addr(chr_id);
        int atb_remaining = *(int*)(chr_base_addr + ATB_REMAIN_OFFSET);

        // if ATB is full, return 1050 - full bar
        if (atb_remaining == 0) { return 1050.0f; }
        // calculate ticks
        double ticks_left = Math.Ceiling((double)atb_remaining / 95);
        return (float)(1050 - ticks_left);
    }

    // similar to above, but used to calculate values for future turns
    float[] GetNextATBTicksLeft(uint chr_id, bool isTheirTurn) {
        int chr_base_addr = h_get_chr_addr(chr_id);
        int atb_remaining = *(int*)(chr_base_addr + ATB_REMAIN_OFFSET);
        // create an array to store 2 future turn values
        float[] ticks_left_array = new float[2];

        //for the character that has the turn currently
        if (isTheirTurn) {
            //get the command the player hovers over, and run the ATB Recovery calculation, add it to their current value for the answer
            ushort hovered_command = GetHoveredCommand();
            int future_turn_atb_val = h_atb_recovery_calculator(chr_id, hovered_command) + atb_remaining;
            //for the turn after, calculate using Attack and add the result of the previous calculation
            int future_turn_atb_val2 = h_atb_recovery_calculator(chr_id, 0x2C30) + future_turn_atb_val;
            //invert and calculate ticks
            ticks_left_array[0] = 1050 - (future_turn_atb_val / 95);
            ticks_left_array[1] = 1050 - (future_turn_atb_val2 / 95);

        }
        else {
            //for character's that don't have the current turn - calculate 2 future turn ticks using Attack
            int future_turn_atb_val = h_atb_recovery_calculator(chr_id, 0x2C30) + atb_remaining;
            int future_turn_atb_val2 = h_atb_recovery_calculator(chr_id, 0x2C30)+ future_turn_atb_val;
            //invert and calculate ticks
            ticks_left_array[0] = 1050 - (future_turn_atb_val / 95);
            ticks_left_array[1] = 1050 - (future_turn_atb_val2 / 95);
        }
        
        return ticks_left_array;

    }

    //returns the id of which command is being hovered over, used to update the view when current character hovers over different commands
    ushort GetHoveredCommand() {
        ushort sub  = FhUtil.get_at<ushort>(0x00DB7388);
        ushort main = FhUtil.get_at<ushort>(0x00DB7380);

        return (sub != 0x00FF && sub != 0xFF00)
            ? sub
            : main;
    }

    // CTB Style widget ----------------------------
    void CTBStyleBar(float normalized_value, Vector2 size) {

        var draw = ImGui.GetWindowDrawList();
        Vector2 pos = ImGui.GetCursorScreenPos();

        // thresholds for each visual layer
        float[] layers = { 0f, 0.2f, 0.4f, .6f, 0.8f };

        Vector4[] colors =
    {
        new Vector4(0.20f, 0.00f, 0.35f, 1f), // deep purple
        new Vector4(0.35f, 0.05f, 0.55f, 1f),
        new Vector4(0.55f, 0.15f, 0.75f, 1f),
        new Vector4(0.75f, 0.30f, 0.90f, 1f),
        new Vector4(0.90f, 0.60f, 1.00f, 1f)  // brightest
    };

        uint bg = ImGui.GetColorU32(new Vector4(0.05f, 0.02f, 0.08f, 1));

        draw.AddRectFilled(pos, pos + size, bg);

        for (int i = 0; i < layers.Length; i++) {
            // Adjust the fill logic based on the new max value
            float fill = Math.Clamp((normalized_value - layers[i]) / (1f - layers[i]), 0f, 1f);

            if (fill <= 0f)
                continue;

            Vector2 fillMax = pos + new Vector2(size.X * fill, size.Y);

            draw.AddRectFilled(
                pos,
                fillMax,
                ImGui.GetColorU32(colors[i])
            );
        }

        ImGui.Dummy(size);
    }

    //function to read character's name string
    unsafe string ReadChrName(uint chr_id) {
        int chr_base_addr = h_get_chr_addr(chr_id);
        //pointer to start of Chr name string
        byte* p = (byte*)(chr_base_addr + 0x358);
        Span<byte> buf = stackalloc byte[40];

        // read character bytes into buffer
        int len = 0;
        for (int i = 0; i < 16; i++) {
            byte b = p[i];
            if (b == 0) break;
            buf[len++] = b;
        }

        // create Span for Fh decoded string
        Span<byte> decoded_string = stackalloc byte[40];
        // Decode the bytes from FFXX-2 encoding to normal ASCII
        FhEncoding.decode(buf, decoded_string, FhLangId.English, FhGameId.FFX2);

        return System.Text.Encoding.ASCII.GetString(decoded_string.Slice(0, len));
    }

    
    // TURN ORDER WINDOW RENDERING --------------------------------------------------
    public override void render_imgui() {
        base.render_imgui();
        int num_allies_ready = FhUtil.get_at<int>(0xDB7480);

        // if a player character has a turn - show the turn order window
        if (num_allies_ready != 0) {

            ImGui.Begin(
                // doesn't grab focus, and you can't collapse the window
                "Turn Order",
                ImGuiWindowFlags.NoFocusOnAppearing |
                ImGuiWindowFlags.NoCollapse
            );

            int chr_structs_start = h_get_chr_addr(0);
            // get the ID of the character who currently has the turn
            uint active_chr_id = FhUtil.get_at<uint>(0x00DB747C);

            /* create a list that will store character's turn information
             * Floats that represent how close they are to getting a turn
             * String for their character name.
             */
            var TurnOrderList = new List<Tuple<float, string>>();
            
            // add immediate next turn entries - including 0 left for character with current turn 
            for (uint chr_id = 0; chr_id < 0x1f; chr_id++) {

                int chr_base_addr = h_get_chr_addr(chr_id);
                byte actual_unit = *(byte*)(chr_base_addr + 0x1784);
                int remaining_hp = *(int*)(chr_base_addr + 0x3b4);

                // if is an actual unit and has HP remaining
                if (actual_unit == 1 && remaining_hp > 0) {
                    //add the character's immediate turn to the list
                    TurnOrderList.Add(Tuple.Create(ReadATBTicksLeft(chr_id), ReadChrName(chr_id)));
                }
            }

            //add future turn entries 
            for (uint chr_id = 0; chr_id < 0x1f; chr_id++) {

                int chr_base_addr = h_get_chr_addr(chr_id);
                byte actual_unit = *(byte*)(chr_base_addr + 0x1784);
                int remaining_hp = *(int*)(chr_base_addr + 0x3b4);

                // if is an actual unit and has HP remaining
                if (actual_unit == 1 && remaining_hp > 0) {
                    if (chr_id == active_chr_id) {
                        float[] ticks_left_array = GetNextATBTicksLeft(chr_id, true);
                        TurnOrderList.Add(Tuple.Create(ticks_left_array[0], ReadChrName(chr_id)));
                        TurnOrderList.Add(Tuple.Create(ticks_left_array[1], ReadChrName(chr_id)));
                    }
                    else {
                        float[] ticks_left_array = GetNextATBTicksLeft(chr_id, false);
                        TurnOrderList.Add(Tuple.Create(ticks_left_array[0], ReadChrName(chr_id)));
                        TurnOrderList.Add(Tuple.Create(ticks_left_array[1], ReadChrName(chr_id)));
                    }

                    
                }
            }

            // sort and draw from the Turn Order list
            var sortedList = TurnOrderList.OrderByDescending(x => x.Item1).ToList();
            // get min/max and range for normalisation so all bars aren't all bright and nearly full
            float minTicks = TurnOrderList.Min(x => x.Item1);
            float maxTicks = TurnOrderList.Max(x => x.Item1);
            float range = Math.Max(1f, maxTicks - minTicks);

            // for each item in TurnOrderList (sorted) - Create a CTBStyleBar, and on the same line, print the character name.
            foreach (var item in sortedList) {
                float normalized = (item.Item1 - minTicks) / range;

                CTBStyleBar(normalized, new Vector2(/*(1 - normalized) */ 32, 28));
                ImGui.SameLine();
                ImGui.Text(item.Item2);
            }

            ImGui.End();
        }
    }

  

    
    // FH init ------------------------------------------------------------------------------------
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
