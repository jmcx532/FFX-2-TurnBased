// SPDX-License-Identifier: MIT
/* This function sets the ATB at the start of battle
 * Sets ATB to 0 for pre-emptive strikes normally
 */


namespace Fahrenheit.Modules.FFX2TurnBased;

[FhLoad(FhGameId.FFX2)]
public unsafe class PreEmptiveModule : FhModule {
    /*
    //function delegates
    //618b80 -- checks DAT_DF94a5 - (is 1 on premptive)(2 on ambush, 0 normal) and processes
    //ALWAYS RUNS ON BATTLE START -- MsCalcFirstAttack
    //Returns the battle state number
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int MsCalcFirstAttack();
    //checks DAT_DF94a5 - this is 1 on pre-emptive strike (may be 0 normally and 2 on ambush? - need to check
    //parent function of preemptive_fx
    private static FhMethodHandle<MsCalcFirstAttack> _MsCalcFirstAttack =>
        new ( new FhMethodLocation("FFX-2.exe", 0x218B80) );

    //6348a0 - writes the ATB progress remaining when there is a pre-emptive strike (Maybe ambush too) (called by the previous function)
    //ONLY CALLED IF THERE IS PREEMPTIVE STRIKE / Ambush - not on normal start -- MsChrAtbReset
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int MsChrAtbReset(byte chr_id, int param_2);
    //actually writes the ATB value on preemptive strike - called by init_atb_progress
    private static FhMethodHandle<MsChrAtbReset> _MsChrAtbReset =>
        new ( new FhMethodLocation("FFX-2.exe", 0x2348A0) );

    //634730
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate uint MsChrAtbInit(int chr_base_address, int param_2, int param_3);
    //first strike handling - 634730
    private static FhMethodHandle<MsChrAtbInit> _MsChrAtbInit =>
        new ( new FhMethodLocation("FFX-2.exe", 0x234730) );

    //611450
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int MsGetChr(uint chr_id);
    //gets character base address
    private static FhMethodHandle<MsGetChr> _MsGetChr =>
        new ( new FhMethodLocation("FFX-2.exe", 0x211450) );


    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int MsGetComData(uint command_id, int* param_2);
    //used to get commands base address, can be used for auto abilites and Garment Grids maybe
    private static FhMethodHandle<MsGetComData> _MsGetComData =>
        new ( new FhMethodLocation("FFX-2.exe", 0x225160) );

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]// 61add0 -- MsGetRndChr
    public delegate int MsGetRndChr(int param_1, int param_2);
    private static FhMethodHandle<MsGetRndChr> _MsGetRndChr =>
        new ( new FhMethodLocation("FFX-2.exe", 0x21ADD0) );

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]// 61e290 -- brnd();
    public delegate uint brnd(int param_1);
    private static FhMethodHandle<brnd> _brnd =>
        new ( new FhMethodLocation("FFX-2.exe", 0x21E290) );

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]// 625bf0 -- MsGetRamChrMonster
    public delegate uint MsGetRamChrMonster(byte param_1);
    private static FhMethodHandle<MsGetRamChrMonster> _MsGetRamChrMonster =>
        new ( new FhMethodLocation("FFX-2.exe", 0x225BF0) );

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]// 613360 -- MsGetChrStatDeathStone
    public delegate uint MsGetChrStatDeathStone(uint param_1);
    private static FhMethodHandle<MsGetChrStatDeathStone> _MsGetChrStatDeathStone =>
        new ( new FhMethodLocation("FFX-2.exe", 0x213360) );
    */

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int MsGetComData(uint command_id, int* param_2);
    //used to get commands base address, can be used for auto abilites and Garment Grids maybe
    private static FhMethodHandle<MsGetComData> _MsGetComData =>
        new(new FhMethodLocation("FFX-2.exe", 0x225160));

    public PreEmptiveModule() { }


    /*this function gets the base address of a characters Battle data section
     * If it's parameter is less than 31 it returns a characters BattleData base address
     *Passed with 0 gets the first party members data, 1 the second and so on
     *If it's passed with a parameter greater than 155 it does end of battle cleanup I noticed from logging before
     */
    public Chr* h_MsGetChr(uint chr_id) {
        return FFX2.FhCall.MsGetChr.chain_from(h_MsGetChr).fnptr!(chr_id);
    }

    //this function returns the base address for commands -- AND OTHER EXCEL DATA - look through its sub-functions
    //param_1 is the command id (e.g 0x3002)
    public unsafe int h_MsGetComData(uint command_id, int* param_2) {
        return _MsGetComData.chain_from(h_MsGetComData).fnptr!(command_id, param_2);
    }

    public int h_MsGetRndChr(uint param_1, int param_2) {
        return FFX2.FhCall.MsGetRndChr.chain_from(h_MsGetRndChr).fnptr!(param_1, param_2);
    }

    /*
    public int h_brnd(int param_1) {
        return FhCall.brnd.chain_from(h_brnd).fnptr!(param_1);
    }*/

    public uint h_MsGetRamChrMonster(uint param_1) {
        return FFX2.FhCall.MsGetRamChrMonster.chain_from(h_MsGetRamChrMonster).fnptr!(param_1);
    }

    public uint h_MsGetChrStatDeathStone(uint param_1) {
        return FFX2.FhCall.MsGetChrStatDeathStone.chain_from(h_MsGetChrStatDeathStone).fnptr!(param_1);
    }

