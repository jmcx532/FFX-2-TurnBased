// SPDX-License-Identifier: MIT

namespace Fahrenheit.Modules.FFX2TurnBased;

[FhLoad(FhGameId.FFX2)]
public class RemoveATBGauges : FhModule {

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    //756590 - TOBtlDrawATBGaude - NOT a typo
    public delegate void TOBtlDrawATBGaude(int param_1, int param_2, int param_3);

    private readonly FhMethodHandle<TOBtlDrawATBGaude> _TOBtlDrawATBGaude_handle;

    public RemoveATBGauges() {

        int addr_offset = 0x400000;
        //_SClogger = new FhLogger($"{FhUtil.get_timestamp_string()}_SpherechangeATBDelay.log");

        _TOBtlDrawATBGaude_handle = new FhMethodHandle<TOBtlDrawATBGaude>(this, "FFX-2.exe", 0x756590 - addr_offset, h_TOBtlDrawATBGaude);
        
    }

    public void h_TOBtlDrawATBGaude(int param_1, int param_2, int param_3) {
        return;
    }
    

    public override bool init(FhModContext mod_context, FileStream global_state_file) {
        _TOBtlDrawATBGaude_handle.hook();
        return true;
    }

    public override void load_local_state(FileStream? local_state_file, FhLocalStateInfo local_state_info) { }
    public override void save_local_state(FileStream local_state_file) { }
}
