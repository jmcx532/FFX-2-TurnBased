// SPDX-License-Identifier: MIT

namespace Fahrenheit.Modules.FFX2TurnBased;

//function delegates
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public delegate uint MsSetATBwait(byte target_value);
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public delegate int MsGetChr(uint chr_id);

[FhLoad(FhGameId.FFX2)]
public class TurnBasedModule : FhModule {
    
    private readonly FhMethodHandle<MsSetATBwait>_MsSetATBwait_handle;
    private readonly FhMethodHandle<MsGetChr> _MsGetChr_handle;

    public TurnBasedModule() {
        int addr_offset = 0x400000;

        
        _MsSetATBwait_handle = new FhMethodHandle<MsSetATBwait>(this, "FFX-2.exe", 0x634ae0 - addr_offset, h_MsSetATBwait);
        _MsGetChr_handle = new FhMethodHandle<MsGetChr>(this, "FFX-2.exe", 0x611450 - addr_offset, h_MsGetChr);
    }

    /*this function gets the base address of a characters Battle data section
     * If it's parameter is less than 31 it returns a characters BattleData base address
     *If it's passed with a parameter greater than 155 it does end of battle cleanup I noticed from logging before
     * Chr ids: Y: 0, R: 2, P: 3 -- enemies from 15 onward
     */
    public int h_MsGetChr(uint chr_id) {
        return _MsGetChr_handle.orig_fptr.Invoke(chr_id);
    }

    
    public unsafe uint h_MsSetATBwait(byte target_value) {

        //wait loop - counterattack handling
        for (uint i = 0; i < 31; i++) {
            nint chr_base = h_MsGetChr(i);
            byte is_countering = *(byte*)(chr_base + 0xe6c);
            
            if(is_countering == 1)
            {
                FhUtil.set_at<byte>(0x9F8817, 1);
                return 1;
            }
            
        }//end of loop

        //number of allies ready variable/flag (DAT_011B7480) - menu open handling
        int num_allies_ready = FhUtil.get_at<byte>((nint)0xDB7480);
        if (num_allies_ready != 0) {
            FhUtil.set_at<byte>(0x9F8817, 1);
            return 1;
        }
        
        //number of characters acting at 0xDF7903 - Attack handling (magic commands have com_share flag set)
        int num_characters_acting = FhUtil.get_at<byte>((nint)0x9F7903);
        if (num_characters_acting != 0) {
            FhUtil.set_at<byte>(0x9F8817, 1);
            return 1;
        }

        //unset wait flag
        FhUtil.set_at<byte>(0x9F8817, 0);
        return 1;
    }

    public override bool init(FhModContext mod_context, FileStream global_state_file) {
        return _MsSetATBwait_handle.hook() && _MsGetChr_handle.hook();
    }

    public override void load_local_state(FileStream? local_state_file, FhLocalStateInfo local_state_info) { }
    public override void save_local_state(FileStream local_state_file) { }
}
