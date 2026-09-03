// SPDX-License-Identifier: MIT

namespace Fahrenheit.Modules.FFX2TurnBased;

[FhLoad(FhGameId.FFX2)]
public class TurnBasedModule : FhModule {

    public TurnBasedModule() { }

    /*this function gets the base address of a characters Battle data section
     * If it's parameter is less than 31 it returns a characters BattleData base address
     *If it's passed with a parameter greater than 155 it does end of battle cleanup I noticed from logging before
     * Chr ids: Y: 0, R: 2, P: 3 -- enemies from 15 onward
     */
    public unsafe Chr* h_MsGetChr(uint chr_id)
    {
        return FFX2.FhCall.MsGetChr.chain_from(h_MsGetChr).fnptr!(chr_id);
    }


    public unsafe int h_MsSetATBwait(sbyte target_value) {

        //wait loop - counterattack handling
        for (uint i = 0; i < 31; i++) {
            Chr* chr = h_MsGetChr(i);
            int chr_base = (int)chr;
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

    public unsafe override bool init(FhModContext mod_context, FileStream global_state_file) {
        return FFX2.FhCall.MsSetATBwait.hook(this, h_MsSetATBwait) 
            && FFX2.FhCall.MsGetChr.hook(this, h_MsGetChr);
    }

    public override void load_local_state(FileStream? local_state_file, FhLocalStateInfo local_state_info) { }
    public override void save_local_state(FileStream local_state_file) { }
}
