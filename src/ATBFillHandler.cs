// SPDX-License-Identifier: MIT

/* This module replaces vanilla ATB fill behaviour with a turn based approach:
 * calculating the lowest atb_remaining value between applicable characters and
 * deduct that value from everybodies atb_remaining.
 * 
 * Pairs up with ATBFillStatusHandler.cs (partial class)
 * Status handling is triggered on ATB Full via a custom function called inside the
 * ATB Fill function - but defined in ATBFillStatusHandler.cs
 * 
 */


namespace Fahrenheit.Modules.FFX2TurnBased;

[FhLoad(FhGameId.FFX2)]
public unsafe partial class ATBFillModule : FhModule {
    //offset address
    int addr_offset = 0x400000;

    protected readonly FhLogger _fill_logger;

    //delegates
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    /*634b40 - atb function and parameters - this handles ATB timer counting down and more -> msChrATBprocess - lowercase m variant
     * There is a second MsChrATBprocess with a capital first letter */
    public unsafe delegate int atb_fx(byte chr_id, int chr_base_address, int* param_3, int param_4);
    //Sub-Functions
    //644bb0 - MsMagicCheckCommandExe
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public unsafe delegate int MsMagicCheckCommandExe(int* param_1, uint param_2, int* param_3, int* param_4);

    /*634b00 - this function checks some character values including their ATB tick down speed
    *pseudo checks for Stop (i.e their tick down value is 0 but Sleep/Petrify/Stop are better covered
    in the ChrAtbSpeedHandler*/
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int bravo_fx(int param_1);

    //61c290 - MsCheckMonsterOversoul
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int MsCheckMonsterOversoul(uint param_1);

    //636900 -  Warriors Sentinel related - MsResetDefenseStatus
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int MsResetDefenseStatus(byte param_1);

    //636400
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int MsClearDanceStatusMotion(byte param_1);
    //634390 - checks if Control Creatures and Control Enemy debug flags are set - MsGetRamChrMonster
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int MsGetRamChrMonster(byte param_1);
    //636360 - MsCheckDanceStatus
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int MsCheckDanceStatus(byte param_1);

    //MsGetChr FUN_00611450
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int MsGetChr(uint chr_id);

    //main function
    private readonly FhMethodHandle<atb_fx> _atb_fx_handle;
    //sub-functions
    private readonly FhMethodHandle<MsMagicCheckCommandExe> _MsMagicCheckCommandExe_handle;
    private readonly FhMethodHandle<bravo_fx> _bravo_fx_handle;
    private readonly FhMethodHandle<MsCheckMonsterOversoul> _MsCheckMonsterOversoul_handle;
    private readonly FhMethodHandle<MsResetDefenseStatus> _MsResetDefenseStatus_handle;
    private readonly FhMethodHandle<MsClearDanceStatusMotion> _MsClearDanceStatusMotion_handle;
    private readonly FhMethodHandle<MsGetRamChrMonster> _MsGetRamChrMonster_handle;
    private readonly FhMethodHandle<MsCheckDanceStatus> _MsCheckDanceStatus_handle;
    private readonly FhMethodHandle<MsGetChr> _MsGetChr_handle;

