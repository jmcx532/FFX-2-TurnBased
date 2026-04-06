
using System.Data;
using TerraFX.Interop.Windows;

namespace Fahrenheit.Modules.FFX2TurnBased;

public unsafe partial class ATBRecoveryModule : FhModule {

    // TURN ORDER WINDOW STUFF ----------------------------------------------------------------------------

    const int CHR_STRIDE = 0x17E0;
    const int ATB_REMAIN_OFFSET = 0x9D8;
    const int ATB_LENGTH_OFFSET = 0x9DC;

    const uint TURNS_TO_SHOW = 16;
    

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

    public struct SimTurnEntry {
        public uint chr_id; 
        public int time; // at what point of simulation is the turn
        public string chr_name;
        public bool is_Targeted;

        public SimTurnEntry(uint chrId, int simTime, string chrName, bool isTargeted) {
            chr_id = chrId;
            time = simTime;
            chr_name = chrName;
            is_Targeted = isTargeted;
        }
    }

    class SimBattleUnit {
        public int chr_id;
        public int base_addr;
        public string chr_name;
        public int atb_remaining;          // current time_remaining
        public bool hasHpRemaining;
        public bool isActualUnit;

        // Status flags
        public bool hasHaste;
        public bool hasSlow;

        public bool isTargeted;
    }


