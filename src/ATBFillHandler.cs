// SPDX-License-Identifier: MIT

namespace Fahrenheit.Modules.FFX2TurnBased;

[FhLoad(FhGameId.FFX2)]
public unsafe class ATBFillModule : FhModule {
    //offset address
    int addr_offset = 0x400000;

    protected readonly FhLogger _fill_logger;

    //delegates
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    //634b40 - atb function and parameters - this handles ATB timer counting down and more
    public unsafe delegate int atb_fx(byte chr_id, int chr_base_address, int* param_3, int param_4);
    //Sub-Functions
    //644bb0
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public unsafe delegate int alpha_fx(int* param_1, uint param_2, int* param_3, int* param_4);

    /*634b00 - this function checks some character values including their ATB tick down speed
    *pseudo checks for Stop (i.e their tick down value is 0 but Sleep/Petrify/Stop are better covered
    in the ChrAtbSpeedHandler*/
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int bravo_fx(int param_1);

    //61c290 - oversoul handling
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int oversoul_fx(uint param_1);

    //636900 - seems to do status checks if user had used Warriors Sentinel
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int delta_fx(byte param_1);

    //636400
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int echo_fx(byte param_1);
    //634390 - checks if Control Creatures and Control Enemy debug flags are set
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int foxtrot_fx(byte param_1);
    //636360
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int golf_fx(byte param_1);

    //get_chr_addr FUN_00611450
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int get_chr_addr(uint param_1);

    //main function
    private readonly FhMethodHandle<atb_fx> _atb_fx_handle;
    //sub-functions
    private readonly FhMethodHandle<alpha_fx> _alpha_fx_handle;
    private readonly FhMethodHandle<bravo_fx> _bravo_fx_handle;
    private readonly FhMethodHandle<oversoul_fx> _oversoul_fx_handle;
    private readonly FhMethodHandle<delta_fx> _delta_fx_handle;
    private readonly FhMethodHandle<echo_fx> _echo_fx_handle;
    private readonly FhMethodHandle<foxtrot_fx> _foxtrot_fx_handle;
    private readonly FhMethodHandle<golf_fx> _golf_fx_handle;
    private readonly FhMethodHandle<get_chr_addr> _get_chr_addr_handle;

    public ATBFillModule() {

        _fill_logger = new FhLogger("TurnBased_FillHandler.log");
        //main function
        int atb_fx_addr = 0x634b40 - addr_offset;
        _atb_fx_handle = new FhMethodHandle<atb_fx>(this, "FFX-2.exe", atb_fx_addr, h_atb_fx);

        //sub-functions
        int alpha_fx_addr = 0x644bb0 - addr_offset;
        _alpha_fx_handle = new FhMethodHandle<alpha_fx>(this, "FFX-2.exe", alpha_fx_addr, h_alpha_fx);
        int bravo_fx_addr = 0x634b00 - addr_offset;
        _bravo_fx_handle = new FhMethodHandle<bravo_fx>(this, "FFX-2.exe", bravo_fx_addr, h_bravo_fx);
        int oversoul_fx_addr = 0x61c290 - addr_offset;
        _oversoul_fx_handle = new FhMethodHandle<oversoul_fx>(this, "FFX-2.exe", oversoul_fx_addr, h_oversoul_fx);
        int delta_fx_addr = 0x636900 - addr_offset;
        _delta_fx_handle = new FhMethodHandle<delta_fx>(this, "FFX-2.exe", delta_fx_addr, h_delta_fx);
        int echo_fx_addr = 0x636400 - addr_offset;
        _echo_fx_handle = new FhMethodHandle<echo_fx>(this, "FFX-2.exe", echo_fx_addr, h_echo_fx);
        int foxtrot_fx_addr = 0x634390 - addr_offset;
        _foxtrot_fx_handle = new FhMethodHandle<foxtrot_fx>(this, "FFX-2.exe", foxtrot_fx_addr, h_foxtrot_fx);
        int golf_fx_addr = 0x636360 - addr_offset;
        _golf_fx_handle = new FhMethodHandle<golf_fx>(this, "FFX-2.exe", golf_fx_addr, h_golf_fx);

        _get_chr_addr_handle = new FhMethodHandle<get_chr_addr>(this, "FFX-2.exe", 0x611450 - addr_offset, h_get_chr_addr);
        
    }

