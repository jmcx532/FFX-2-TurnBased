

namespace Fahrenheit.Modules.FFX2TurnBased;

public unsafe partial class ATBRecoveryModule : FhModule {

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
    int ReadATBValue(uint chr_id) {
        int chr_base_addr = h_MsGetChr(chr_id);
        int atb_remaining = *(int*)(chr_base_addr + ATB_REMAIN_OFFSET);

        return atb_remaining;
    }

    // similar to above, but used to calculate values for future turns
    // construct an array of floats, which are used in CTBStyleBar(remaining, size)
    int[] GetNextATBValues(uint chr_id, bool isTheirTurn) {
        int chr_base_addr = h_MsGetChr(chr_id);


        int atb_remaining = *(int*)(chr_base_addr + ATB_REMAIN_OFFSET);
        // create an array to store 2 future turn values
        int[] atb_values_array = new int[8];

        //get the command the player hovers over, and run the ATB Recovery calculation, add it to their current value for the answer
        ushort hovered_command = GetHoveredCommand();                   // id of command
        int cmd_base = h_MsGetComData(hovered_command, (int*)0);        // root address of cmd in loaded command.bin

        //for character isTheirTurn - the turn after, calculate using Attack and add the result of the previous calculation
        int immediate_turn_atb_val = h_MsATBgetRestTime(chr_id, hovered_command);
        atb_values_array[0] = immediate_turn_atb_val;


        bool cmdHasATBHealingOrDmg = (*(byte*)(cmd_base + 0x27)) == 4;  // Does the command target ATB
        uint com_dmg_data = *(uint*)(cmd_base + 0x1c);                  // get damage flags
        uint com_exp_data = *(uint*)(cmd_base + 0x14);                  // get exp_data flags
        uint com_cursor = *(uint*)(cmd_base + 0x10);

        uint com_cursor_target = FhUtil.get_bits(com_cursor, 2, 2);

        bool com_heals = ((com_dmg_data >> 4) & 1) != 0;                // does the command heal? or damage?
        bool com_weak_delay = (com_exp_data & 0x1000) != 0;          // is weak delay flag set?
        bool com_strong_delay = (com_exp_data & 0x2000) != 0;        // is strong delay flag set?

        bool isTargeted = IsTargeted(chr_id);

        bool commandInflictsHaste = *(byte*)(cmd_base + 0x4b) > 0;
        bool commandInflictsSlow = *(byte*)(cmd_base + 0x4c) > 0;


        // ATB Healing/Damage preview
        if (cmdHasATBHealingOrDmg && isTargeted) {
            byte cmd_power = *(byte*)(cmd_base + 0x2b);
            float multiplier = (cmd_power / 16.0f);
            int atb_damage;
            if (isTheirTurn) {
                atb_damage = (int)((float)immediate_turn_atb_val * multiplier);
                if (com_heals) { atb_damage = -atb_damage;}
                immediate_turn_atb_val = immediate_turn_atb_val + atb_damage;
            }
            else {
                atb_damage = (int)((float)atb_remaining * multiplier);
                if (com_heals) { atb_damage = -atb_damage; }
                atb_remaining = atb_remaining + atb_damage;
            }

        }

        // Delay preview
        uint potential_delay = 0;
        if (isTargeted) {
            if (com_weak_delay) {
                potential_delay += FhUtil.get_at<uint>(0x9f8ea0);
            }
            if (com_strong_delay) {
                potential_delay += FhUtil.get_at<uint>(0x9f8ea4);
            }
        }

        int wait_value_per_turn = h_MsATBgetRestTime(chr_id, 0x2C30);
        // Haste / Slow handling
        if (commandInflictsHaste && isTargeted) {
            if (*(int*)(chr_base_addr + 0x4b8) < 1) {
                wait_value_per_turn = wait_value_per_turn / 2;
            }
        }
        if (commandInflictsSlow && isTargeted) {
            if (*(int*)(chr_base_addr + 0x4b9) < 1) {
                wait_value_per_turn = wait_value_per_turn / 2;
            }
        }


        // for the character that has the turn currently
        if (isTheirTurn) {
            for (int i = 1; i < 8; i++) {
                int future_turn_atb_val = (int)(immediate_turn_atb_val + potential_delay + (wait_value_per_turn * i));
                //invert and calculate ticks
                atb_values_array[i] = future_turn_atb_val;
            }

        }
        else {
            for (int i = 0; i < 8; i++) {
                int future_turn_atb_val = (int)(atb_remaining + potential_delay +(wait_value_per_turn * i));
                atb_values_array[i] = future_turn_atb_val;
            }
        }

        return atb_values_array;

    }

    public bool IsTargeted(uint chr_id) {
        uint targeted_chrs_field = FhUtil.get_at<uint>(0xdb74b8);
        uint[] targeted_chrs = new uint[31];

        for (int i = 0;i < targeted_chrs.Length; i++) {
            targeted_chrs[i] = (targeted_chrs_field >> i) & 1;
        }

        if (targeted_chrs[chr_id] == 1) {
            return true;
        }
        else {
            return false;
        }
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
        int chr_base_addr = h_MsGetChr(chr_id);
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

            int chr_structs_start = h_MsGetChr(0);
            // get the ID of the character who currently has the turn
            uint active_chr_id = FhUtil.get_at<uint>(0x00DB747C);

            /* create a list that will store character's turn information
             * ATB Remaining values
             * String for their character name.
             * chr_id
             */
            var TurnOrderList = new List<Tuple<int, string, uint>>();

            // add immediate next turn entries - including 0 left for character with current turn 
            for (uint chr_id = 0; chr_id < 0x1f; chr_id++) {

                int chr_base_addr = h_MsGetChr(chr_id);
                bool isTheirTurn = *(int*)(chr_base_addr + 0x9d8) < 1 ;
                byte actual_unit = *(byte*)(chr_base_addr + 0x1784);
                int remaining_hp = *(int*)(chr_base_addr + 0x3b4);

                bool firstTurnAdded = false;
                //add current turn entry
                if (actual_unit == 1 && remaining_hp > 0 && isTheirTurn && !firstTurnAdded) {
                    TurnOrderList.Add(Tuple.Create(ReadATBValue(chr_id), ReadChrName(chr_id), chr_id));
                    firstTurnAdded = true;
                }

                // if is an actual unit and has HP remaining - add future turn entries
                if (actual_unit == 1 && remaining_hp > 0) {
                    int[] atb_values = GetNextATBValues(chr_id, isTheirTurn);
                    for (int i = 0; i < atb_values.Length; i++) {
                        TurnOrderList.Add(Tuple.Create(atb_values[i], ReadChrName(chr_id), chr_id));
                    }

                }
            }

            // sort and draw from the Turn Order list
            var sortedList = TurnOrderList.OrderBy(x => x.Item1).ToList();
            // get min/max and range for normalisation so all bars aren't all bright and nearly full
            float minTicks = TurnOrderList.Min(x => x.Item1);
            float maxTicks = TurnOrderList.Max(x => x.Item1);
            float range = Math.Max(1f, maxTicks - minTicks);

            // for each item in TurnOrderList (sorted) - Create a CTBStyleBar, and on the same line, print the character name.
            foreach (var item in sortedList) {
                float normalized = (item.Item1 - minTicks) / range;

                CTBStyleBar(1 - normalized, new Vector2( 32, 28));
                ImGui.SameLine();
                ImGui.Text(item.Item2);
            }

            ImGui.End();
        }
    }
}


