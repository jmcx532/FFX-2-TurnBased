// SPDX-License-Identifier: MIT

namespace Fahrenheit.Modules.TBUnstuckModule;

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public delegate nint get_chr_addr(uint chr_id);



[FhLoad(FhGameId.FFX2)]
public unsafe class UnstuckModule : FhModule {
    //protected readonly FhLogger _logger;
    private readonly FhMethodHandle<get_chr_addr> _get_chr_addr_handle;

    public UnstuckModule() {
;       int addr_offset = 0x400000;
        _get_chr_addr_handle = new FhMethodHandle<get_chr_addr>(this, "FFX-2.exe", 0x611450 - addr_offset, h_get_chr_addr);
    }


    public override void render_imgui() {
        base.render_imgui();

        int* chr_base_address = FhUtil.ptr_at<int>(0xA0FBAC);
        if (*chr_base_address != 0) {

            ImGui.Begin(
                "Get Unstuck",
                ImGuiWindowFlags.NoFocusOnAppearing
                );

            for (int i = 0; i < 3; i++) {
                if (ImGui.Button("Unstick Chr" + i)) {
                    nint chr_base_addr = h_get_chr_addr((uint)i);
                    *(byte*)(chr_base_addr + 0xEC2) = 1;
                }
                nint cb2 = h_get_chr_addr((uint)i);
                byte flag_value = *(byte*)(cb2 + 0xEC2);
                ImGui.SameLine();
                ImGui.Text("Flag value is: " + flag_value);
            }
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