    List<SimBattleUnit> BattleUnits = new List<SimBattleUnit>();
    List<SimTurnEntry> TurnSimulation() {

        BattleUnits.Clear(); // empty the valid BattleUnits list
        // generate list of valid battle units to show
        int[] length_array = new int[0x1f];
        // Build BattleUnits list - should only contain characters who should appear on Turn list
        for(int i = 0; i < length_array.Length; i++) {
            SimBattleUnit chr = new SimBattleUnit();
            chr.chr_id = i;
            chr.base_addr = h_MsGetChr((uint)i);
            chr.chr_name = ReadChrName((uint)chr.chr_id);
            chr.atb_remaining = ReadATBValue((uint)chr.chr_id);
            chr.hasHpRemaining = *(int*)(chr.base_addr + 0x3b4) > 0;
            chr.isActualUnit = *(byte*)(chr.base_addr + 0x1784) == 1;

            chr.hasHaste = *(sbyte*)(chr.base_addr + 0x43c) != 0;
            chr.hasSlow = *(sbyte*)(chr.base_addr + 0x43d) != 0;

            if (chr.isActualUnit && chr.hasHpRemaining) {
                BattleUnits.Add(chr);
            }
        }
        // You now have a list of BattleUnits that can/should appear on the turn list


        int sim_time = 0; //time starting point
        List<SimTurnEntry> turn_order = new List<SimTurnEntry>(); // Create a List<TurnEntry> This will be returned so render_imgui can build a window with CTBStyleBars and names
        // begin simulating turns -  A turn entry contains a chr_id, their name and a timeline value for CTBStyleBar
        for (int i = 0; i < TURNS_TO_SHOW; i++) {

            // get chr id of who has the turn at current point in simulation
            int ChrIdWhoHasTurn = BattleUnits.FirstOrDefault(chr => chr.atb_remaining == 0)?.chr_id ?? 0;
            

            SimTurnEntry turn = new SimTurnEntry();
            turn.chr_id = (uint)ChrIdWhoHasTurn;
            turn.time = sim_time;
            turn.chr_name = ReadChrName((uint)ChrIdWhoHasTurn);
            turn.is_Targeted = BattleUnits.FirstOrDefault(chr => chr.chr_id == ChrIdWhoHasTurn).isTargeted == true;
            turn_order.Add(turn);
            //Turn added to returned list of turn entries

            // Begin status effect preview
            ushort hovered_command = GetHoveredCommand(); // used for previewing status effect changes, and turn recovery

            // how their highlighted command might affect their next turn and other battle units
            for (int t = 0; t < BattleUnits.Count; t++) {
                BattleUnits[t].isTargeted = IsTargeted((uint)BattleUnits[t].chr_id);
            }

            int cmd_base = h_MsGetComData(hovered_command, (byte*)0);

            uint com_dmg_data = *(uint*)(cmd_base + 0x1c);                  // get damage flags
            bool comHealsStatuses = ((com_dmg_data >> 5) & 1) != 0;
            bool commandInflictsHaste = (*(byte*)(cmd_base + 0x4b) > 0) && !comHealsStatuses;      // Does command inflict Haste
            bool commandInflictsSlow = (*(byte*)(cmd_base + 0x4c) > 0) && !comHealsStatuses;       // Does command inflict Slow

            uint com_exp_data = *(uint*)(cmd_base + 0x14);                  // get exp_data flags
            

            uint WEAK_DELAY = FhUtil.get_at<uint>(0x9f8ea0);
            uint STRONG_DELAY = FhUtil.get_at<uint>(0x9f8ea4);


            bool com_weak_delay = (com_exp_data & 0x1000) != 0;             // is weak delay flag set?
            bool com_strong_delay = (com_exp_data & 0x2000) != 0;           // is strong delay flag set?
            bool cmdDelaysNoSlow = (com_weak_delay || com_strong_delay) && !commandInflictsSlow; // Does the command only delay, no Slow statuse effect chance

            foreach (var unit in BattleUnits) {
                if (unit.isTargeted) {
                    int original_atb_rem = unit.atb_remaining;
                    if (commandInflictsHaste) { unit.hasHaste = true; }// Update Haste bool
                    if (commandInflictsSlow) { unit.hasSlow = true; }  // Update Slow bool

                    if (com_weak_delay) { 
                        if (cmdDelaysNoSlow) { unit.atb_remaining += (int)WEAK_DELAY; }
                        if (commandInflictsSlow && !unit.hasSlow) { unit.atb_remaining += (int)WEAK_DELAY; }
                    }
                    if (com_strong_delay) {
                        if (cmdDelaysNoSlow) { unit.atb_remaining += (int)STRONG_DELAY; }
                        if (commandInflictsSlow && !unit.hasSlow) { unit.atb_remaining += (int)STRONG_DELAY; }
                    }

                    bool cmdHasATBHealingOrDmg = (*(byte*)(cmd_base + 0x27)) == 4;  // Does the command target ATB
                    bool cmdTgtsATBNoHasteOrSlow = (cmdHasATBHealingOrDmg && !commandInflictsHaste && !commandInflictsSlow);
                    bool com_heals = ((com_dmg_data >> 4) & 1) != 0;

                    if (cmdHasATBHealingOrDmg) {
                        byte cmd_power = *(byte*)(cmd_base + 0x2b);
                        float multiplier = (cmd_power / 16.0f);
                        int amount_to_modify_by = (int)((float)original_atb_rem * multiplier);

                        if (com_heals) {
                            // Apply ATB healing only if Haste can actually be applied
                            if (commandInflictsHaste && !unit.hasHaste) {
                                unit.atb_remaining -= amount_to_modify_by;
                            }
                            if (cmdTgtsATBNoHasteOrSlow) { unit.atb_remaining -= amount_to_modify_by; }
                        }
                        else {
                            // Apply ATB damage only if Slow can actually be applied
                            if (commandInflictsSlow && !unit.hasSlow) {
                                unit.atb_remaining += amount_to_modify_by;
                            }
                            if (cmdTgtsATBNoHasteOrSlow) { unit.atb_remaining += amount_to_modify_by; }
                        }
                    }
                }
            }


            // turn recovery -- calculate recovery for ChrIdWhoHasTurn
            SimBattleUnit? considered_unit = BattleUnits.FirstOrDefault(chr => chr.chr_id == ChrIdWhoHasTurn);
            if (i == 0) {
                if (considered_unit != null){
                    // update the units recovery time
                    considered_unit.atb_remaining = SimGetATBRestTime((uint)ChrIdWhoHasTurn, hovered_command, true);
                }
            }
            else {
                if (considered_unit != null) {
                    // update the units recovery time
                    considered_unit.atb_remaining = SimGetATBRestTime((uint)ChrIdWhoHasTurn, 0x2C30, false);
                }
            }


            // Find next time advancement (smallest ATB remaining value until 0)
            int next_turn_value = BattleUnits              // from battle_units List<SimBattleUnit>
            .Min(unit => unit.atb_remaining);               // Find the minimum atb_remaining value

            // Advance time - deduct the smallest ATB remaining value from ALL characters - next chr now has atb_remaining of 0
            for (int iterator = 0; iterator < BattleUnits.Count; iterator++) {
                BattleUnits[iterator].atb_remaining -= next_turn_value;
            }

            sim_time += next_turn_value;

        }

        return turn_order;
    }