    //processes the initial battle state normal, preemptive
    //ALWAYS RUNS ON BATTLE START, rturns the battle state number (0/1/2, Normal, Pre, Ambush)
    public int h_MsCalcFirstAttack() {

        reenable_time_trip();// Re-enables Psychic's Time Trip command - can only use once per battle

        //update YRPs +ec2 state flag (normally increments when target hit) - used for counterattack handling and needs to be set at start of battle to avoid softlock
        //this is used for ally counter-attack handling to set wait mode until they've finished
        Chr* y_chr = h_MsGetChr(0);
        Chr* r_chr = h_MsGetChr(1);
        Chr* p_chr = h_MsGetChr(2);

        int y_addr = (int)y_chr;
        int r_addr = (int)r_chr;
        int p_addr = (int)p_chr;

        //write YRPs posion damage value to be 32 - damage is 32/256 of their HP (12.5%)
        //delayed slightly - FUN_00628820 writes this first, but hooking that function breaks the mod - YRP have no ATBS
        //and battle is stuck in Active Mode, but nothing happens.
        *(int*)(y_addr + 0x694) = 32;
        *(int*)(r_addr + 0x694) = 32;
        *(int*)(p_addr + 0x694) = 32;

        return FFX2.FhCall.MsCalcFirstAttack.chain_from(h_MsCalcFirstAttack).fnptr!();

    }

    //overwrites characters ATB time left
    //ONLY CALLED IF THERE IS PREEMPTIVE STRIKE (maybe ambush too, but not on normal start)
    public unsafe int h_MsChrAtbReset(uint chr_id, int param_2) {

        Chr* chr;
        int iVar1;
        int iVar2;
        int iVar3;
        int iVar4;
        int iVar5;

        chr = h_MsGetChr(chr_id);
        iVar1 = (int)chr;

        iVar5 = *(int*)(iVar1 + 0x9dc);
        iVar2 = h_MsGetRndChr(chr_id, 0);
        iVar3 = FhCall.brnd.fnptr!(iVar2);
        iVar3 = iVar3 & 0xf;
        if (param_2 == 0) {
            iVar4 = (int)h_MsGetRamChrMonster(chr_id);
            if (iVar4 == 0) { iVar3 = 0; }
            iVar4 = 0;
        }
        else {
            iVar4 = 0x71;
        }
        //iVar5 = (iVar4 + uVar3) * iVar5;
        iVar5 = (int)((iVar4 + (int)iVar3) * iVar5);

        iVar5 = (int)((iVar5 >> 0x1f & 0x7fU) + iVar5) >> 7;
        if ((*(byte*)(iVar1 + 0x650) & 1) == 0) {
            iVar4 = (int)h_MsGetChrStatDeathStone(chr_id);
            if (iVar4 == 0) {
                /*overwrite characters ATB time remaining as Chr_id + 1 if they have priority
                *YRP will end up with values of 3,4 5 - enemies will have ATB time values of 18, 19, 20
                *These values are low/high enough that enemies will get the first turn on Ambush, but First Strike will always outpace them.
                *Allies with First Strike get priority -> see next function (And First Strike always beats pre-emptive with offset of +3)
                */

                //*(int*)(iVar1 + 0x9d8) = iVar5;//original
                *(int*)(iVar1 + 0x9d8) = (int)(iVar5 + (chr_id + 3));
                return iVar5;
            }
        }
        return iVar5;
    }


    //Tb -handle case where multiple characters may have First Strike
    public int h_MsChrAtbInit(Chr* chr, int param_2, int param_3) {
        int original_result = FFX2.FhCall.MsChrAtbInit.chain_from(h_MsChrAtbInit).fnptr!(chr, param_2, param_3);

        //original result is this if chr_base_address is non-zero in original function
        if (original_result == 0xffffffff) {

            //if First Strike flag bit is set
            if ((*(byte*)((int)(chr) + 0x650) & 1) != 0) {
                //overwrite ATB Time left to be their character ID -> 0, 1 or 2 - If they haven't been KOed by the previous attack
                if ( (*(int*)((int)(chr) + 0x434) & 1) == 1) {
                    *(int*)((int)(chr) + 0x9d8) = *(byte*)((int)(chr) + 0xC);
                }
            }

            // When called from MsDamageCheckDeath which writes ATB on KO
            if (param_2 == 1 && param_3 == -1)
            {
                *(int*)((int)(chr) + 0x9dc) = (*(int*)((int)(chr) + 0x9dc)) / 2;
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
        return FFX2.FhCall.MsGetChr.hook(this, h_MsGetChr)
        && FFX2.FhCall.MsChrAtbReset.hook(this, h_MsChrAtbReset)
        && FFX2.FhCall.MsCalcFirstAttack.hook(this, h_MsCalcFirstAttack)
        && FFX2.FhCall.MsChrAtbInit.hook(this, h_MsChrAtbInit)
        && _MsGetComData.hook(this, h_MsGetComData)
        && FFX2.FhCall.MsGetRndChr.hook(this, h_MsGetRndChr)
        //&& FhCall.brnd.hook(this, h_brnd)
        && FFX2.FhCall.MsGetRamChrMonster.hook(this, h_MsGetRamChrMonster)
        && FFX2.FhCall.MsGetChrStatDeathStone.hook(this, h_MsGetChrStatDeathStone);

    }

    public override void load_local_state(FileStream? local_state_file, FhLocalStateInfo local_state_info) { }
    public override void save_local_state(FileStream local_state_file) { }
}
