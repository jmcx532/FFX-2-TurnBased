// SPDX-License-Identifier: MIT
/* This function sets the ATB at the start of battle
 * Sets ATB to 0 for pre-emptive strikes normally
 */


namespace Fahrenheit.Modules.FFX2TurnBased;

[FhLoad(FhGameId.FFX2)]
public unsafe class PreEmptiveModule : FhModule {

    //function delegates
    //618b80 -- checks DAT_DF94a5 - (is 1 on premptive)(Need to check if its 2 on ambush, 0 normal) and processes
    //ALWAYS RUNS ON BATTLE START -- MsCalcFirstAttack
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int MsCalcFirstAttack();

    //6348a0 - writes the ATB progress remaining when there is a pre-emptive strike (Maybe ambush too) (called by the previous function)
    //ONLY CALLED IF THERE IS PREEMPTIVE STRIKE / Ambush - not on normal start -- MsChrAtbReset
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int MsChrAtbReset(byte chr_id, int param_2);

    //634730
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate uint MsChrAtbInit(int chr_base_address, int param_2, int param_3);

    //611450
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int MsGetChr(uint chr_id);

    //625160 
    /* this function returns a base address for a command as far as the scope of ATB recovery time is concerned.
     * In reality, it checks a whole range of things, commands (item, command, monmagic), auto-abilities, Garment Grids*/
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public unsafe delegate int MsGetComData(uint command_id, int* param_2);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]// 61add0 -- MsGetRndChr
    public delegate int MsGetRndChr(int param_1, int param_2);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]// 61e290 -- brnd();
    public delegate uint brnd(int param_1);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]// 625bf0 -- MsGetRamChrMonster
    public delegate int MsGetRamChrMonster(byte param_1);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]// 613360 -- MsGetChrStatDeathStone
    public delegate int MsGetChrStatDeathStone(uint param_1);

    //actually writes the ATB value on preemptive strike - called by init_atb_progress
    private readonly FhMethodHandle<MsChrAtbReset> _MsChrAtbReset_handle;
    //checks DAT_DF94a5 - this is 1 on pre-emptive strike (may be 0 normally and 2 on ambush? - need to check
    //parent function of preemptive_fx
    private readonly FhMethodHandle<MsCalcFirstAttack> _MsCalcFirstAttack_handle;
    //first strike handling - 634730
    private readonly FhMethodHandle<MsChrAtbInit> _MsChrAtbInit_handle;
    //gets character base address
    private readonly FhMethodHandle<MsGetChr> _MsGetChr_handle;
    //used to get commands base address, can be used for auto abilites and Garment Grids maybe
    private readonly FhMethodHandle<MsGetComData> _MsGetComData_handle;
    private readonly FhMethodHandle<MsGetRndChr> _MsGetRndChr_handle;
    private readonly FhMethodHandle<brnd> _brnd_handle;
    private readonly FhMethodHandle<MsGetRamChrMonster> _MsGetRamChrMonster_handle;
    private readonly FhMethodHandle<MsGetChrStatDeathStone> _MsGetChrStatDeathStone_handle;

    public PreEmptiveModule() {
        int addr_offset = 0x400000;

        _MsCalcFirstAttack_handle = new FhMethodHandle<MsCalcFirstAttack>(this, "FFX-2.exe", 0x618b80 - addr_offset, h_MsCalcFirstAttack);
        _MsChrAtbReset_handle = new FhMethodHandle<MsChrAtbReset>(this, "FFX-2.exe", 0x6348a0 - addr_offset, h_MsChrAtbReset);
        _MsChrAtbInit_handle = new FhMethodHandle<MsChrAtbInit>(this, "FFX-2.exe", 0x634730 - addr_offset, h_MsChrAtbInit);

        _MsGetChr_handle = new FhMethodHandle<MsGetChr>(this, "FFX-2.exe", 0x611450 - addr_offset, h_MsGetChr);
        _MsGetComData_handle = new FhMethodHandle<MsGetComData>(this, "FFX-2.exe", 0x625160 - addr_offset, h_MsGetComData);


        //MsChrAtbReset sub-functions
        _MsGetRndChr_handle = new FhMethodHandle<MsGetRndChr>(this, "FFX-2.exe", 0x61add0 - addr_offset, h_MsGetRndChr);
        _brnd_handle = new FhMethodHandle<brnd>(this, "FFX-2.exe", 0x61e290 - addr_offset, h_brnd);
        _MsGetRamChrMonster_handle = new FhMethodHandle<MsGetRamChrMonster>(this, "FFX-2.exe", 0x625bf0 - addr_offset, h_MsGetRamChrMonster);
        _MsGetChrStatDeathStone_handle = new FhMethodHandle<MsGetChrStatDeathStone>(this, "FFX-2.exe", 0x613360 - addr_offset, h_MsGetChrStatDeathStone);
    }


    /*this function gets the base address of a characters Battle data section
     * If it's parameter is less than 31 it returns a characters BattleData base address
     *Passed with 0 gets the first party members data, 1 the second and so on
     *If it's passed with a parameter greater than 155 it does end of battle cleanup I noticed from logging before
     */
    public int h_MsGetChr(uint chr_id) {
        return _MsGetChr_handle.orig_fptr.Invoke(chr_id);
    }

    //this function returns the base address for commands -- AND OTHER EXCEL DATA - look through its sub-functions
    //param_1 is the command id (e.g 0x3002)
    public unsafe int h_MsGetComData(uint command_id, int* param_2) {
        return _MsGetComData_handle.orig_fptr.Invoke(command_id, param_2);
    }

    public int h_MsGetRndChr(int param_1, int param_2) {
        return _MsGetRndChr_handle.orig_fptr.Invoke(param_1, param_2);
    }

    public uint h_brnd(int param_1) {
        return _brnd_handle.orig_fptr.Invoke(param_1);
    }

    public int h_MsGetRamChrMonster(byte param_1) {
        return _MsGetRamChrMonster_handle.orig_fptr.Invoke(param_1);
    }

    public int h_MsGetChrStatDeathStone(uint param_1) {
        return _MsGetChrStatDeathStone_handle.orig_fptr.Invoke(param_1);
    }

    //processes the initial battle state normal, preemptive
    //ALWAYS RUNS ON BATTLE START
    public unsafe int h_MsCalcFirstAttack() {

        reenable_time_trip();// Re-enables Psychic's Time Trip command - can only use once per battle

        //update YRPs +ec2 state flag (normally increments when target hit) - used for counterattack handling and needs to be set at start of battle to avoid softlock
        //this is used for ally counter-attack handling to set wait mode until they've finished
        int y_addr = h_MsGetChr(0);
        int r_addr = h_MsGetChr(1);
        int p_addr = h_MsGetChr(2);

        //write YRPs posion damage value to be 32 - damage is 32/256 of their HP (12.5%)
        //delayed slightly - FUN_00628820 writes this first, but hooking that function breaks the mod - YRP have no ATBS
        //and battle is stuck in Active Mode, but nothing happens.
        *(int*)(y_addr + 0x694) = 32;
        *(int*)(r_addr + 0x694) = 32;
        *(int*)(p_addr + 0x694) = 32;

        return _MsCalcFirstAttack_handle.orig_fptr.Invoke();
    }

    //overwrites characters ATB time left
    //ONLY CALLED IF THERE IS PREEMPTIVE STRIKE (maybe ambush too, but not on normal start)
    public unsafe int h_MsChrAtbReset(byte chr_id, int param_2) {


        int iVar1;
        int iVar2;
        uint uVar3;
        int iVar4;
        int iVar5;

        iVar1 = h_MsGetChr(chr_id);
        iVar5 = *(int*)(iVar1 + 0x9dc);
        iVar2 = h_MsGetRndChr(chr_id, 0);
        uVar3 = h_brnd(iVar2);
        uVar3 = uVar3 & 0xf;
        if (param_2 == 0) {
            iVar4 = h_MsGetRamChrMonster(chr_id);
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
            iVar4 = h_MsGetChrStatDeathStone(chr_id);
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


    //Tb -handle case where multiple characters may have First Strike
    public uint h_MsChrAtbInit(int chr_base_address, int param_2, int param_3) {
        uint original_result = _MsChrAtbInit_handle.orig_fptr.Invoke(chr_base_address, param_2, param_3);

        //original result is this if chr_base_address is non-zero in original function
        if (original_result == 0xffffffff) {
            //if First Strike flag bit is set
            if ((*(byte*)(chr_base_address + 0x650) & 1) != 0) {
                //overwrite ATB Time left to be their character ID -> 0, 1 or 2 - If they haven't been KOed by the previous attack
                if ( (*(int*)(chr_base_address + 0x434) & 1) == 1) {
                    *(int*)(chr_base_address + 0x9d8) = *(byte*)(chr_base_address + 0xC);
                }
            }
        }
        
        return original_result;
    }

    //function to re-enable Psychics Time Trip command
    public void reenable_time_trip() {
        
        //get the commands data 
        int tt_exp_data = h_MsGetComData(0x31ea, (int*)(0));
        int tt_mp_cost = h_MsGetComData(0x31ea, (int*)(0));
        

        if ((tt_exp_data & (1 << 28)) != 0) {
            //set its com_dark_flag to false - this makes it drain MP again not HP
            *(int*)(tt_exp_data + 0x14) &= ~(1 << 28);
            //Restore MP cost to default
            *(byte*)(tt_mp_cost + 0x26) = 20;
        }
   
     }


    public override bool init(FhModContext mod_context, FileStream global_state_file) {
        return _MsGetChr_handle.hook()
        && _MsChrAtbReset_handle.hook()
        && _MsCalcFirstAttack_handle.hook()
        && _MsChrAtbInit_handle.hook()
        && _MsGetComData_handle.hook()
        && _MsGetRndChr_handle.hook()
        && _brnd_handle.hook()
        && _MsGetRamChrMonster_handle.hook()
        && _MsGetChrStatDeathStone_handle.hook();

    }

    public override void load_local_state(FileStream? local_state_file, FhLocalStateInfo local_state_info) { }
    public override void save_local_state(FileStream local_state_file) { }
}