    public int SimGetATBRestTime(uint chr_id, uint command_id, bool evaluate_spherechange) { 
        int recovery_time;
        SimBattleUnit? BattleUnit = BattleUnits.FirstOrDefault(chr => chr.chr_id == chr_id);
        int cmd_base_address = h_MsGetComData(command_id, (byte*)(0));
        int cmd_recovery_time = (int)(*(ushort*)(cmd_base_address + 0x22) * 10000);
        byte agility = (*(byte*)(BattleUnit.base_addr + 0x39a));

        // Build custom divisor - as defined in ATBRecovery_Main.cs
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

        recovery_time = h_clamp_between((int)((cmd_recovery_time / agility_divisor)), 0, 99999);

        if (BattleUnit.hasHaste){ recovery_time = recovery_time / 2; }
        if (BattleUnit.hasSlow) { recovery_time = recovery_time * 2; }

        // percentage reduction -- used in auto-abilties that reduce recovery time
        int percent_reduction = calc_aa_cmd_recov_reduction(BattleUnit.base_addr, command_id, (int)cmd_base_address);
        //apply auto ability reduction
        recovery_time = ((100 - percent_reduction) * recovery_time) / 100;


        // calculate recovery time if spherechanging
        byte spherechange_menu_open = FhUtil.get_at<byte>(0x9F7000);
        ushort highlighted_dressphere = 0x5001;
        switch (chr_id) {
            case 0: // Yuna
                highlighted_dressphere = FhUtil.get_at<ushort>(0xA016F6);
                break;
            case 1: // Rikku
                highlighted_dressphere = FhUtil.get_at<ushort>(0xA01776);
                break;
            case 2: // Paine
                highlighted_dressphere = FhUtil.get_at<ushort>(0xA017F6);
                break;
        }

         
        if (spherechange_menu_open == 1 && evaluate_spherechange) {
            int job_base_address = GetDressphereJobData(highlighted_dressphere);
            if (job_base_address != 0) {
                byte[] agility_growth_constants = new byte[5];

                for (int i = 0; i < 5; i++) {
                    byte agl_constant = *(byte*)(job_base_address + 0x28 + i);
                    agility_growth_constants[i] = agl_constant;
                }

                byte chr_level = *(byte*)(BattleUnit.base_addr + 0x380);
                //e.g berserker
                byte ac0 = agility_growth_constants[0]; // 0 or 1, gets divided by 10 to usually give 0 or 0.1
                byte ac1 = agility_growth_constants[1]; //80
                byte ac2 = agility_growth_constants[2]; //58
                byte ac3 = agility_growth_constants[3]; //200
                byte ac4 = agility_growth_constants[4]; //4

                float ac0f = ac0 / 10f;
                float ac1f = ac1 == 0 ? 1f : ac1;
                float ac3f = ac3 == 0 ? 1f : ac3;
                float ac4f = ac4 == 0 ? 1f : ac4;
                float ds_agility = (chr_level * ac0f) + ((chr_level / ac1f) + ac2) - ((chr_level * chr_level) / 16f / ac3f / ac4f);

                recovery_time = (int)((spherechange_atb_cost * 10000) / (ds_agility + 1f));
                if (BattleUnit.hasHaste) { recovery_time = recovery_time / 2; }
                if (BattleUnit.hasSlow) { recovery_time = recovery_time * 2; }
            }
        }// end if spherechange recovery

        return recovery_time;
    }