    public ATBFillModule() {

        _fill_logger = new FhLogger("TurnBased_FillHandler.log");

        //ATB Fill - main function
        int atb_fx_addr = 0x634b40 - addr_offset;
        _atb_fx_handle = new FhMethodHandle<atb_fx>(this, "FFX-2.exe", atb_fx_addr, h_atb_fx);

        //ATB Fill -sub-functions
        _MsMagicCheckCommandExe_handle = new FhMethodHandle<MsMagicCheckCommandExe>(this, "FFX-2.exe", 0x644bb0 - addr_offset, h_MsMagicCheckCommandExe);
        _bravo_fx_handle = new FhMethodHandle<bravo_fx>(this, "FFX-2.exe", 0x634b00 - addr_offset, h_bravo_fx);
        _MsCheckMonsterOversoul_handle = new FhMethodHandle<MsCheckMonsterOversoul>(this, "FFX-2.exe", 0x61c290 - addr_offset, h_MsCheckMonsterOversoul);
        _MsResetDefenseStatus_handle = new FhMethodHandle<MsResetDefenseStatus>(this, "FFX-2.exe", 0x636900 - addr_offset, h_MsResetDefenseStatus);
        _MsClearDanceStatusMotion_handle = new FhMethodHandle<MsClearDanceStatusMotion>(this, "FFX-2.exe", 0x636400 - addr_offset, h_MsClearDanceStatusMotion);
        _MsGetRamChrMonster_handle = new FhMethodHandle<MsGetRamChrMonster>(this, "FFX-2.exe", 0x634390 - addr_offset, h_MsGetRamChrMonster);
        _MsCheckDanceStatus_handle = new FhMethodHandle<MsCheckDanceStatus>(this, "FFX-2.exe", 0x636360 - addr_offset, h_MsCheckDanceStatus);

        _MsGetChr_handle = new FhMethodHandle<MsGetChr>(this, "FFX-2.exe", 0x611450 - addr_offset, h_MsGetChr);

        //status handling - see ATBFillStatusHandler.cs for delegates and FhMethodHandle setup
        _MsStatusProcess_handle = new FhMethodHandle<MsStatusProcess>(this, "FFX-2.exe", 0x636eb0 - addr_offset, h_MsStatusProcess);
        _MsStatCheckStop_handle = new FhMethodHandle<MsStatCheckStop>(this, "FFX-2.exe", 0x6430f0 - addr_offset, h_MsStatCheckStop);
        _MsATBActiveCheck_handle = new FhMethodHandle<MsATBActiveCheck>(this, "FFX-2.exe", 0x633f90 - addr_offset, h_MsATBActiveCheck);
        _FUN_006218E0_handle = new FhMethodHandle<F6218E0>(this, "FFX-2.exe", 0x6218E0 - addr_offset, h_FUN_006218E0);//MsCheckStatCount?
        _ClampBetween_handle = new FhMethodHandle<ClampBetween>(this, "FFX-2.exe", 0x624cd0 - addr_offset, h_ClampBetween);//MsCheckRange
        _FUN_00636690_handle = new FhMethodHandle<F636690>(this, "FFX-2.exe", 0x636690 - addr_offset, h_FUN_00636690);
        _MsStructClear_handle = new FhMethodHandle<MsStructClear>(this, "FFX-2.exe", 0x62a0f0 - addr_offset, h_MsStructClear);
        _MsDamageBufferExe_handle = new FhMethodHandle<MsDamageBufferExe>(this, "FFX-2.exe", 0x6422d0 - addr_offset, h_MsDamageBufferExe);
        _MsSetStatus_handle = new FhMethodHandle<MsSetStatus>(this, "FFX-2.exe", 0x636ca0 - addr_offset, h_MsSetStatus);
        _MsSetChrWeak_handle = new FhMethodHandle<MsSetChrWeak>(this, "FFX-2.exe", 0x61b080 - addr_offset, h_MsSetChrWeak);
        _MsStatusEffectCheck_handle = new FhMethodHandle<MsStatusEffectCheck>(this, "FFX-2.exe", 0x623290 - addr_offset, h_MsStatusEffectCheck);
        _MsMotionRecoverExe_handle = new FhMethodHandle<MsMotionRecoverExe>(this, "FFX-2.exe", 0x6330e0 - addr_offset, h_MsMotionRecoverExe);

    }

    public int h_MsGetChr(uint param_1) {
        return _MsGetChr_handle.orig_fptr.Invoke(param_1);
    }

