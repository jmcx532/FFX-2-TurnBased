

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
    float ReadATBTicksLeft(uint chr_id) {
        int chr_base_addr = h_MsGetChr(chr_id);
        int atb_remaining = *(int*)(chr_base_addr + ATB_REMAIN_OFFSET);

        // if ATB is full, return 1050 - full bar
        if (atb_remaining == 0) { return 1050.0f; }
        // calculate ticks
        double ticks_left = Math.Ceiling((double)atb_remaining / 95);
        return (float)(1050 - ticks_left);
    }

    // similar to above, but used to calculate values for future turns
    float[] GetNextATBTicksLeft(uint chr_id, bool isTheirTurn) {
        int chr_base_addr = h_MsGetChr(chr_id);
        int atb_remaining = *(int*)(chr_base_addr + ATB_REMAIN_OFFSET);
        // create an array to store 2 future turn values
        float[] ticks_left_array = new float[2];

        //for the character that has the turn currently
        if (isTheirTurn) {
            //get the command the player hovers over, and run the ATB Recovery calculation, add it to their current value for the answer
            ushort hovered_command = GetHoveredCommand();
            int future_turn_atb_val = h_MsATBgetRestTime(chr_id, hovered_command) + atb_remaining;
            //for the turn after, calculate using Attack and add the result of the previous calculation
            int future_turn_atb_val2 = h_MsATBgetRestTime(chr_id, 0x2C30) + future_turn_atb_val;
            //invert and calculate ticks
            ticks_left_array[0] = 1050 - (future_turn_atb_val / 95);
            ticks_left_array[1] = 1050 - (future_turn_atb_val2 / 95);

        }
        else {
            //for character's that don't have the current turn - calculate 2 future turn ticks using Attack
            int future_turn_atb_val = h_MsATBgetRestTime(chr_id, 0x2C30) + atb_remaining;
            int future_turn_atb_val2 = h_MsATBgetRestTime(chr_id, 0x2C30)+ future_turn_atb_val;
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
             * Floats that represent how close they are to getting a turn
             * String for their character name.
             */
            var TurnOrderList = new List<Tuple<float, string>>();

            // add immediate next turn entries - including 0 left for character with current turn 
            for (uint chr_id = 0; chr_id < 0x1f; chr_id++) {

                int chr_base_addr = h_MsGetChr(chr_id);
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

                int chr_base_addr = h_MsGetChr(chr_id);
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
}