    int GetDressphereJobData(ushort dressphere_id) {
        int job_data_address;

        int job_bin_addr = FhUtil.get_at<int>(0x9f9188);// read job.bin pointer


        uint ds_id = (uint)(dressphere_id & 0xfff);
        uint header_size = 32;
        uint struct_size = 0xe4;

        job_data_address = (int)(job_bin_addr + header_size + (ds_id * struct_size));

        return job_data_address;
    }

    // returns true if chr_id is being targeted
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
    public unsafe string ReadChrName(uint chr_id) {
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

    uint GetCharacterColor(int chrId) {
        return chrId switch {
            0 => ImGui.ColorConvertFloat4ToU32(new Vector4(1.0f, 0.25f, 0.55f, 1f)), // red/pink - Yuna
            1 => ImGui.ColorConvertFloat4ToU32(new Vector4(1.0f, 0.63f, 0.04f, 1f)), // yellow - Rikku 
            2 => ImGui.ColorConvertFloat4ToU32(new Vector4(0.5f, 0.4f, 1.0f, 1f)),   // purple - Paine
            3 => ImGui.ColorConvertFloat4ToU32(new Vector4(0.6f, 0.15f, 0.3f, 1f)),   // darker pink - Pistil?
            4 => ImGui.ColorConvertFloat4ToU32(new Vector4(0.6f, 0.15f, 0.3f, 1f)),   // darker pink - Pistil?
            5 => ImGui.ColorConvertFloat4ToU32(new Vector4(1f, 0.8f, 0.3f, 1f)),   // gold - Smasher/Crusher?
            6 => ImGui.ColorConvertFloat4ToU32(new Vector4(1f, 0.8f, 0.3f, 1f)),   // gold - Smasher/Crusher?
            7 => ImGui.ColorConvertFloat4ToU32(new Vector4(0.4f, 0.25f, 0.65f, 1f)),   // darker purple - Dextral/Sinistral?
            8 => ImGui.ColorConvertFloat4ToU32(new Vector4(0.4f, 0.25f, 0.65f, 1f)),   // gold - Dextral/Sinistral?
            _ => ImGui.ColorConvertFloat4ToU32(new Vector4(0.8f, 0.8f, 0.8f, 1f)), // enemies / fallback
        };
    }

    private int[] allowedSteps = new int[]
{
    -3, -2, -1, 0, 1, 2, 3, 4, 5
};

    // TURN ORDER WINDOW RENDERING --------------------------------------------------
    public override void render_imgui() {
        base.render_imgui();
        int num_allies_ready = FhUtil.get_at<int>(0xDB7480);

        // if a player character has a turn - show the turn order window
        if (num_allies_ready != 0) {


            ImGui.PushStyleColor(ImGuiCol.WindowBg, new Vector4(0.18f, 0.28f, 0.15f, 0.0f)); // Green
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(4, 2));
            
            
            ImGui.SetNextWindowSize(new Vector2(585, 45), ImGuiCond.Always);
            // Turn list, musical staff style
            ImGui.Begin(
                "TurnOrderStaff",
                ImGuiWindowFlags.NoTitleBar |
                ImGuiWindowFlags.NoResize |
                ImGuiWindowFlags.NoScrollbar |
                ImGuiWindowFlags.NoScrollWithMouse |
                ImGuiWindowFlags.NoFocusOnAppearing |
                ImGuiWindowFlags.NoCollapse
            );

            var draw = ImGui.GetWindowDrawList();
            Vector2 canvasPos = ImGui.GetCursorScreenPos();
            Vector2 canvasSize = ImGui.GetContentRegionAvail();
            Vector2 pos = ImGui.GetWindowPos();
            Vector2 size = ImGui.GetWindowSize();

            // gradient background
            uint bgLeft  = ImGui.GetColorU32(new Vector4(0.18f, 0.28f, 0.15f, 0.2f)); // low alpha
            uint bgMid   = ImGui.GetColorU32(new Vector4(0.18f, 0.28f, 0.15f, 1.0f)); // full alpha
            uint bgRight = ImGui.GetColorU32(new Vector4(0.18f, 0.28f, 0.15f, 0.2f)); // low alpha

            draw.AddRectFilledMultiColor( // Left half
                new Vector2(canvasPos.X, canvasPos.Y),
                new Vector2(canvasPos.X + canvasSize.X * 0.5f),
                bgLeft, bgMid, bgMid, bgLeft
            );

            draw.AddRectFilledMultiColor( // Right half
            new Vector2(canvasPos.X + canvasSize.X * 0.5f, canvasPos.Y),
            new Vector2(canvasPos.X + canvasSize.X, canvasPos.Y + canvasSize.Y),
            bgMid, bgRight, bgRight, bgMid
            );

            // Background rectangle inside window? Do I need another one? 
            /*
            draw.AddRectFilled(
                canvasPos,
                canvasPos + canvasSize,
                ImGui.ColorConvertFloat4ToU32(new Vector4(0.05f, 0.5f, 0.08f, 0.5f))
            );*/

            // Draw top and bottom yellow gradient border
            float win_border_thickness = 4.0f;
            // Colors with varying alpha
            uint colLeft  = ImGui.GetColorU32(new Vector4(0.7f, 0.7f, 0f, 0.4f)); // low alpha
            uint colMid   = ImGui.GetColorU32(new Vector4(0.7f, 0.7f, 0f, 1.0f)); // full alpha
            uint colRight = ImGui.GetColorU32(new Vector4(0.7f, 0.7f, 0f, 0.4f)); // low alpha

            float y = pos.Y;

            // gradient - top window border
            draw.AddRectFilledMultiColor( // Left half
                new Vector2(pos.X, y),
                new Vector2(pos.X + size.X * 0.5f, y + win_border_thickness),
                colLeft, colMid, colMid, colLeft
            );
            draw.AddRectFilledMultiColor( // Right half
                new Vector2(pos.X + size.X * 0.5f, y),
                new Vector2(pos.X + size.X, y + win_border_thickness),
                colMid, colRight, colRight, colMid
            );
            // gradient - bottom window border
            float yBottom = pos.Y + size.Y - win_border_thickness;
            draw.AddRectFilledMultiColor( //Left half
                new Vector2(pos.X, yBottom),
                new Vector2(pos.X + size.X * 0.5f, yBottom + win_border_thickness),
                colLeft, colMid, colMid, colLeft
            );
            draw.AddRectFilledMultiColor( // Right half
                new Vector2(pos.X + size.X * 0.5f, yBottom),
                new Vector2(pos.X + size.X, yBottom + win_border_thickness),
                colMid, colRight, colRight, colMid
            );

            
            // Start drawing musical notes sheet
            float padding = 4f;
            float timelineStartX = canvasPos.X + padding;
            float timelineEndX = canvasPos.X + canvasSize.X - padding;
            float timelineWidth = timelineEndX - timelineStartX;

            // Staff setup
            float centerY = canvasPos.Y + canvasSize.Y * 0.5f;
            float staffSpacing = 6f; // distance between lines

            // Draw the 5 staff lines
            for (int i = -2; i <= 2; i++) {
                float y2 = centerY + i * staffSpacing;

                draw.AddLine(
                    new Vector2(timelineStartX, y2),
                    new Vector2(timelineEndX, y2),
                    ImGui.ColorConvertFloat4ToU32(new Vector4(0.7f, 0.7f, 0.8f, 0.2f))
                );
            }

            // Turn data
            var turnList = TurnSimulation();

            // ordered mapping
            var orderedChars = turnList
            .Select(t => (int)t.chr_id)
            .Distinct()
            .OrderBy(id => id)
            .ToList();

            Dictionary<int, int> pitchMapFrame = new();

            for (int i = 0; i < orderedChars.Count; i++) {
                int chrId = orderedChars[i];

                int index = Math.Min(i, allowedSteps.Length - 1);
                pitchMapFrame[chrId] = allowedSteps[index];
            }

            float minTime = turnList.Min(t => t.time);
            float maxTime = turnList.Max(t => t.time);

            float Normalize(float t) {
                if (maxTime - minTime < 0.001f) return 0f;
                return (t - minTime) / (maxTime - minTime);
            }

            // Draw notes
            for (int i = 0; i < turnList.Count; i++) {
                var entry = turnList[i];
                int chrId = (int)entry.chr_id;

                float norm = Normalize(entry.time);
                float x = timelineStartX + norm * timelineWidth;

                bool isTarget = turnList[i].is_Targeted;

                float radius = isTarget ? 5.0f : 4.2f;
                float pulse = (float)((Math.Sin(ImGui.GetTime() * 8) + 1) * 0.5);

                if (isTarget) {
                    radius += pulse * 1.2f;
                }

                uint color = GetCharacterColor(chrId);

                int step = pitchMapFrame[chrId];

                // Each step is half a staff spacing (lines + spaces)
                float y3 = centerY + step * (staffSpacing * 0.5f);


                // Stem direction (like real notation)
                float stemInset = 1.0f; // tweak this (0.3–1.0 range)
                bool stemUp = step < 0;

                // Base + pulse growth
                float baseStemLength = 12.0f;
                float extraStem = isTarget ? (pulse * 4.0f) : 0.0f; // tweak 6.0f for intensity
                float stemLength = baseStemLength + extraStem;
                float stem_thickness = isTarget ? (2.5f + pulse * 0.5f) : 2.5f;

                // draw note head outline
                uint note_outline_color  = ImGui.GetColorU32(new Vector4(0f, 0f, 0f, 0.5f));
                draw.AddCircleFilled(new Vector2(x, y3), radius + 1.0f, note_outline_color, 16);

                //draw stem outline
                if (stemUp) {
                    draw.AddLine(
                        new Vector2(x + radius - stemInset, y3),
                        new Vector2(x + radius - stemInset, y3 - stemLength - 0.5f),
                        note_outline_color,
                        stem_thickness + 2.0f
                    );
                } else {
                    draw.AddLine(
                        new Vector2(x - radius + stemInset, y3),
                        new Vector2(x - radius + stemInset, y3 + stemLength + 0.5f),
                        note_outline_color,
                        stem_thickness + 2.0f
                        );
                }

                 // Draw note head
                 draw.AddCircleFilled(new Vector2(x, y3), radius, color, 16);

                // draw stem
                if (stemUp) {
                    draw.AddLine(
                        new Vector2(x + radius - stemInset, y3),
                        new Vector2(x + radius - stemInset, y3 - stemLength),
                        color,
                        stem_thickness
                    );
                }
                else {
                    draw.AddLine(
                        new Vector2(x - radius + stemInset, y3),
                        new Vector2(x - radius + stemInset, y3 + stemLength),
                        color,
                        stem_thickness
                    );
                }

                // Optional: ledger lines if outside staff
                if (step <= -5 || step >= 5) {
                    float ledgerY = y3;

                    draw.AddLine(
                        new Vector2(x - 6, ledgerY),
                        new Vector2(x + 6, ledgerY),
                        ImGui.ColorConvertFloat4ToU32(new Vector4(0.8f, 0.8f, 0.9f, 0.3f)),
                        1f
                    );
                }
            }


            ImGui.End();
            ImGui.PopStyleVar(1);
            ImGui.PopStyleColor(1);
        }
    }
}