    //ATB Fill - sub-function set up
    public unsafe int h_MsMagicCheckCommandExe(int* param_1, uint param_2, int* param_3, int* param_4) {
        return _MsMagicCheckCommandExe_handle.orig_fptr.Invoke(param_1, param_2, param_3, param_4); 
    }
    public int h_bravo_fx(int param_1) {
        return _bravo_fx_handle.orig_fptr.Invoke(param_1);
    }
    public int h_MsCheckMonsterOversoul(uint param_1) {
        return _MsCheckMonsterOversoul_handle.orig_fptr.Invoke(param_1);
    }
    public int h_MsResetDefenseStatus(byte param_1) {
        return _MsResetDefenseStatus_handle.orig_fptr.Invoke(param_1);
    }
    public int h_MsClearDanceStatusMotion(byte param_1) {
        return _MsClearDanceStatusMotion_handle.orig_fptr.Invoke(param_1);
    }
    public int h_MsGetRamChrMonster(byte param_1) {
        return _MsGetRamChrMonster_handle.orig_fptr.Invoke(param_1);
    }
    public int h_MsCheckDanceStatus(byte param_1) {
        return _MsCheckDanceStatus_handle.orig_fptr.Invoke(param_1);
    }


//This is the turn based rewrite of the main ATB fill function
//p3 is a pointer to some array, p4 is index
 public unsafe int h_atb_fx(byte chr_id, int chr_base_address, int* param_3, int param_4){
        bool bVar1;
        int iVar2;
        int iVar3;
        byte character_state;
        int local_8;


        iVar2 = chr_base_address;//local copy seemingly necessary for it not to crash on h_bravo_fx

        

        //checks some flags and returns early if they're set as below
        if (((chr_base_address == 0) || (*(byte*)(chr_base_address + 0x1789) == 0)) ||
        (*(byte*)(chr_base_address + 0xe67) != 0)) {
            return param_4;
        }

        //get character state variable (1 = can't act (ATB fill/Petrified/charging) etc.
        //4 is character can act, 9 when character is performing an action
        character_state = *(byte*)(chr_base_address + 0xe68);

        //if not in a certain state return early
        if (character_state != 0 && character_state != 1 && character_state != 2) { goto LAB_RETURN; }


        if (character_state == 0) {
            //need a pointer to this memory address for (h_MsMagicCheckCommandExe) first parameter
            int* DAT_00DF7F90 = FhUtil.ptr_at<int>(0x9F7F90);
            //data for if check
            byte DAT_00DF8814 = FhUtil.get_at<byte>(0x9F8814);
            byte DAT_00DF8816 = FhUtil.get_at<byte>(0x9F8816);
            //sub-menu open Wait flag
            byte DAT_00DF8817 = FhUtil.get_at<byte>(0x9F8817);
            byte DAT_00DF78A0 = FhUtil.get_at<byte>(0x9F78A0);
            byte DAT_00DF78A3 = FhUtil.get_at<byte>(0x9F78A3);
            byte DAT_00DF78A4 = FhUtil.get_at<byte>(0x9F78A4);

            //FUN_00644bb0(&DAT_00df7f90, 0xff, &param_2, &local_8);
            h_MsMagicCheckCommandExe(DAT_00DF7F90, 0xFF, &chr_base_address, &local_8);

            bVar1 = true;
            //check some flags including the sub-menu open wait flag (commented out) and return early if set
            if ( (DAT_00DF8816 != '\0') || (DAT_00DF78A4 != '\0') ||
               /*(DAT_00DF8817 != '\0') ||*/ (DAT_00DF8814 != '\0') || (chr_base_address == 2)) goto LAB_RETURN;

            //more early return checks
            if ((DAT_00DF78A0 != '\x01') || (DAT_00DF78A3 != '\0')) {
                bVar1 = false;
            }
            if ((local_8 != 0) || (!bVar1)) goto LAB_RETURN;


            //update character stat variable
            character_state = 1;
        }//end if character_state = 0 block

       if (character_state == 1) {

            int fill_check_bravo;
            //634b00
            //a sort of check if character's ATB is allowed to fill - mainly related to character's calculated ATB Speed value
            /*Will not return early if Stop/Sleep/Petrify are active and won't prevent characters with these from charging
             * - these statuses affect the speed value
             * in ChrAtbSpeedHandler*/
            //iVar2 is the character's base address stored in a var
            fill_check_bravo = h_bravo_fx(iVar2);/*this function checks if character is Active, Alive and the if the character's speed value (if 0 (i.e Stopped)). 
                                        * If not stopped etc - returns 1
                                        * otherwise returns 0
                                         */
            //return early if character's ATB not allowed to fill
            if (fill_check_bravo == 0) goto LAB_RETURN;


            //custom ATB fill handling
            custom_atb_progress();

            //if ATB still not full then return early
            if (0 < *(int*)(iVar2 + 0x9d8)) goto LAB_RETURN;
            
            //oversoul handling
            h_MsCheckMonsterOversoul(chr_id);
            //Warrior Sentinel Handling
            h_MsResetDefenseStatus(chr_id);
            //Not sure what this does. It checks a flag that seems to be 0 msot of the time and does nothing.
            //636400
            h_MsClearDanceStatusMotion(chr_id);

            character_state = 2;

            /*This function checks whether the actor is in the enemy/creature section of the BattleChr data
             *and checks whether the 'Control Creatures' and 'Control Enemies' debug flags are set
             *Returns 1 if either of them are set
             *Returns 0 if neither of them are set
             */
            iVar3 = h_MsGetRamChrMonster(chr_id);
            //if Control Creatures/Enemies is enabled
            if (iVar3 != 0) {
                //zero this
                *(int*)(iVar2 + 0x9f4) = 0;
            }

        }//end if character_state = 1 block
        else if (character_state != 2) {
            // all other states return
            goto LAB_RETURN;
        }


        iVar3 = h_MsCheckDanceStatus(chr_id); // fun_00636360(chr_id) - Usually returns 1
        //This block progresses characters on from state 2 -> 3, after this characters can move onto state 4 (Window showing) or onto acting 
        // + 9f4 is usually 0, so this runs most of the time (some pause/ delay buffer?)
        //if buffer not 0, causes regen/poison bug fixed in BugCausingFXModule.cs
        if (*(int*)(iVar2 + 0x9f4) < 1) {
            //usually 1 so does run most of the time
            if (iVar3 != 0) {
                iVar3 = h_bravo_fx(iVar2);
                if (iVar3 != 0 && param_4 < 0x1F) {

                    //updates some table with the chr_id
                    param_3[param_4] = chr_id;
                    //*(int*)(param_3 + param_4 * 4) = chr_id; //Ghidra decomp original
                    *(byte*)(iVar2 + 0xe68) = 3; // progresses characters character_state to 3

                    TbStatusProcess();

                    return param_4 + 1;//returns 1
                }
            }
        }
        else {
            //some decrementing counter, but for what purpose?
            //if buffer not 0, causes regen/poison bug fixed in BugCausingFXModule.cs - essentially, I don't want this block entered
            *(int*)(iVar2 + 0x9f4) = *(int*)(iVar2 + 0x9f4) - *(int*)(iVar2 + 0x9fc);
        }

        

    LAB_RETURN:
        //write character state and return
        *(byte*)(iVar2 + 0xe68) = character_state;
        return param_4;
}//END h_atb_fx

//function that rewrites how ATB Progress is handled
    public void custom_atb_progress() {
        //define an array will hold a flag for each character that says whether their ATB can charge or not
        int[] can_fill_array = new int[31];
        //an array to hold each characters time until ATB full value
        int[] atb_timer_values = new int[31];

        //fill up arrays
        for (int i = 0; i < can_fill_array.Length; i++) {
            int chr_base_addr = h_MsGetChr((uint)i);
            can_fill_array[i] = h_bravo_fx(chr_base_addr);
            atb_timer_values[i] = *(int*)(chr_base_addr + 0x9d8);

        }

        // Find lowest time remaining value of the characters whos ATBS are allowed to charge
        int winningIndex = -1;
        int bestValue = int.MaxValue;
        //cycle through the chr can_fill array
        for (int i = 0; i < can_fill_array.Length; i++) {
            //where the character is allowed to fill, update time remaining variable
            if (can_fill_array[i] == 1) {
                int time_remaining = atb_timer_values[i];
                //if the time remaining is lower than the current lowest recorded, update with the new lowest
                if (time_remaining < bestValue) {
                    bestValue = time_remaining;
                    winningIndex = i;
                }
            }
        }

        //update ATBs
        //If a at least 1 character's ATB is allowed to fill then
        if (winningIndex != -1) {
            //cycle through the Can_Fill array
            for (int i = 0; i < can_fill_array.Length; i++) {
                /*where the character's ATB is allowed to fill, get their address and subtract the smallest
                 * ATB Time remaining for all characters from their remaining ATB time*/
                if (can_fill_array[i] == 1) {
                    int chr_addr = h_MsGetChr((uint)i);
                    *(int*)(chr_addr + 0x9d8) = *(int*)(chr_addr + 0x9d8) - bestValue;
                }
            }
        }
        else {
            _fill_logger.Info("No characters eligible for ATB fill.");
        }
        //logging  
        //_fill_logger.Info("Can fill array: " + string.Join("", can_fill));
        //_fill_logger.Info("ATB remaining: " + string.Join(" / ", atb_timer_values));
    }

    public override bool init(FhModContext mod_context, FileStream global_state_file) {
        // ATB fill hooks
        _atb_fx_handle.hook();
        _MsMagicCheckCommandExe_handle.hook();
        _bravo_fx_handle.hook();
        _MsCheckMonsterOversoul_handle.hook();
        _MsResetDefenseStatus_handle.hook();
        _MsClearDanceStatusMotion_handle.hook();
        _MsGetRamChrMonster_handle.hook();
        _MsGetChr_handle.hook();

        // status handling hooks
        _MsStatusProcess_handle.hook();
        _MsStatCheckStop_handle.hook();
        _MsATBActiveCheck_handle.hook();
        _FUN_006218E0_handle.hook();
        _ClampBetween_handle.hook();
        _FUN_00636690_handle.hook();
        _MsStructClear_handle.hook();
        _MsDamageBufferExe_handle.hook();
        _MsSetStatus_handle.hook();
        _MsSetChrWeak_handle.hook();
        _MsStatusEffectCheck_handle.hook();
        _MsMotionRecoverExe_handle.hook();
        return true;
        
    }

    public override void load_local_state(FileStream? local_state_file, FhLocalStateInfo local_state_info) { }
    public override void save_local_state(FileStream local_state_file) { }
}