    public int h_get_chr_addr(uint param_1) {
        return _get_chr_addr_handle.orig_fptr.Invoke(param_1);
    }

    //sub-function set up
    public unsafe int h_alpha_fx(int* param_1, uint param_2, int* param_3, int* param_4) {
        return _alpha_fx_handle.orig_fptr.Invoke(param_1, param_2, param_3, param_4); 
    }
    public int h_bravo_fx(int param_1) {
        return _bravo_fx_handle.orig_fptr.Invoke(param_1);
    }
    public int h_oversoul_fx(uint param_1) {
        return _oversoul_fx_handle.orig_fptr.Invoke(param_1);
    }
    public int h_delta_fx(byte param_1) {
        return _delta_fx_handle.orig_fptr.Invoke(param_1);
    }
    public int h_echo_fx(byte param_1) {
        return _echo_fx_handle.orig_fptr.Invoke(param_1);
    }
    public int h_foxtrot_fx(byte param_1) {
        return _foxtrot_fx_handle.orig_fptr.Invoke(param_1);
    }
    public int h_golf_fx(byte param_1) {
        return _golf_fx_handle.orig_fptr.Invoke(param_1);
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
            //need a pointer to this memory address for (h_alpha_fx) first parameter
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
            h_alpha_fx(DAT_00DF7F90, 0xFF, &chr_base_address, &local_8);

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
            h_oversoul_fx(chr_id);
            //this function decrements status timers? 636900
            h_delta_fx(chr_id);
            //Not sure what this does. It checks a flag that seems to be 0 msot of the time and does nothing.
            //636400
            h_echo_fx(chr_id);

            character_state = 2;

            /*This function checks whether the actor is in the enemy/creature section of the BattleChr data
             *and checks whether the 'Control Creatures' and 'Control Enemies' debug flags are set
             *Returns 1 if either of them are set
             *Returns 0 if neither of them are set
             */
            iVar3 = h_foxtrot_fx(chr_id);
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


        iVar3 = h_golf_fx(chr_id); // fun_00636360(chr_id) - Usually returns 1
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
            int chr_base_addr = h_get_chr_addr((uint)i);
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
                    int chr_addr = h_get_chr_addr((uint)i);
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

    //not used currently
    public int calc_lowest_time() {
        //define an array will hold a flag for each character that says whether their ATB can charge or not
        int[] can_fill_array = new int[31];
        //an array to hold each characters time until ATB full value
        int[] atb_timer_values = new int[31];


        //fill up arrays
        for (int i = 0; i < can_fill_array.Length; i++) {
            int chr_base_addr = h_get_chr_addr((uint)i);
            can_fill_array[i] = h_bravo_fx(chr_base_addr);
            atb_timer_values[i] = *(int*)(chr_base_addr + 0x9d8);
        }


        // Find lowest time remaining value of the characters whos ATBS are allowed to charge
        int winningIndex = -1;
        int lowestValue = int.MaxValue;
        //cycle through the chr can_fill array
        for (int i = 0; i < can_fill_array.Length; i++) {
            //where the character is allowed to fill, update time remaining variable
            if (can_fill_array[i] == 1) {
                int time_remaining = atb_timer_values[i];
                //if the time remaining is lower than the current lowest recorded, update with the new lowest
                if (time_remaining < lowestValue) {
                    lowestValue = time_remaining;
                    winningIndex = i;
                }
            }
        }
        return lowestValue;
    }

    public override bool init(FhModContext mod_context, FileStream global_state_file) {
        _atb_fx_handle.hook();
        _alpha_fx_handle.hook();
        _bravo_fx_handle.hook();
        _oversoul_fx_handle.hook();
        _delta_fx_handle.hook();
        _echo_fx_handle.hook();
        _foxtrot_fx_handle.hook();
        _get_chr_addr_handle.hook();
        return true;
        
    }

    public override void load_local_state(FileStream? local_state_file, FhLocalStateInfo local_state_info) { }
    public override void save_local_state(FileStream local_state_file) { }
}
