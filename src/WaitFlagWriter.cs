// SPDX-License-Identifier: MIT

namespace Fahrenheit.Modules.FFX2TurnBased;

//function delegates
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public delegate uint MsSetATBwait(byte target_value);
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public delegate int MsGetChr(uint chr_id);

[FhLoad(FhGameId.FFX2)]
public class TurnBasedModule : FhModule {
    protected readonly FhLogger _wait_flag_logger;
    private readonly FhMethodHandle<MsSetATBwait>_MsSetATBwait_handle;
    private readonly FhMethodHandle<MsGetChr> _MsGetChr_handle;

    public TurnBasedModule() {
        int addr_offset = 0x400000;

        _wait_flag_logger = new FhLogger("WaitFlag_Writer_TurnBased.log");
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

        //wait loop
        for (uint i = 0; i < 31; i++) {
            nint chr_base = h_MsGetChr(i);
            byte is_active = *(byte*)(chr_base + 0x1784);
            byte state = *(byte*)(chr_base + 0xe68);
            byte num_targets_hit = *(byte*)(chr_base + 0xec2);

            //Are they KOed
            uint status = *(uint*)(chr_base + 0x434);
            bool isAlive = (status & 0x1) == 0;
            if (!isAlive)
                continue; // dead - go to next iteration/character in for loop

            //Attack / counter attack handling
            //for counter-attack handling, I had to monitor a certain flag, but it misbehaves if certain commands are used
            // and the wait flag is stuck because it isn't set correctly. This relates to the yrp_state checks below*
            //check for Escape, Scan, Teleport... ,dresspheres are for when SpecialDressphere dies

            var exceptionCommands = new HashSet<int> {0x3001,0x303D, 0x31EE,0x5001,0x5002,0x5003,0x5004,0x5005,0x5006,0x5007,0x5008,
                                                0x5009,0x500A,0x500B,0x500C,0x500D,0x500E,0x500F,0x5010,0x5011,0x5012,
                                                0x5013, 0x5014, 0x5015, 0x5016, 0x5017, 0x5018, 0x5019,0x501A,0x501B, 0x501C, 0x501D,
                                                0x501E, 0x501F, 0x5020, 0x5021};

            //get YRP (some state flag?) and the last command they performed- used for counterattack handling
            int y_addr = h_MsGetChr(0);
            byte y_tgts_hit = *(byte*)(y_addr + 0xec2);
            ushort y_last_command = *(ushort*)(y_addr + 0xf3c);

            int r_addr = h_MsGetChr(1);
            byte r_tgts_hit = *(byte*)(r_addr + 0xec2);
            ushort r_last_command = *(ushort*)(r_addr + 0xf3c);

            int p_addr = h_MsGetChr(2);
            byte p_tgts_hit = *(byte*)(p_addr + 0xec2);
            ushort p_last_command = *(ushort*)(p_addr + 0xf3c);

            if 
            (
            (y_tgts_hit == 0 && isAlive && !exceptionCommands.Contains(y_last_command)) ||
            (r_tgts_hit == 0 && isAlive && !exceptionCommands.Contains(r_last_command)) ||
            (p_tgts_hit == 0 && isAlive && !exceptionCommands.Contains(p_last_command))
            ) {
                //write submenu open wait flag (DAT_00DF8817)
                FhUtil.set_at<byte>(0x9F8817, 1);
                return 1;
            }
        }//end of loop


        //number of allies ready variable/flag (DAT_011B7480)
        int num_allies_ready = FhUtil.get_at<byte>((nint)0xDB7480);
        if (num_allies_ready != 0) {
            FhUtil.set_at<byte>(0x9F8817, 1);
            return 1;
        }
        
        //number of characters acting at 0xDF7903
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
        _MsSetATBwait_handle.hook();
        _MsGetChr_handle.hook();
        return true;
    }

    public override void load_local_state(FileStream? local_state_file, FhLocalStateInfo local_state_info) { }
    public override void save_local_state(FileStream local_state_file) { }
}
