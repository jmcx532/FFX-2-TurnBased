// SPDX-License-Identifier: MIT
/* This function sets the ATB at the start of battle
 * Sets ATB to 0 for pre-emptive strikes normally
 */

namespace Fahrenheit.Modules.FFX2TurnBased;

[FhLoad(FhGameId.FFX2)]
public unsafe class PreEmptiveModule : FhModule {

    public const byte TIME_TRIP_MP_COST = 10;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int MsGetComData(uint command_id, int* param_2);
    //used to get commands base address, can be used for auto abilites and Garment Grids maybe
    private static FhMethodHandle<MsGetComData> _MsGetComData =>
        new(new FhMethodLocation("FFX-2.exe", 0x225130));

    public PreEmptiveModule() { }
        
    //this function returns the base address for commands -- AND OTHER EXCEL DATA - look through its sub-functions
    /*param_1 is the command id (e.g 0x3002)
    public unsafe int h_MsGetComData(uint command_id, int* param_2) {
        return _MsGetComData.chain_from(h_MsGetComData).fnptr!(command_id, param_2);
    }*/

    //processes the initial battle state normal, preemptive
    //ALWAYS RUNS ON BATTLE START, rturns the battle state number (0/1/2, Normal, Pre, Ambush)
    public int h_MsCalcFirstAttack() {

        reenable_time_trip();// Re-enables Psychic's Time Trip command - can only use once per battle

        //update YRPs +ec2 state flag (normally increments when target hit) - used for counterattack handling and needs to be set at start of battle to avoid softlock
        //this is used for ally counter-attack handling to set wait mode until they've finished
        Chr* y_chr = FFX2.FhCall.MsGetChr.fnptr!(0);
        Chr* r_chr = FFX2.FhCall.MsGetChr.fnptr!(1);
        Chr* p_chr = FFX2.FhCall.MsGetChr.fnptr!(2);

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

        chr = FFX2.FhCall.MsGetChr.fnptr!(chr_id);
        iVar1 = (int)chr;

        iVar5 = *(int*)(iVar1 + 0x9dc);
        iVar2 = FFX2.FhCall.MsGetRndChr.fnptr!(chr_id, 0);
        iVar3 = FhCall.brnd.fnptr!(iVar2);
        iVar3 = iVar3 & 0xf;
        if (param_2 == 0) {
            iVar4 = (int)FFX2.FhCall.MsGetRamChrMonster.fnptr!(chr_id);
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
            iVar4 = (int)FFX2.FhCall.MsGetChrStatDeathStone.fnptr!(chr_id);
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
        if (original_result == -1) {

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
                
                *(int*)((int)(chr) + 0x9dc) = (*(int*)((int)(chr) + 0x9dc)) / 2;// Length
                *(int*)((int)(chr) + 0x9d8) = (*(int*)((int)(chr) + 0x9dc)); // Remaining
            }

        }
        
        return original_result;
    }

    //function to re-enable Psychics Time Trip command
    public void reenable_time_trip() {
        
        //get the commands data 
        int tt_exp_data = _MsGetComData.fnptr!(0x31ea, (int*)(0));
        int tt_mp_cost = _MsGetComData.fnptr!(0x31ea, (int*)(0));
        

        if ((tt_exp_data & (1 << 28)) != 0) {
            //set its com_dark_flag to false - this makes it drain MP again not HP
            *(int*)(tt_exp_data + 0x14) &= ~(1 << 28);
            //Restore MP cost to default
            *(byte*)(tt_mp_cost + 0x26) = TIME_TRIP_MP_COST;
        }
   
     }


    public override bool init(FhModContext mod_context, FileStream global_state_file) {
        return FFX2.FhCall.MsChrAtbReset.hook(this, h_MsChrAtbReset)
        && FFX2.FhCall.MsCalcFirstAttack.hook(this, h_MsCalcFirstAttack)
        && FFX2.FhCall.MsChrAtbInit.hook(this, h_MsChrAtbInit);
        //&& _MsGetComData.hook(this, h_MsGetComData);
        //&& FhCall.brnd.hook(this, h_brnd)

    }

    public override void load_local_state(FileStream? local_state_file, FhLocalStateInfo local_state_info) { }
    public override void save_local_state(FileStream local_state_file) { }
}
