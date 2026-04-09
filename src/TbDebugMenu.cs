// SPDX-License-Identifier: MIT

namespace Fahrenheit.Modules.FFX2TurnBased.DebugMenu;

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public delegate nint get_chr_addr(uint chr_id);


[FhLoad(FhGameId.FFX2)]
public unsafe class TbDebugModule : FhModule {

    bool showMenu = false;

    //protected readonly FhLogger _logger;
    private readonly FhMethodHandle<get_chr_addr> _get_chr_addr_handle;

    public TbDebugModule() {
;       int addr_offset = 0x400000;
        _get_chr_addr_handle = new FhMethodHandle<get_chr_addr>(this, "FFX-2.exe", 0x611450 - addr_offset, h_get_chr_addr);
    }


    public override void render_imgui() {
        base.render_imgui();

        // Check if Battle Chr pointer is set
        int* chr_structs_ptr = FhUtil.ptr_at<int>(0xA0FBAC);
        if (*chr_structs_ptr != 0 && showMenu) {

            ImGui.Begin(
                "Turn-based Debug",
                ImGuiWindowFlags.NoFocusOnAppearing
                );
            for (int i = 0; i < 3; i++) {

                if (ImGui.Button("Unstick Chr" + i)) {
                    nint chr_base_addr = h_get_chr_addr((uint)i);
                    *(byte*)(chr_base_addr + 0xEC2) = 1;
                }
                nint cb2 = h_get_chr_addr((uint)i);
                byte flag_value = *(byte*)(cb2 + 0xEC2);
                ushort last_command = *(ushort*)(cb2 + 0xf3c);
                ImGui.SameLine();
                ImGui.Text("Flag value is: " + flag_value);

                ImGui.Text("Last used command: " + last_command.ToString("X"));
            }
            ImGui.End();

            ImGui.Begin(
                "Yuna Status Info",
                ImGuiWindowFlags.NoFocusOnAppearing
                );

            nint Ychr_base = h_get_chr_addr(0);

            int Yspeed1 = *(int*)(Ychr_base + 0x9e4);
            ImGui.Text("Speed1 value: " + Yspeed1);
            int Yspeed2 = *(int*)(Ychr_base + 0x9e8);
            ImGui.Text("Speed2 value: " + Yspeed2);
            int Yspeed3 = *(int*)(Ychr_base + 0x9ec);
            ImGui.Text("Speed3 value: " + Yspeed3);
            int Yspeed4 = *(int*)(Ychr_base + 0x9f0);
            ImGui.Text("Speed4 value: " + Yspeed4);

            int Ystatus_bitfield = *(int*)(Ychr_base + 0x450);
            ImGui.Text("Status Bitfield value: " + Ystatus_bitfield);
            sbyte Yprotect_timer = *(sbyte*)(Ychr_base + 0x439);
            ImGui.Text("Protect turns left: " + Yprotect_timer);
            sbyte Yhaste_timer = *(sbyte*)(Ychr_base + 0x43c);
            ImGui.Text("Haste turns left: " + Yhaste_timer);
            sbyte Ystop_timer = *(sbyte*)(Ychr_base + 0x43e);
            ImGui.Text("Stop turns left: " + Ystop_timer);
            sbyte Ydoom_count = *(sbyte*)(Ychr_base + 0x446);
            ImGui.Text("Doom turns left: " + (Ydoom_count - 1));

            sbyte Yregen_timer = *(sbyte*)(Ychr_base + 0x43b);
            ImGui.Text("Regen turns left: " + Yregen_timer);
            int Yregen_accumulator = *(int*)(Ychr_base + 0x688);
            ImGui.Text("Regen accumulator value: " + Yregen_accumulator);
            uint Yregen_threshold = *(uint*)(Ychr_base + 0x690);
            ImGui.Text("Regen threshold value: " + Yregen_threshold);

            int Ypoison_accumulator = *(int*)(Ychr_base + 0x684);
            ImGui.Text("Poison accumulator value: " + Ypoison_accumulator);
            uint Ypoison_threshold = *(uint*)(Ychr_base + 0x68c);
            ImGui.Text("Poison threshold value: " + Ypoison_threshold);

            uint Yoff_count1 = *(uint*)(Ychr_base + 0x454);//death
            ImGui.Text("off_count: death:  " + Yoff_count1);
            uint Yoff_count2 = *(uint*)(Ychr_base + 0x458);//stone
            ImGui.Text("off_count: stone:  " + Yoff_count2);
            uint Yoff_count3 = *(uint*)(Ychr_base + 0x45c);//sleep
            ImGui.Text("off_count: sleep:  " + Yoff_count3);
            uint Yoff_count4 = *(uint*)(Ychr_base + 0x460);//silence
            ImGui.Text("off_count: silence:  " + Yoff_count4);
            uint Yoff_count5 = *(uint*)(Ychr_base + 0x464);//blind
            uint Yoff_count6 = *(uint*)(Ychr_base + 0x468);//poison
            uint Yoff_count7 = *(uint*)(Ychr_base + 0x46c);//confusion
            ImGui.Text("off_count: confuse:  " + Yoff_count7);
            uint Yoff_count8 = *(uint*)(Ychr_base + 0x470);//berserk
            ImGui.Text("off_count: berserk:  " + Yoff_count8);

            ImGui.End();

            ImGui.Begin(
            "Rikku Status Info",
            ImGuiWindowFlags.NoFocusOnAppearing
            );

            nint Rchr_base = h_get_chr_addr(0);

            int Rspeed1 = *(int*)(Rchr_base + 0x9e4);
            ImGui.Text("Speed1 value: " + Rspeed1);
            int Rspeed2 = *(int*)(Rchr_base + 0x9e8);
            ImGui.Text("Speed2 value: " + Rspeed2);
            int Rspeed3 = *(int*)(Rchr_base + 0x9ec);
            ImGui.Text("Speed3 value: " + Rspeed3);
            int Rspeed4 = *(int*)(Rchr_base + 0x9f0);
            ImGui.Text("Speed4 value: " + Rspeed4);

            int Rstatus_bitfield = *(int*)(Rchr_base + 0x450);
            ImGui.Text("Status Bitfield value: " + Rstatus_bitfield);
            sbyte Rprotect_timer = *(sbyte*)(Rchr_base + 0x439);
            ImGui.Text("Protect turns left: " + Rprotect_timer);
            sbyte Rhaste_timer = *(sbyte*)(Rchr_base + 0x43c);
            ImGui.Text("Haste turns left: " + Rhaste_timer);
            sbyte Rstop_timer = *(sbyte*)(Rchr_base + 0x43e);
            ImGui.Text("Stop turns left: " + Rstop_timer);
            sbyte Rdoom_count = *(sbyte*)(Rchr_base + 0x446);
            ImGui.Text("Doom turns left: " + (Rdoom_count - 1));

            sbyte Rregen_timer = *(sbyte*)(Rchr_base + 0x43b);
            ImGui.Text("Regen turns left: " + Rregen_timer);
            int Rregen_accumulator = *(int*)(Rchr_base + 0x688);
            ImGui.Text("Regen accumulator value: " + Rregen_accumulator);
            uint Rregen_threshold = *(uint*)(Rchr_base + 0x690);
            ImGui.Text("Regen threshold value: " + Rregen_threshold);

            int Rpoison_accumulator = *(int*)(Rchr_base + 0x684);
            ImGui.Text("Poison accumulator value: " + Rpoison_accumulator);
            uint Rpoison_threshold = *(uint*)(Rchr_base + 0x68c);
            ImGui.Text("Poison threshold value: " + Rpoison_threshold);

            uint Roff_count1 = *(uint*)(Rchr_base + 0x454);//death
            ImGui.Text("off_count: death:  " + Roff_count1);
            uint Roff_count2 = *(uint*)(Rchr_base + 0x458);//stone
            ImGui.Text("off_count: stone:  " + Roff_count2);
            uint Roff_count3 = *(uint*)(Rchr_base + 0x45c);//sleep
            ImGui.Text("off_count: sleep:  " + Roff_count3);
            uint Roff_count4 = *(uint*)(Rchr_base + 0x460);//silence
            ImGui.Text("off_count: silence:  " + Roff_count4);
            uint Roff_count5 = *(uint*)(Rchr_base + 0x464);//blind
            uint Roff_count6 = *(uint*)(Rchr_base + 0x468);//poison
            uint Roff_count7 = *(uint*)(Rchr_base + 0x46c);//confusion
            ImGui.Text("off_count: confuse:  " + Roff_count7);
            uint Roff_count8 = *(uint*)(Rchr_base + 0x470);//berserk
            ImGui.Text("off_count: berserk:  " + Roff_count8);

            ImGui.End();

            ImGui.Begin(
                "Paine Status Info",
                ImGuiWindowFlags.NoFocusOnAppearing
                );
            nint Pchr_base = h_get_chr_addr(2);

            
            int Pspeed1 = *(int*)(Pchr_base + 0x9e4);
            ImGui.Text("Speed1 value: " + Pspeed1);
            int Pspeed2 = *(int*)(Pchr_base + 0x9e8);
            ImGui.Text("Speed2 value: " + Pspeed2);
            int Pspeed3 = *(int*)(Pchr_base + 0x9ec);
            ImGui.Text("Speed3 value: " + Pspeed3);
            int Pspeed4 = *(int*)(Pchr_base + 0x9f0);
            ImGui.Text("Speed4 value: " + Pspeed4);


            int Pstatus_bitfield = *(int*)(Pchr_base + 0x450);
            ImGui.Text("Status Bitfield value: " + Pstatus_bitfield);
            sbyte Pprotect_timer = *(sbyte*)(Pchr_base + 0x439);
            ImGui.Text("Protect turns left: " + Pprotect_timer);
            sbyte Phaste_timer = *(sbyte*)(Pchr_base + 0x43c);
            ImGui.Text("Haste turns left: " + Phaste_timer);
            sbyte Pstop_timer = *(sbyte*)(Pchr_base + 0x43e);
            ImGui.Text("Stop turns left: " + Pstop_timer);
            sbyte Pdoom_count = *(sbyte*)(Pchr_base + 0x446);
            ImGui.Text("Doom turns left: " + (Pdoom_count - 1));

            sbyte Pregen_timer = *(sbyte*)(Pchr_base + 0x43b);
            ImGui.Text("Regen turns left: " + Pregen_timer);
            int Pregen_accumulator = *(int*)(Pchr_base + 0x688);
            ImGui.Text("Regen accumulator value: " + Pregen_accumulator);
            uint Pregen_threshold = *(uint*)(Pchr_base + 0x690);
            ImGui.Text("Regen threshold value: " + Pregen_threshold);

            int Ppoison_accumulator = *(int*)(Pchr_base + 0x684);
            ImGui.Text("Poison accumulator value: " + Ppoison_accumulator);
            uint Ppoison_threshold = *(uint*)(Pchr_base + 0x68c);
            ImGui.Text("Poison threshold value: " + Ppoison_threshold);

            uint Poff_count1 = *(uint*)(Pchr_base + 0x454);//death
            ImGui.Text("off_count: death:  " + Poff_count1);
            uint Poff_count2 = *(uint*)(Pchr_base + 0x458);//stone
            ImGui.Text("off_count: stone:  " + Poff_count2);
            uint Poff_count3 = *(uint*)(Pchr_base + 0x45c);//sleep
            ImGui.Text("off_count: sleep:  " + Poff_count3);
            uint Poff_count4 = *(uint*)(Pchr_base + 0x460);//silence
            ImGui.Text("off_count: silence:  " + Poff_count4);
            uint Poff_count5 = *(uint*)(Pchr_base + 0x464);//blind
            uint Poff_count6 = *(uint*)(Pchr_base + 0x468);//poison
            uint Poff_count7 = *(uint*)(Pchr_base + 0x46c);//confusion
            ImGui.Text("off_count: confuse:  " + Poff_count7);
            uint Poff_count8 = *(uint*)(Pchr_base + 0x470);//berserk
            ImGui.Text("off_count: berserk:  " + Poff_count8);
            ImGui.End();

            ImGui.Begin(
                "Enemy 1 Status Info",
                ImGuiWindowFlags.NoFocusOnAppearing
                );
            nint Echr_base = h_get_chr_addr(0xf);

            int Espeed1 = *(int*)(Echr_base + 0x9e4);
            ImGui.Text("Speed1 value: " + Espeed1);
            int Espeed2 = *(int*)(Echr_base + 0x9e8);
            ImGui.Text("Speed2 value: " + Espeed2);
            int Espeed3 = *(int*)(Echr_base + 0x9ec);
            ImGui.Text("Speed3 value: " + Espeed3);
            int Espeed4 = *(int*)(Echr_base + 0x9f0);
            ImGui.Text("Speed4 value: " + Espeed4);

            int Estatus_bitfield = *(int*)(Echr_base + 0x450);
            ImGui.Text("Status Bitfield value: " + Estatus_bitfield);
            sbyte Eprotect_timer = *(sbyte*)(Echr_base + 0x439);
            ImGui.Text("Protect turns left: " + Eprotect_timer);
            sbyte Ehaste_timer = *(sbyte*)(Echr_base + 0x43c);
            ImGui.Text("Haste turns left: " + Ehaste_timer);
            sbyte Estop_timer = *(sbyte*)(Echr_base + 0x43e);
            ImGui.Text("Stop turns left: " + Estop_timer);
            sbyte Edoom_count = *(sbyte*)(Echr_base + 0x446);
            ImGui.Text("Doom turns left: " + (Edoom_count - 1));

            sbyte Eregen_timer = *(sbyte*)(Echr_base + 0x43b);
            ImGui.Text("Regen turns left: " + Eregen_timer);
            int Eregen_accumulator = *(int*)(Echr_base + 0x688);
            ImGui.Text("Regen accumulator value: " + Eregen_accumulator);
            uint Eregen_threshold = *(uint*)(Echr_base + 0x690);
            ImGui.Text("Regen threshold value: " + Eregen_threshold);

            int Epoison_accumulator = *(int*)(Echr_base + 0x684);
            ImGui.Text("Poison accumulator value: " + Epoison_accumulator);
            uint Epoison_threshold = *(uint*)(Echr_base + 0x68c);
            ImGui.Text("Poison threshold value: " + Epoison_threshold);

            uint Eoff_count1 = *(uint*)(Echr_base + 0x454);//death
            ImGui.Text("off_count: death:  " + Eoff_count1);
            uint Eoff_count2 = *(uint*)(Echr_base + 0x458);//stone
            ImGui.Text("off_count: stone:  " + Eoff_count2);
            uint Eoff_count3 = *(uint*)(Echr_base + 0x45c);//sleep
            ImGui.Text("off_count: sleep:  " + Eoff_count3);
            uint Eoff_count4 = *(uint*)(Echr_base + 0x460);//silence
            ImGui.Text("off_count: silence:  " + Eoff_count4);
            uint Eoff_count5 = *(uint*)(Echr_base + 0x464);//blind
            uint Eoff_count6 = *(uint*)(Echr_base + 0x468);//poison
            uint Eoff_count7 = *(uint*)(Echr_base + 0x46c);//confusion
            ImGui.Text("off_count: confuse:  " + Eoff_count7);
            uint Eoff_count8 = *(uint*)(Echr_base + 0x470);//berserk
            ImGui.Text("off_count: berserk:  " + Eoff_count8);
            ImGui.End();

        }

    }

    public nint h_get_chr_addr(uint chr_id) {
        return _get_chr_addr_handle.orig_fptr.Invoke(chr_id);
    }

    public override bool init(FhModContext mod_context, FileStream global_state_file) {
        _get_chr_addr_handle.hook();
        return true;
    }

    public override void load_local_state(FileStream? local_state_file, FhLocalStateInfo local_state_info) { }
    public override void save_local_state(FileStream local_state_file) { }
}
