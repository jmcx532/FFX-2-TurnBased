// SPDX-License-Identifier: MIT
/* This function sets the ATB at the start of battle
 * Sets ATB to 0 for pre-emptive strikes normally
 */

namespace Fahrenheit.Modules.FFX2TurnBased;

[FhLoad(FhGameId.FFX2)]
public unsafe class PreEmptiveModule : FhModule {
    protected readonly FhLogger _logger;

    //function delegates
    //618b80 -- checks DAT_DF94a5 - (is 1 on premptive)(Need to check if its 2 on ambush, 0 normal) and processes
    //ALWAYS RUNS ON BATTLE START
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int process_btl_init_state();

    //6348a0 - writes the ATB progress remaining when there is a pre-emptive strike (Maybe ambush too) (called by the previous function)
    //ONLY CALLED IF THERE IS PREEMPTIVE STRIKE / Ambush - not on normal start
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int preemptive_atb_writer(byte chr_id, int param_2);

    //634730
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate uint first_strike(int chr_base_address, int param_2, int param_3);

    //611450
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int get_chr_addr(uint chr_id);

    //625160 
    /* this function returns a base address for a command as far as the scope of ATB recovery time is concerned.
     * In reality, it checks a whole range of things, commands (item, command, monmagic), auto-abilities, Garment Grids*/
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public unsafe delegate int get_cmd_addr(uint command_id, int* param_2);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int alpha_fx(int param_1, int param_2);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate uint bravo_fx(int param_1);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int charlie_fx(byte param_1);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int delta_fx(uint param_1);

    //actually writes the ATB value on preemptive strike - called by init_atb_progress
    private readonly FhMethodHandle<preemptive_atb_writer> _preemptive_handle;
    //checks DAT_DF94a5 - this is 1 on pre-emptive strike (may be 0 normally and 2 on ambush? - need to check
    //parent function of preemptive_fx
    private readonly FhMethodHandle<process_btl_init_state> _btl_init_state_handle;
    //first strike handling - 634730
    private readonly FhMethodHandle<first_strike> _first_strike_handle;
    //gets character base address
    private readonly FhMethodHandle<get_chr_addr> _get_chr_addr;
    //used to get commands base address, can be used for auto abilites and Garment Grids maybe
    private readonly FhMethodHandle<get_cmd_addr> _get_cmd_addr_handle;
    private readonly FhMethodHandle<alpha_fx> _alpha_fx_handle;
    private readonly FhMethodHandle<bravo_fx> _bravo_fx_handle;
    private readonly FhMethodHandle<charlie_fx> _charlie_fx_handle;
    private readonly FhMethodHandle<delta_fx> _delta_fx_handle;

    public PreEmptiveModule() {
        int addr_offset = 0x400000;


        _logger = new FhLogger($"TurnBased_ATB_init_handler.log");

        _btl_init_state_handle = new FhMethodHandle<process_btl_init_state>(this, "FFX-2.exe", 0x618b80 - addr_offset, h_init_btl_state);
        _preemptive_handle = new FhMethodHandle<preemptive_atb_writer>(this, "FFX-2.exe", 0x6348a0 - addr_offset, h_preemptive_init);
        _first_strike_handle = new FhMethodHandle<first_strike>(this, "FFX-2.exe", 0x634730 - addr_offset, h_first_strike);

        _get_chr_addr = new FhMethodHandle<get_chr_addr>(this, "FFX-2.exe", 0x611450 - addr_offset, h_get_chr_addr);
        _get_cmd_addr_handle = new FhMethodHandle<get_cmd_addr>(this, "FFX-2.exe", 0x625160 - addr_offset, h_get_cmd_addr);

        _alpha_fx_handle = new FhMethodHandle<alpha_fx>(this, "FFX-2.exe", 0x61add0 - addr_offset, h_alpha_fx);
        _bravo_fx_handle = new FhMethodHandle<bravo_fx>(this, "FFX-2.exe", 0x61e290 - addr_offset, h_bravo_fx);
        _charlie_fx_handle = new FhMethodHandle<charlie_fx>(this, "FFX-2.exe", 0x625bf0 - addr_offset, h_charlie_fx);
        _delta_fx_handle = new FhMethodHandle<delta_fx>(this, "FFX-2.exe", 0x613360 - addr_offset, h_delta_fx);
    }


    /*this function gets the base address of a characters Battle data section
     * If it's parameter is less than 31 it returns a characters BattleData base address
     *Passed with 0 gets the first party members data, 1 the second and so on
     *If it's passed with a parameter greater than 155 it does end of battle cleanup I noticed from logging before
     */
    public int h_get_chr_addr(uint chr_id) {
        return _get_chr_addr.orig_fptr.Invoke(chr_id);
    }

    //this function returns the base address for commands -- AND OTHER EXCEL DATA - look through its sub-functions
    //param_1 is the command id (e.g 0x3002)
    public unsafe int h_get_cmd_addr(uint command_id, int* param_2) {
        //_logger.Info("GET_CMD_ADDR PARAM_1 is:" + param_1.ToString("X"));
        return _get_cmd_addr_handle.orig_fptr.Invoke(command_id, param_2);
    }

    public int h_alpha_fx(int param_1, int param_2) {
        return _alpha_fx_handle.orig_fptr.Invoke(param_1, param_2);
    }

    public uint h_bravo_fx(int param_1) {
        return _bravo_fx_handle.orig_fptr.Invoke(param_1);
    }

