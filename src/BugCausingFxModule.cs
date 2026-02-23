// SPDX-License-Identifier: MIT
/*
 * A generic module that handles functions that cause bugs in a Turn-Based environment.
 * 
 * alpha_fx deals with some extra timer/accumulator that prevents them transitioning from:
 * (chr_base + 0xe68) -> State 2
 * Enemies get stuck in this state for a period of time during which they may get stuck if wait mode is forced or
 * Active mode is enabled for a period, which makes timed statuses tick down quickly, makes multiple regen/poison ticks occur
 * 
 */

namespace Fahrenheit.Modules.BugCausersModule;

[FhLoad(FhGameId.FFX2)]
public unsafe class BugCausersModule : FhModule {
    protected readonly FhLogger _bugcausers_logger;

    //6341a0
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int alpha_fx(uint chr_id);
    private readonly FhMethodHandle<alpha_fx> _alpha_fx_handle;

    public BugCausersModule() {
        int addr_offset = 0x400000;
        _bugcausers_logger = new FhLogger($"TurnBased_BugCausers.log");

        _alpha_fx_handle = new FhMethodHandle<alpha_fx>(this, "FFX-2.exe", 0x6341a0 - addr_offset, h_alpha_fx);
    }

    public int h_alpha_fx(uint chr_id) {
        int original_result = _alpha_fx_handle.orig_fptr.Invoke(chr_id);

        //return 0 instead
        return 0;
    }


    public override bool init(FhModContext mod_context, FileStream global_state_file) {
        _alpha_fx_handle.hook();

        return true;
    }

    public override void load_local_state(FileStream? local_state_file, FhLocalStateInfo local_state_info) { }
    public override void save_local_state(FileStream local_state_file) { }
}

