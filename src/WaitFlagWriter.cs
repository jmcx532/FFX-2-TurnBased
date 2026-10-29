// SPDX-License-Identifier: MIT

namespace Fahrenheit.Modules.FFX2TurnBased;

[FhLoad(FhGameId.FFX2)]
public class TurnBasedModule : FhModule {

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void d_TOMkpShape2dBtl(int param_1, int param_2, uint param_3, uint param_4);
    public static FhMethodHandle<d_TOMkpShape2dBtl> TOMkpShape2dBtl =>
        new(new FhMethodLocation("FFX-2.exe", 0x3B0f20));

    public TurnBasedModule() { }

    // Hide Active/Wait mode balloon???? Can't remember
    public void h_TOMkpShape2dBtl(int param_1, int param_2, uint param_3, uint param_4)
    {

#if !DEBUG
        if (param_3 == 0 || param_3 == 1)
        {
            return;
        }
        else
        {
            TOMkpShape2dBtl.chain_from(h_TOMkpShape2dBtl).fnptr!(param_1, param_2, param_3, param_4);
        }
#endif

        TOMkpShape2dBtl.chain_from(h_TOMkpShape2dBtl).fnptr!(param_1, param_2, param_3, param_4);
    }

    /// <summary>
    /// Sets Wait Mode Flag when certain conditions are met.
    /// </summary>
    /// <param name="target_value"></param>
    /// <returns></returns>
    public unsafe int h_MsSetATBwait(sbyte target_value) {
        
        //wait loop - counterattack handling
        for (uint i = 0; i < 31; i++) {
            Chr* chr = FFX2.FhCall.MsGetChr.fnptr!(i);
            int chr_base = (int)chr;
            byte is_countering = *(byte*)(chr_base + 0xe6c);
            
            if(is_countering == 1)
            {
                FhUtil.set_at<byte>(0x9F7817, 1);
                return 1;
            }
            
        }//end of loop

        // menu open handling - Number of allies ready variable/flag (DAT_011B6480) -
        int num_allies_ready = FhUtil.get_at<byte>((nint)0xDB6480);
        if (num_allies_ready != 0) {
            FhUtil.set_at<byte>(0x9F7817, 1);
            return 1;
        }

        // Attack handling - Number of characters acting at 0xDF6903 (magic commands have com_share flag set)
        int num_characters_acting = FhUtil.get_at<byte>((nint)0x9F6903);
        if (num_characters_acting != 0) {
            FhUtil.set_at<byte>(0x9F7817, 1);
            return 1;
        }

        //unset wait flag
        FhUtil.set_at<byte>(0x9F7817, 0);
        return 1;
    }

    public override bool init(FhModContext mod_context, FileStream global_state_file) {
        return FFX2.FhCall.MsSetATBwait.hook(this, h_MsSetATBwait)
            && TOMkpShape2dBtl.hook(this, h_TOMkpShape2dBtl);
    }

    public override void load_local_state(FileStream? local_state_file, FhLocalStateInfo local_state_info) { }
    public override void save_local_state(FileStream local_state_file) { }
}