    public int h_charlie_fx(byte param_1) {
        return _charlie_fx_handle.orig_fptr.Invoke(param_1);
    }

    public int h_delta_fx(uint param_1) {
        return _delta_fx_handle.orig_fptr.Invoke(param_1);
    }

    //processes the initial battle state normal, preemptive
    //ALWAYS RUNS ON BATTLE START
    public unsafe int h_init_btl_state() {

        _logger.Info("h_init_btl_state function called");
        //re-enable Psychics Time Trip command - can only use once per battle
        reenable_time_trip();

        //update YRPs +ec2 state flag (normally increments when target hit) - used for counterattack handling and needs to be set at start of battle to avoid softlock
        //this is used for ally counter-attack handling to set wait mode until they've finished
        int y_addr = h_get_chr_addr(0);
        int r_addr = h_get_chr_addr(1);
        int p_addr = h_get_chr_addr(2);

        *(byte*)(y_addr + 0xec2) = 1;
        *(byte*)(r_addr + 0xec2) = 1;
        *(byte*)(p_addr + 0xec2) = 1;

        //write YRPs posion damage value to be 32 - damage is 32/256 of their HP (12.5%)
        //delayed slightly - FUN_00628820 writes this first, but hooking that function breaks the mod - YRP have no ATBS
        //and battle is stuck in Active Mode, but nothing happens.
        *(int*)(y_addr + 0x694) = 32;
        *(int*)(r_addr + 0x694) = 32;
        *(int*)(p_addr + 0x694) = 32;

        return _btl_init_state_handle.orig_fptr.Invoke();
    }

    //overwrites characters ATB time left
    //ONLY CALLED IF THERE IS PREEMPTIVE STRIKE (maybe ambush too, but not on normal start)
    public unsafe int h_preemptive_init(byte chr_id, int param_2) {


        int iVar1;
        int iVar2;
        uint uVar3;
        int iVar4;
        int iVar5;

        iVar1 = h_get_chr_addr(chr_id);
        iVar5 = *(int*)(iVar1 + 0x9dc);
        iVar2 = h_alpha_fx(chr_id, 0);
        uVar3 = h_bravo_fx(iVar2);
        uVar3 = uVar3 & 0xf;
        if (param_2 == 0) {
            iVar4 = h_charlie_fx(chr_id);
            if (iVar4 == 0) { uVar3 = 0; }
            iVar4 = 0;
        }
        else {
            iVar4 = 0x71;
        }
        //iVar5 = (iVar4 + uVar3) * iVar5;
        iVar5 = (int)((iVar4 + (int)uVar3) * iVar5);

        iVar5 = (int)((iVar5 >> 0x1f & 0x7fU) + iVar5) >> 7;
        if ((*(byte*)(iVar1 + 0x650) & 1) == 0) {
            iVar4 = h_delta_fx(chr_id);
            if (iVar4 == 0) {
                /*overwrite characters ATB time remaining as Chr_id + 1 if they have priority
                *YRP will end up with values of 3,4 5 - enemies will have ATB time values of 18, 19, 20
                *These values are low/high enough that enemies will get the first turn on Ambush, but First Strike will always outpace them.
                *Allies with First Strike get priority -> see next function (And First Strike always beats pre-emptive with offset of +3)
                */

                //*(int*)(iVar1 + 0x9d8) = iVar5;//original
                *(int*)(iVar1 + 0x9d8) = iVar5 + (chr_id + 3);
                return iVar5;
            }
        }
        return iVar5;
    }


    //handle case where multiple characters may have First Strike - TEST AMBUSH - DOES FIRST STRIKE CORRECTLY GIVE PRIORITY?
    public uint h_first_strike(int chr_base_address, int param_2, int param_3) {
        uint original_result = _first_strike_handle.orig_fptr.Invoke(chr_base_address, param_2, param_3);

        //original result is this if chr_base_address is non-zero in original function
        if (original_result == 0xffffffff) {
            //if First Strike flag bit is set
            if ((*(byte*)(chr_base_address + 0x650) & 1) != 0) {
                //overwrite ATB Time left to be their character ID -> 0, 1 or 2
                *(int*)(chr_base_address + 0x9d8) = *(byte*)(chr_base_address + 0xC);
            }
        }
        
        return original_result;
    }

    //function to re-enable Psychics Time Trip command
    public void reenable_time_trip() {

        _logger.Info("Re-enable Time Trip Function");
        
        //get the commands data 
        int tt_exp_data = h_get_cmd_addr(0x31ea, (int*)(0));
        int tt_mp_cost = h_get_cmd_addr(0x31ea, (int*)(0));
        

        if ((tt_exp_data & (1 << 28)) != 0) {
            //set its com_dark_flag to false - this makes it drain MP again not HP
            *(int*)(tt_exp_data + 0x14) &= ~(1 << 28);
            //Restore MP cost to default
            *(byte*)(tt_mp_cost + 0x26) = 20;
        }
   
     }

    
    




    public override bool init(FhModContext mod_context, FileStream global_state_file) {
        _get_chr_addr.hook();
        _preemptive_handle.hook();
        _btl_init_state_handle.hook();
        _first_strike_handle.hook();

        _get_cmd_addr_handle.hook();
        _alpha_fx_handle.hook();
        _bravo_fx_handle.hook();
        _charlie_fx_handle.hook();
        _delta_fx_handle.hook();
        return true;
    }

    public override void load_local_state(FileStream? local_state_file, FhLocalStateInfo local_state_info) { }
    public override void save_local_state(FileStream local_state_file) { }
}
